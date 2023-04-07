using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Raii;
using Dalamud.Logging;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public unsafe class PresetCard
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;
    public static Vector2 PortraitSize = new(153, 256); // ~ (576, 960) * 0.2664f

    private readonly PresetBrowserOverlay overlay;
    private readonly Guid id;

    private string textureHash = string.Empty;
    private TextureWrap? textureWrap;

    private bool warningLogged;

    private ushort bannerFrame;
    private int? bannerFrameImage;

    private ushort bannerDecoration;
    private int? bannerDecorationImage;

    public PresetCard(PresetBrowserOverlay overlay, Guid id)
    {
        this.overlay = overlay;
        this.id = id;
    }

    public void Draw()
    {
        var preset = Config.Presets.FirstOrDefault((preset) => preset.Id == id);
        if (preset == null)
        {
            if (!warningLogged)
            {
                PluginLog.Warning($"Could not find Preset {id} for PresetCard???");
                warningLogged = true;
            }
            return;
        }
        if (preset.Preset == null)
        {
            if (!warningLogged)
            {
                PluginLog.Warning($"Removing SavedPreset {id}: Preset is null"); // 😳
                preset.Delete();
                warningLogged = true;
            }
            return;
        }

        if (preset.Preset.BannerFrame != bannerFrame)
        {
            bannerFrame = preset.Preset.BannerFrame;
            bannerFrameImage = Service.Data.GetExcelSheet<BannerFrame>()?.GetRow(bannerFrame)?.Image;
        }

        if (preset.Preset.BannerDecoration != bannerDecoration)
        {
            bannerDecoration = preset.Preset.BannerDecoration;
            bannerDecorationImage = Service.Data.GetExcelSheet<BannerDecoration>()?.GetRow(bannerDecoration)?.Image;
        }

        if (preset.TextureHash != textureHash)
        {
            textureHash = preset.TextureHash;
            textureWrap?.Dispose();

            if (!string.IsNullOrEmpty(textureHash))
            {
                var thumbPath = Config.GetPortraitThumbnailPath(textureHash);

                // TODO: re-create if not found, maybe with loading spinner in right side of menu bar
                if (File.Exists(thumbPath))
                {
                    using var image = Image.Load<Rgba32>(thumbPath);
                    var data = new byte[sizeof(Rgba32) * image.Width * image.Height];
                    image.CopyPixelDataTo(data);
                    textureWrap = Service.PluginInterface.UiBuilder.LoadImageRaw(data, image.Width, image.Height, 4);
                }
                else
                {
                    textureWrap = null;
                }
            }
            else
            {
                textureWrap = null;
            }
        }

        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(preset.Id.ToString());

        var windowPos = ImGui.GetWindowPos();
        var cursorPos = ImGui.GetCursorPos();

        //  TODO: fallback behind image :sadface:
        if (textureWrap != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGui.Image(textureWrap.ImGuiHandle, PortraitSize);
        }

        if (bannerFrameImage != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGuiUtils.DrawIcon(bannerFrameImage.Value, PortraitSize);
        }

        if (bannerDecorationImage != null)
        {
            ImGui.SetCursorPos(cursorPos);
            ImGuiUtils.DrawIcon(bannerDecorationImage.Value, PortraitSize);
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
                        ImGui.Button($"##{preset.Id}_Button", PortraitSize);
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
            if (ImGui.Selectable("Load Preset"))
            {
                overlay.Tweak.PresetToState(preset.Preset, ImportFlags.All);
                overlay.Tweak.ChangeView(ViewMode.Normal);
            }

            if (ImGui.Selectable("Edit Preset"))
            {
                overlay.EditPresetDialog.Open(preset);
            }

            if (ImGui.Selectable("Export to Clipboard"))
            {
                overlay.Tweak.PresetToClipboard(preset.Preset);
            }

            ImGui.Separator();

            if (ImGui.Selectable("Delete Preset"))
            {
                overlay.DeletePresetDialog.Open(preset);
            }

            ImGui.EndPopup();
        }
    }
}
