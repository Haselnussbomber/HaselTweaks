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

namespace HaselTweaks.Windows.PortraitHelperWindows;

public unsafe class PresetCard
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;
    private static Vector2 NativePortraitSize = new(576, 960);
    private static readonly float Scale = 0.3f;
    private static Vector2 PortraitSize = NativePortraitSize * Scale;

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

        if (preset.Texture.Hash != textureHash)
        {
            textureHash = preset.Texture.Hash;
            textureWrap?.Dispose();

            var rgbData = preset.Texture.Data;
            var rgbaData = new byte[rgbData.Length + rgbData.Length / 3];
            for (int i = 0, j = 0; i < rgbData.Length; i += 3, j += 4)
            {
                rgbaData[j] = rgbData[i];           // red channel
                rgbaData[j + 1] = rgbData[i + 1];   // green channel
                rgbaData[j + 2] = rgbData[i + 2];   // blue channel
                rgbaData[j + 3] = 255;              // alpha channel
            }

            textureWrap = Service.PluginInterface.UiBuilder.LoadImageRaw(rgbaData, preset.Texture.Width, preset.Texture.Height, 4);
        }

        var style = ImGui.GetStyle();

        using var child = ImRaii.Child(
            preset.Id.ToString(),
            PortraitSize + new Vector2(0, style.ItemSpacing.Y + ImGui.GetTextLineHeight()),
            false,
            ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoScrollbar
        );

        var windowPos = ImGui.GetWindowPos();
        var cursorPos = ImGui.GetCursorPos();

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
        ImGui.Dummy(PortraitSize);
        if (ImGui.IsItemHovered())
        {
            ImGui.GetWindowDrawList().AddRectFilled(
                windowPos + cursorPos,
                windowPos + cursorPos + PortraitSize,
                ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, 0.2f))
            );

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
