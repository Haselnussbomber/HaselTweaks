using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Logging;
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

namespace HaselTweaks.Windows.PortraitHelperWindows;

public class PresetCard : IDisposable
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;
    public static Vector2 PortraitSize { get; } = new(576, 960); // native texture size

    private bool isDisposed;
    private readonly CancellationTokenSource closeTokenSource = new();

    private readonly PresetBrowserOverlay overlay;
    private readonly SavedPreset preset;

    private bool isImageLoading;
    private bool doesImageFileExist;
    private bool isImageUpdatePending;

    private string? textureHash;
    private Image<Rgba32>? image;
    private TextureWrap? textureWrap;
    private DateTime lastTextureCheck = DateTime.MinValue;

    private float lastScale;

    private ushort bannerFrame;
    private int? bannerFrameImage;

    private ushort bannerDecoration;
    private int? bannerDecorationImage;

    public PresetCard(PresetBrowserOverlay overlay, SavedPreset preset)
    {
        this.overlay = overlay;
        this.preset = preset;
    }

    public void Dispose()
    {
        if (isDisposed)
            return;

        closeTokenSource.Cancel();
        closeTokenSource.Dispose();

        image?.Dispose();
        image = null;

        textureWrap?.Dispose();
        textureWrap = null;

        isDisposed = true;
    }

    public void Draw(float scale)
    {
        if (isDisposed)
            return;

        Update(scale);

        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(preset.Id.ToString());

        var windowPos = ImGui.GetWindowPos();
        var cursorPos = ImGui.GetCursorPos();
        var center = windowPos + cursorPos + PortraitSize * scale / 2f - new Vector2(0, ImGui.GetScrollY());

        if (isImageLoading)
        {
            ImGuiUtils.DrawLoadingSpinner(center);
        }
        else if (!doesImageFileExist)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(ImGuiUtils.ColorRed)))
                {
                    ImGui.SetCursorPos(center - windowPos - ImGui.CalcTextSize(FontAwesomeIcon.FileImage.ToIconString()) / 2f);
                    ImGui.TextUnformatted(FontAwesomeIcon.FileImage.ToIconString());
                }
            }
        }
        else if (textureWrap != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGui.Image(textureWrap.ImGuiHandle, PortraitSize * scale);
        }

        if (bannerFrameImage != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGuiUtils.DrawIcon(bannerFrameImage.Value, PortraitSize * scale);
        }

        if (bannerDecorationImage != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGuiUtils.DrawIcon(bannerDecorationImage.Value, PortraitSize * scale);
        }

        ImGui.SetCursorPos(cursorPos);

        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(ImGuiUtils.ColorTransparent)))
        {
            using (ImRaii.PushColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.3f))))
            {
                using (ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.2f))))
                {
                    using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0))
                    {
                        ImGui.Button($"##{preset.Id}_Button", PortraitSize * scale);
                    }
                }
            }
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source != null && source.Success)
            {
                ImGui.TextUnformatted($"Moving {preset.Name}");

                var idPtr = Marshal.StringToHGlobalAnsi(preset.Id.ToString());
                ImGui.SetDragDropPayload("MovePresetCard", idPtr, (uint)MemoryUtils.strlen(idPtr));
                Marshal.FreeHGlobal(idPtr);
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target != null && target.Success)
            {
                var payload = ImGui.AcceptDragDropPayload("MovePresetCard");
                unsafe
                {
                    if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                    {
                        var presetId = Marshal.PtrToStringAnsi(payload.Data, payload.DataSize);
                        var oldIndex = Config.Presets.IndexOf((preset) => preset.Id.ToString() == presetId);
                        var newIndex = Config.Presets.IndexOf(preset);
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
                ImGui.TextUnformatted(preset.Name);
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                overlay.Tweak.PresetToState(preset.Preset, ImportFlags.All);
                overlay.Tweak.ChangeView(ViewMode.Normal);
            }
        }

        if (ImGui.BeginPopupContextItem($"{preset.Id}_Popup"))
        {
            if (ImGui.MenuItem("Load Preset"))
            {
                overlay.Tweak.PresetToState(preset.Preset, ImportFlags.All);
                overlay.Tweak.ChangeView(ViewMode.Normal);
            }

            if (ImGui.MenuItem("Edit Preset"))
            {
                overlay.EditPresetDialog.Open(preset);
            }

            if (ImGui.MenuItem("Export to Clipboard"))
            {
                overlay.Tweak.PresetToClipboard(preset.Preset);
            }

            if (image != null && ImGui.BeginMenu("Copy Image"))
            {
                if (ImGui.MenuItem("Everything"))
                {
                    Task.Run(() => CopyImage());
                }

                if (ImGui.MenuItem("Without Frame"))
                {
                    Task.Run(() => CopyImage(CopyImageFlags.NoFrame));
                }

                if (ImGui.MenuItem("Without Decoration"))
                {
                    Task.Run(() => CopyImage(CopyImageFlags.NoDecoration));
                }

                if (ImGui.MenuItem("Without Frame and Decoration"))
                {
                    Task.Run(() => CopyImage(CopyImageFlags.NoFrame | CopyImageFlags.NoDecoration));
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Delete Preset"))
            {
                overlay.DeletePresetDialog.Open(preset);
            }

            ImGui.EndPopup();
        }
    }

    private void CopyImage(CopyImageFlags flags = CopyImageFlags.None)
    {
        if (image == null)
            return;

        using var tempImage = image.Clone();

        if (!flags.HasFlag(CopyImageFlags.NoFrame) && bannerFrameImage != null)
        {
            var iconId = bannerFrameImage.Value;
            var texture = Service.Data.GetFile<TexFile>($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex");
            if (texture != null)
            {
                using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                tempImage.Mutate(x => x.DrawImage(image, 1f));
            }
        }

        if (!flags.HasFlag(CopyImageFlags.NoDecoration) && bannerDecorationImage != null)
        {
            var iconId = bannerDecorationImage.Value;
            var texture = Service.Data.GetFile<TexFile>($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex");
            if (texture != null)
            {
                using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                tempImage.Mutate(x => x.DrawImage(image, 1f));
            }
        }

        _ = ClipboardUtils.SetClipboardImage(tempImage);
    }

    private void Update(float scale)
    {
        if (preset.Preset!.BannerFrame != bannerFrame)
        {
            bannerFrame = preset.Preset.BannerFrame;
            bannerFrameImage = Service.Data.GetExcelSheet<BannerFrame>()?.GetRow(bannerFrame)?.Image;
        }

        if (preset.Preset.BannerDecoration != bannerDecoration)
        {
            bannerDecoration = preset.Preset.BannerDecoration;
            bannerDecorationImage = Service.Data.GetExcelSheet<BannerDecoration>()?.GetRow(bannerDecoration)?.Image;
        }

        if (!isImageLoading && preset.TextureHash != textureHash)
        {
            image?.Dispose();
            image = null;

            textureWrap?.Dispose();
            textureWrap = null;

            if (!string.IsNullOrEmpty(preset.TextureHash) && DateTime.Now - lastTextureCheck > TimeSpan.FromSeconds(1))
            {
                var thumbPath = Config.GetPortraitThumbnailPath(preset.TextureHash);

                if (File.Exists(thumbPath))
                {
                    isImageLoading = true;
                    Task.Run(async () =>
                    {
                        try
                        {
                            image = await Image.LoadAsync<Rgba32>(thumbPath, closeTokenSource.Token);
                            isImageUpdatePending = true;
                            doesImageFileExist = true;
                        }
                        catch (Exception e)
                        {
                            PluginLog.Error("Error while loading thumbnail", e);
                            isImageLoading = false;
                            doesImageFileExist = false;
                        }
                        finally
                        {
                            textureHash = preset.TextureHash;
                        }
                    }, closeTokenSource.Token);
                }

                lastTextureCheck = DateTime.Now;
            }
        }

        if (scale != lastScale)
        {
            isImageUpdatePending = true;
            lastScale = scale;
        }

        if (image != null && isImageUpdatePending)
        {
            textureWrap?.Dispose();
            isImageUpdatePending = false;

            Task.Run(() =>
            {
                try
                {
                    var scaledImage = image.Clone();

                    if (closeTokenSource.IsCancellationRequested)
                        return;

                    scaledImage.Mutate(i => i.Resize((int)(PortraitSize.X * scale), (int)(PortraitSize.Y * scale), KnownResamplers.Lanczos3));

                    if (closeTokenSource.IsCancellationRequested)
                        return;

                    var data = new byte[4 * scaledImage.Width * scaledImage.Height];
                    scaledImage.CopyPixelDataTo(data);

                    if (closeTokenSource.IsCancellationRequested)
                        return;

                    textureWrap = Service.PluginInterface.UiBuilder.LoadImageRaw(data, scaledImage.Width, scaledImage.Height, 4);
                }
                catch (Exception e)
                {
                    PluginLog.Error("Error while resizing/loading thumbnail", e);
                }
                finally
                {
                    isImageLoading = false;
                }
            }, closeTokenSource.Token);
        }
    }
}
