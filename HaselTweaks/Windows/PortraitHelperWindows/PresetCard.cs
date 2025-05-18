using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Memory;
using Dalamud.Utility;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using Lumina.Data.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HaselTweaks.Windows.PortraitHelperWindows;

[RegisterTransient]
public partial class PresetCard : IDisposable
{
    public static readonly Vector2 PortraitSize = new(576, 960); // native texture size

    private static readonly Color ButtonActiveColor = Color.White with { A = 0.3f };
    private static readonly Color ButtonHoveredColor = Color.White with { A = 0.2f };

    private readonly ILogger<PresetCard> _logger;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IDataManager _dataManager;
    private readonly ITextureProvider _textureProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly BannerUtils _bannerUtils;

    private readonly SavedPreset _preset;
    private readonly uint _bannerFrameImage;
    private readonly uint _bannerDecorationImage;
    private readonly bool _isBannerTimelineUnlocked;
    private readonly bool _isBannerBgUnlocked;
    private readonly bool _isBannerFrameUnlocked;
    private readonly bool _isBannerDecorationUnlocked;

    private CancellationTokenSource? _closeTokenSource;

    private bool _isDisposed;
    private bool _isImageLoading;
    private bool _doesImageFileExist;
    private bool _isImageUpdatePending;
    private Guid? _textureGuid;
    private Image<Rgba32>? _image;
    private IDalamudTextureWrap? _textureWrap;
    private DateTime _lastTextureCheck = DateTime.MinValue;

    private float _lastScale;

    public PresetCard(SavedPreset preset)
    {
        _logger = Service.Get<ILogger<PresetCard>>();
        _pluginInterface = Service.Get<IDalamudPluginInterface>();
        _dataManager = Service.Get<IDataManager>();
        _textureProvider = Service.Get<ITextureProvider>();
        _pluginConfig = Service.Get<PluginConfig>();
        _textService = Service.Get<TextService>();
        _excelService = Service.Get<ExcelService>();
        _bannerUtils = Service.Get<BannerUtils>();

        _preset = preset;

        if (_excelService.TryGetRow<BannerFrame>(_preset.Preset!.BannerFrame, out var bannerFrameRow))
            _bannerFrameImage = (uint)bannerFrameRow.Image;

        if (_excelService.TryGetRow<BannerDecoration>(_preset.Preset.BannerDecoration, out var bannerDecorationImageRow))
            _bannerDecorationImage = (uint)bannerDecorationImageRow.Image;

        _isBannerTimelineUnlocked = _bannerUtils.IsBannerTimelineUnlocked(_preset.Preset.BannerTimeline);
        _isBannerBgUnlocked = _bannerUtils.IsBannerBgUnlocked(_preset.Preset.BannerBg);
        _isBannerFrameUnlocked = _bannerUtils.IsBannerFrameUnlocked(_preset.Preset.BannerFrame);
        _isBannerDecorationUnlocked = _bannerUtils.IsBannerDecorationUnlocked(_preset.Preset.BannerDecoration);
    }

    public void Dispose()
    {
        _closeTokenSource?.Cancel();
        _closeTokenSource?.Dispose();
        _closeTokenSource = null;
        _image?.Dispose();
        _image = null;
        _textureWrap?.Dispose();
        _textureWrap = null;
        _isDisposed = true;
    }

    public void Draw(PresetBrowserOverlay overlay, float scale, uint defaultImGuiTextColor)
    {
        if (_isDisposed)
            return;

        Update(scale);

        var hasErrors = !_isBannerTimelineUnlocked || !_isBannerBgUnlocked || !_isBannerFrameUnlocked || !_isBannerDecorationUnlocked;
        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(_preset.Id.ToString());

        var cursorPos = ImGui.GetCursorPos();
        var center = cursorPos + PortraitSize * scale / 2f;

        var TextureService = Service.Get<TextureService>();

        TextureService.DrawIcon(190009, PortraitSize * scale);
        ImGui.SetCursorPos(cursorPos);

        if (_isImageLoading)
        {
            DrawLoadingSpinner(ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + center);
        }
        else if (!_doesImageFileExist)
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            using var color = Color.Red.Push(ImGuiCol.Text);
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
            TextureService.DrawIcon(_bannerFrameImage, PortraitSize * scale);
        }

        if (_bannerDecorationImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            TextureService.DrawIcon(_bannerDecorationImage, PortraitSize * scale);
        }

        if (hasErrors)
        {
            ImGui.SetCursorPos(cursorPos + new Vector2(PortraitSize.X - 190, 10) * scale);
            TextureService.Draw("ui/uld/Warning_hr1.tex", 160 * scale);
        }

        ImGui.SetCursorPos(cursorPos);

        {
            using var color = ImRaii.PushColor(ImGuiCol.Button, 0)
                .Push(ImGuiCol.ButtonActive, ButtonActiveColor)
                .Push(ImGuiCol.ButtonHovered, ButtonHoveredColor);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"##{_preset.Id}_Button", PortraitSize * scale);
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetCard.MovingPresetCard.Tooltip", _preset.Name));

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
            if (target)
            {
                var payload = ImGui.AcceptDragDropPayload("MovePresetCard");
                unsafe
                {
                    if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                    {
                        var config = _pluginConfig.Tweaks.PortraitHelper;
                        var presetId = MemoryHelper.Read<Guid>(payload.Data).ToString();
                        var oldIndex = config.Presets.AsEnumerable().IndexOf((preset) => preset.Id.ToString() == presetId);
                        var newIndex = config.Presets.IndexOf(_preset);
                        var item = config.Presets[oldIndex];
                        config.Presets.RemoveAt(oldIndex);
                        config.Presets.Insert(newIndex, item);
                        _pluginConfig.Save();
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

                    using (Color.Red.Push(ImGuiCol.Text))
                    {
                        ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.Title"));

                        using (ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 2))
                        using (ImRaii.PushIndent(1))
                        {
                            if (!_isBannerTimelineUnlocked)
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.PoseNotUnlocked", _bannerUtils.GetBannerTimelineName(_preset.Preset!.BannerTimeline)));
                            }

                            if (!_isBannerBgUnlocked && _excelService.TryGetRow<BannerBg>(_preset.Preset!.BannerBg, out var bannerBgRow))
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.BackgroundNotUnlocked", bannerBgRow.Name.ExtractText()));
                            }

                            if (!_isBannerFrameUnlocked && _excelService.TryGetRow<BannerFrame>(_preset.Preset!.BannerFrame, out var bannerFrameRow))
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.FrameNotUnlocked", bannerFrameRow.Name.ExtractText()));
                            }

                            if (!_isBannerDecorationUnlocked && _excelService.TryGetRow<BannerDecoration>(_preset.Preset!.BannerDecoration, out var bannerDecorationRow))
                            {
                                ImGui.TextUnformatted("•");
                                ImGui.SameLine(0, 5);
                                ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.DecorationNotUnlocked", bannerDecorationRow.Name.ExtractText()));
                            }
                        }
                    }
                }
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _preset.Preset?.ToState(_logger, _bannerUtils, ImportFlags.All);
                overlay.MenuBar.CloseOverlays();
            }
        }

        using var popup = ImRaii.ContextPopupItem($"{_preset.Id}_Popup");
        if (!popup)
            return;

        using var textColor = ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor);

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.LoadPreset.Label")))
        {
            _preset.Preset?.ToState(_logger, _bannerUtils, ImportFlags.All);
            overlay.MenuBar.CloseOverlays();
        }

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.EditPreset.Label")))
        {
            overlay.EditPresetDialog.Open(_preset);
        }

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.ExportToClipboard.Label")))
        {
            _preset.Preset?.ToClipboard(_logger);
        }

        if (_image != null && ImGui.BeginMenu(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Label")))
        {
            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Everything.Label")))
            {
                Task.Run(async () => await CopyImage());
            }

            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrame.Label")))
            {
                Task.Run(async () => await CopyImage(CopyImageFlags.NoFrame));
            }

            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutDecoration.Label")))
            {
                Task.Run(async () => await CopyImage(CopyImageFlags.NoDecoration));
            }

            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrameAndDecoration.Label")))
            {
                Task.Run(async () => await CopyImage(CopyImageFlags.NoFrame | CopyImageFlags.NoDecoration));
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.DeletePreset.Label")))
        {
            overlay.DeletePresetDialog.Open(overlay, _preset);
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
            if (_textureProvider.TryGetIconPath(_bannerFrameImage, out var iconPath))
            {
                var texture = _dataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        if (!flags.HasFlag(CopyImageFlags.NoDecoration) && _bannerDecorationImage != 0)
        {
            if (_textureProvider.TryGetIconPath(_bannerDecorationImage, out var iconPath))
            {
                var texture = _dataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        await ClipboardUtils.SetClipboardImage(tempImage);
    }

    private void Update(float scale)
    {
        if (!_isImageLoading && _preset != null && _preset.Id != _textureGuid)
        {
            _image?.Dispose();
            _image = null;

            _textureWrap?.Dispose();
            _textureWrap = null;

            if (DateTime.Now - _lastTextureCheck > TimeSpan.FromSeconds(1))
            {
                var thumbPath = _pluginInterface.GetPortraitThumbnailPath(_preset.Id);

                if (File.Exists(thumbPath))
                {
                    _isImageLoading = true;
                    _closeTokenSource ??= new();
                    Task.Run(async () =>
                    {
                        try
                        {
                            _image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(thumbPath, _closeTokenSource.Token);
                            _isImageUpdatePending = true;
                            _doesImageFileExist = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error while loading thumbnail");
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
                    using var scaledImage = _image.Clone();

                    if (_closeTokenSource.IsCancellationRequested)
                        return;

                    scaledImage.Mutate(i => i.Resize((int)(PortraitSize.X * scale), (int)(PortraitSize.Y * scale), KnownResamplers.Lanczos3));

                    if (_closeTokenSource.IsCancellationRequested)
                        return;

                    var data = new byte[4 * scaledImage.Width * scaledImage.Height];
                    scaledImage.CopyPixelDataTo(data);

                    if (_closeTokenSource.IsCancellationRequested)
                        return;

                    _textureWrap = _textureProvider.CreateFromRaw(RawImageSpecification.Rgba32(scaledImage.Width, scaledImage.Height), data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while resizing/loading thumbnail");
                }
                finally
                {
                    _isImageLoading = false;
                }
            }, _closeTokenSource.Token);
        }
    }
}
