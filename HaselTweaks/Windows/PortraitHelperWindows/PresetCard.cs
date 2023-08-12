using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Excel.GeneratedSheets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = HaselTweaks.Structs.ImColor;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public class PresetCard : IDisposable
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;
    public static Vector2 PortraitSize { get; } = new(576, 960); // native texture size

    private bool _isDisposed;
    private readonly CancellationTokenSource _closeTokenSource = new();

    private readonly PresetBrowserOverlay _overlay;
    private readonly SavedPreset _preset;

    private bool _isImageLoading;
    private bool _doesImageFileExist;
    private bool _isImageUpdatePending;

    private string? _textureHash;
    private Image<Rgba32>? _image;
    private TextureWrap? _textureWrap;
    private DateTime _lastTextureCheck = DateTime.MinValue;

    private float _lastScale;

    private ushort _bannerFrame;
    private int? _bannerFrameImage;

    private ushort _bannerDecoration;
    private int? _bannerDecorationImage;

    public PresetCard(PresetBrowserOverlay overlay, SavedPreset preset)
    {
        _overlay = overlay;
        _preset = preset;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _closeTokenSource.Cancel();
        _closeTokenSource.Dispose();

        _image?.Dispose();
        _image = null;

        _textureWrap?.Dispose();
        _textureWrap = null;

        _isDisposed = true;
    }

    public void Draw(float scale)
    {
        if (_isDisposed)
            return;

        Update(scale);

        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(_preset.Id.ToString());

        var windowPos = ImGui.GetWindowPos();
        var cursorPos = ImGui.GetCursorPos();
        var center = windowPos + cursorPos + PortraitSize * scale / 2f - new Vector2(0, ImGui.GetScrollY());

        Service.TextureManager.GetIcon(190009).Draw(PortraitSize * scale);
        ImGui.SetCursorPos(cursorPos);

        if (_isImageLoading)
        {
            DrawLoadingSpinner(center);
        }
        else if (!_doesImageFileExist)
        {
            using var a = ImRaii.PushFont(UiBuilder.IconFont);
            using var b = ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.Red);
            ImGui.SetCursorPos(center - windowPos - ImGui.CalcTextSize(FontAwesomeIcon.FileImage.ToIconString()) / 2f);
            ImGuiUtils.TextUnformattedDisabled(FontAwesomeIcon.FileImage.ToIconString());
        }
        else if (_textureWrap != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGui.Image(_textureWrap.ImGuiHandle, PortraitSize * scale);
        }

        if (_bannerFrameImage != null)
        {
            ImGui.SetCursorPos(cursorPos);
            Service.TextureManager.GetIcon(_bannerFrameImage.Value).Draw(PortraitSize * scale);
        }

        if (_bannerDecorationImage != null)
        {
            ImGui.SetCursorPos(cursorPos);
            Service.TextureManager.GetIcon(_bannerDecorationImage.Value).Draw(PortraitSize * scale);
        }

        ImGui.SetCursorPos(cursorPos);

        {
            using var a = ImRaii.PushColor(ImGuiCol.Button, (uint)Colors.Transparent);
            using var b = ImRaii.PushColor(ImGuiCol.ButtonActive, (uint)new Color(1, 1, 1, 0.3f));
            using var c = ImRaii.PushColor(ImGuiCol.ButtonHovered, (uint)new Color(1, 1, 1, 0.2f));
            using var d = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"##{_preset.Id}_Button", PortraitSize * scale);
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source.Success)
            {
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
                        var oldIndex = Config.Presets.IndexOf((preset) => preset.Id.ToString() == presetId);
                        var newIndex = Config.Presets.IndexOf(_preset);
                        var item = Config.Presets[oldIndex];
                        Config.Presets.RemoveAt(oldIndex);
                        Config.Presets.Insert(newIndex, item);
                        Plugin.Config.Save();
                    }
                }
            }
        }

        if (ImGui.IsItemHovered() && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            using (ImRaii.Tooltip())
            {
                ImGui.TextUnformatted(_preset.Name);
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _overlay.Tweak.PresetToState(_preset.Preset, ImportFlags.All);
                _overlay.Tweak.CloseWindows();
            }
        }

        if (ImGui.BeginPopupContextItem($"{_preset.Id}_Popup"))
        {
            if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.LoadPreset.Label")))
            {
                _overlay.Tweak.PresetToState(_preset.Preset, ImportFlags.All);
                _overlay.Tweak.CloseWindows();
            }

            if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.EditPreset.Label")))
            {
                _overlay.EditPresetDialog.Open(_preset);
            }

            if (ImGui.MenuItem(t("PortraitHelperWindows.PresetCard.ContextMenu.ExportToClipboard.Label")))
            {
                _overlay.Tweak.PresetToClipboard(_preset.Preset);
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

            ImGui.EndPopup();
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

        if (!flags.HasFlag(CopyImageFlags.NoFrame) && _bannerFrameImage != null)
        {
            var iconId = _bannerFrameImage.Value;
            var texture = Service.DataManager.GetFile<TexFile>($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex");
            if (texture != null)
            {
                using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                tempImage.Mutate(x => x.DrawImage(image, 1f));
            }
        }

        if (!flags.HasFlag(CopyImageFlags.NoDecoration) && _bannerDecorationImage != null)
        {
            var iconId = _bannerDecorationImage.Value;
            var texture = Service.DataManager.GetFile<TexFile>($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex");
            if (texture != null)
            {
                using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                tempImage.Mutate(x => x.DrawImage(image, 1f));
            }
        }

        await ClipboardUtils.SetClipboardImage(tempImage);
    }

    private void Update(float scale)
    {
        if (_preset.Preset!.BannerFrame != _bannerFrame)
        {
            _bannerFrame = _preset.Preset.BannerFrame;
            _bannerFrameImage = GetRow<BannerFrame>(_bannerFrame)?.Image;
        }

        if (_preset.Preset.BannerDecoration != _bannerDecoration)
        {
            _bannerDecoration = _preset.Preset.BannerDecoration;
            _bannerDecorationImage = GetRow<BannerDecoration>(_bannerDecoration)?.Image;
        }

        if (!_isImageLoading && _preset.TextureHash != _textureHash)
        {
            _image?.Dispose();
            _image = null;

            _textureWrap?.Dispose();
            _textureWrap = null;

            if (!string.IsNullOrEmpty(_preset.TextureHash) && DateTime.Now - _lastTextureCheck > TimeSpan.FromSeconds(1))
            {
                var thumbPath = PortraitHelper.Configuration.GetPortraitThumbnailPath(_preset.TextureHash);

                if (File.Exists(thumbPath))
                {
                    _isImageLoading = true;
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
                            PluginLog.Error(ex, "Error while loading thumbnail");
                            _isImageLoading = false;
                            _doesImageFileExist = false;
                        }
                        finally
                        {
                            _textureHash = _preset.TextureHash;
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
                    PluginLog.Error(ex, "Error while resizing/loading thumbnail");
                }
                finally
                {
                    _isImageLoading = false;
                }
            }, _closeTokenSource.Token);
        }
    }
}
