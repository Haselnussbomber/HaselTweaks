using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using Dalamud.Utility;
using HaselCommon.Extensions;
using HaselCommon.Utils;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using Lumina.Data.Files;
using Lumina.Excel.GeneratedSheets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public class PresetCard : IDisposable
{
    private static PortraitHelperConfiguration Config => Service.GetService<Configuration>().Tweaks.PortraitHelper;
    public static readonly Vector2 PortraitSize = new(576, 960); // native texture size

    private readonly uint ButtonActiveColor = Colors.White.WithAlpha(0.3f);
    private readonly uint ButtonHoveredColor = Colors.White.WithAlpha(0.2f);

    private CancellationTokenSource? _closeTokenSource;

    private readonly PresetBrowserOverlay _overlay;
    private readonly SavedPreset _preset;
    private readonly uint _bannerFrameImage;
    private readonly uint _bannerDecorationImage;
    private readonly bool _isBannerTimelineUnlocked;
    private readonly bool _isBannerBgUnlocked;
    private readonly bool _isBannerFrameUnlocked;
    private readonly bool _isBannerDecorationUnlocked;

    private bool _isImageLoading;
    private bool _doesImageFileExist;
    private bool _isImageUpdatePending;

    private Guid? _textureGuid;
    private Image<Rgba32>? _image;
    private IDalamudTextureWrap? _textureWrap;
    private DateTime _lastTextureCheck = DateTime.MinValue;

    private float _lastScale;

    public PresetCard(PresetBrowserOverlay overlay, SavedPreset preset)
    {
        _overlay = overlay;
        _preset = preset;

        _bannerFrameImage = (uint)(GetRow<BannerFrame>(_preset.Preset!.BannerFrame)?.Image ?? 0);
        _bannerDecorationImage = (uint)(GetRow<BannerDecoration>(_preset.Preset.BannerDecoration)?.Image ?? 0);

        _isBannerTimelineUnlocked = PortraitHelper.IsBannerTimelineUnlocked(_preset.Preset.BannerTimeline);
        _isBannerBgUnlocked = PortraitHelper.IsBannerBgUnlocked(_preset.Preset.BannerBg);
        _isBannerFrameUnlocked = PortraitHelper.IsBannerFrameUnlocked(_preset.Preset.BannerFrame);
        _isBannerDecorationUnlocked = PortraitHelper.IsBannerDecorationUnlocked(_preset.Preset.BannerDecoration);
    }

    public void Dispose()
    {
        _closeTokenSource?.Cancel();
        _closeTokenSource?.Dispose();
        _image?.Dispose();
        _textureWrap?.Dispose();
    }

    public void Draw(float scale, uint defaultImGuiTextColor)
    {
        Update(scale);

        var hasErrors = !_isBannerTimelineUnlocked || !_isBannerBgUnlocked || !_isBannerFrameUnlocked || !_isBannerDecorationUnlocked;
        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(_preset.Id.ToString());

        var cursorPos = ImGui.GetCursorPos();
        var center = cursorPos + PortraitSize * scale / 2f;

        Service.TextureManager.GetIcon(190009).Draw(PortraitSize * scale);
        ImGui.SetCursorPos(cursorPos);

        if (_isImageLoading)
        {
            DrawLoadingSpinner(ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + center);
        }
        else if (!_doesImageFileExist)
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            using var color = Colors.Red.Push(ImGuiCol.Text);
            ImGui.SetCursorPos(center - ImGui.CalcTextSize(FontAwesomeIcon.FileImage.ToIconString()) / 2f);
            ImGui.TextUnformatted(FontAwesomeIcon.FileImage.ToIconString());
        }
        else if (_textureWrap != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGui.Image(_textureWrap.ImGuiHandle, PortraitSize * scale);
        }

        if (_bannerFrameImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            Service.TextureManager.GetIcon(_bannerFrameImage).Draw(PortraitSize * scale);
        }

        if (_bannerDecorationImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            Service.TextureManager.GetIcon(_bannerDecorationImage).Draw(PortraitSize * scale);
        }

        if (hasErrors)
        {
            ImGui.SetCursorPos(cursorPos + new Vector2(PortraitSize.X - 190, 10) * scale);
            Service.TextureManager.Get("ui/uld/Warning.tex", 2).Draw(160 * scale);
        }

        ImGui.SetCursorPos(cursorPos);

        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0)
                .Push(ImGuiCol.ButtonActive, ButtonActiveColor)
                .Push(ImGuiCol.ButtonHovered, ButtonHoveredColor);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"##{_preset.Id}_Button", PortraitSize * scale);
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source.Success)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.TextUnformatted(t("PortraitHelperWindows.PresetCard.MovingPresetCard.Tooltip", _preset.Name));

                unsafe
                {
                    var bytes = _preset.Id.ToByteArray();
                    fixed (byte* ptr = bytes)
                    {
                        ImGui.SetDragDropPayload("MovePresetCard", (nint)ptr, (uint)bytes.Length);
                    }
                }
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success)
            {
                var payload = ImGui.AcceptDragDropPayload("MovePresetCard");
                unsafe
                {
                    if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                    {
                        var presetId = MemoryHelper.Read<Guid>(payload.Data).ToString();
                        var oldIndex = Config.Presets.AsEnumerable().IndexOf((preset) => preset.Id.ToString() == presetId);
                        var newIndex = Config.Presets.IndexOf(_preset);
                        var item = Config.Presets[oldIndex];
                        Config.Presets.RemoveAt(oldIndex);
                        Config.Presets.Insert(newIndex, item);
                        Service.GetService<Configuration>().Save();
                    }
                }
            }
        }

        if (ImGui.IsItemHovered() && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            using (ImRaii.Tooltip())
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.TextUnformatted(_preset.Name);

                if (hasErrors)
                {
                    ImGui.Separator();

                    using (ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red))
                    {
                        ImGui.TextUnformatted(t("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.Title"));

                        using (ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 2))
                        using (ImRaii.PushIndent(1))
                        {
                            if (!_isBannerTimelineUnlocked)
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(t("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.PoseNotUnlocked", PortraitHelper.GetBannerTimelineName(_preset.Preset!.BannerTimeline)));
                            }

                            if (!_isBannerBgUnlocked)
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(t("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.BackgroundNotUnlocked", GetSheetText<BannerBg>(_preset.Preset!.BannerBg, "Name")));
                            }

                            if (!_isBannerFrameUnlocked)
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(t("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.FrameNotUnlocked", GetSheetText<BannerFrame>(_preset.Preset!.BannerFrame, "Name")));
                            }

                            if (!_isBannerDecorationUnlocked)
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(t("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.DecorationNotUnlocked", GetSheetText<BannerDecoration>(_preset.Preset!.BannerDecoration, "Name")));
                            }
                        }
                    }
                }
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _preset.Preset?.ToState(ImportFlags.All);
                PortraitHelper.CloseOverlays();
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
        {
            using var popup = ImRaii.ContextPopupItem($"{_preset.Id}_Popup");
            if (popup.Success)
            {
                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.LoadPreset.Label")))
                {
                    _preset.Preset?.ToState(ImportFlags.All);
                    PortraitHelper.CloseOverlays();
                }

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.EditPreset.Label")))
                {
                    _overlay.EditPresetDialog.Open(_preset);
                }

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.ExportToClipboard.Label")))
                {
                    _preset.Preset?.ToClipboard();
                }

                if (_image != null && ImGui.BeginMenu(t("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Label")))
                {
                    if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Everything.Label")))
                    {
                        Task.Run(async () => await CopyImage());
                    }

                    if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrame.Label")))
                    {
                        Task.Run(async () => await CopyImage(CopyImageFlags.NoFrame));
                    }

                    if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutDecoration.Label")))
                    {
                        Task.Run(async () => await CopyImage(CopyImageFlags.NoDecoration));
                    }

                    if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrameAndDecoration.Label")))
                    {
                        Task.Run(async () => await CopyImage(CopyImageFlags.NoFrame | CopyImageFlags.NoDecoration));
                    }

                    ImGui.EndMenu();
                }

                ImGui.Separator();

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.DeletePreset.Label")))
                {
                    _overlay.DeletePresetDialog.Open(_preset);
                }
            }
        }
    }

    public static void DrawLoadingSpinner(Vector2 center, float radius = 10f)
    {
        var angle = 0.0f;
        var numSegments = 10;
        var angleStep = (float)(Math.PI * 2.0f / numSegments);
        var time = ImGui.GetTime();
        var drawList = ImGui.GetWindowDrawList();

        for (var i = 0; i < numSegments; i++)
        {
            var pos = center + new Vector2(
                radius * (float)Math.Cos(angle),
                radius * (float)Math.Sin(angle));

            var t = (float)(-angle / (float)Math.PI / 2f + time) % 1f;
            var color = new Vector4(1f, 1f, 1f, 1 - t);

            drawList.AddCircleFilled(pos, 2f, ImGui.ColorConvertFloat4ToU32(color));

            angle += angleStep;
        }
    }

    private async Task CopyImage(CopyImageFlags flags = CopyImageFlags.None)
    {
        if (_image == null)
            return;

        using var tempImage = _image.Clone();

        if (!flags.HasFlag(CopyImageFlags.NoFrame) && _bannerFrameImage != 0)
        {
            var iconPath = Service.TextureProvider.GetIconPath(_bannerFrameImage);
            if (iconPath != null)
            {
                var texture = Service.DataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        if (!flags.HasFlag(CopyImageFlags.NoDecoration) && _bannerDecorationImage != 0)
        {
            var iconPath = Service.TextureProvider.GetIconPath(_bannerDecorationImage);
            if (iconPath != null)
            {
                var texture = Service.DataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        await ClipboardUtils.SetClipboardImage(tempImage);
    }

    private void Update(float scale)
    {
        if (!_isImageLoading && _preset.Id != _textureGuid)
        {
            _image?.Dispose();
            _image = null;

            _textureWrap?.Dispose();
            _textureWrap = null;

            if (DateTime.Now - _lastTextureCheck > TimeSpan.FromSeconds(1))
            {
                var thumbPath = PortraitHelper.GetPortraitThumbnailPath(_preset.Id);

                if (File.Exists(thumbPath))
                {
                    _isImageLoading = true;
                    _closeTokenSource ??= new();
                    Task.Run(async () =>
                    {
                        try
                        {
                            _image = await Image.LoadAsync<Rgba32>(thumbPath, _closeTokenSource.Token);
                            _isImageUpdatePending = true;
                            _doesImageFileExist = true;
                        }
                        catch (Exception ex)
                        {
                            Service.PluginLog.Error(ex, "Error while loading thumbnail");
                            _isImageLoading = false;
                            _doesImageFileExist = false;
                        }
                        finally
                        {
                            _textureGuid = _preset.Id;
                        }
                    }, _closeTokenSource.Token);
                }

                _lastTextureCheck = DateTime.Now;
            }
        }

        if (scale != _lastScale)
        {
            _isImageUpdatePending = true;
            _lastScale = scale;
        }

        if (_image != null && _isImageUpdatePending)
        {
            _textureWrap?.Dispose();
            _isImageUpdatePending = false;
            _closeTokenSource ??= new();

            Task.Run(() =>
            {
                try
                {
                    var scaledImage = _image.Clone();

                    if (_closeTokenSource.IsCancellationRequested)
                        return;

                    scaledImage.Mutate(i => i.Resize((int)(PortraitSize.X * scale), (int)(PortraitSize.Y * scale), KnownResamplers.Lanczos3));

                    if (_closeTokenSource.IsCancellationRequested)
                        return;

                    var data = new byte[4 * scaledImage.Width * scaledImage.Height];
                    scaledImage.CopyPixelDataTo(data);

                    if (_closeTokenSource.IsCancellationRequested)
                        return;

                    _textureWrap = Service.PluginInterface.UiBuilder.LoadImageRaw(data, scaledImage.Width, scaledImage.Height, 4);
                }
                catch (Exception ex)
                {
                    Service.PluginLog.Error(ex, "Error while resizing/loading thumbnail");
                }
                finally
                {
                    _isImageLoading = false;
                }
            }, _closeTokenSource.Token);
        }
    }
}
