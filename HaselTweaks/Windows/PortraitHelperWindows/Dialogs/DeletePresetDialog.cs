using System.IO;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterSingleton, AutoConstruct]
public partial class DeletePresetDialog
{
    private readonly INotificationManager _notificationManager;
    private readonly TextService _textService;
    private readonly PluginConfig _pluginConfig;
    private readonly ThumbnailService _thumbnailService;

    private bool _shouldOpen;
    private SavedPreset? _preset;

    public void Open(SavedPreset preset)
    {
        _preset = preset;
        _shouldOpen = true;
    }

    public void Draw()
    {
        if (_preset == null)
            return;

        var title = _textService.Translate("PortraitHelperWindows.DeletePresetDialog.Title");

        if (_shouldOpen)
        {
            ImGui.OpenPopup(title);
            _shouldOpen = false;
        }

        if (!ImGui.IsPopupOpen(title))
            return;

        // Always center this window when appearing
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

        using var modal = ImRaiiExt.PopupModal(title, ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal) return;

        ImGui.Text(_textService.Translate("PortraitHelperWindows.DeletePresetDialog.Prompt", _preset.Name));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var combinedButtonWidths = ImGui.GetStyle().ItemSpacing.X
            + MathF.Max(Constants.DialogButtonMinWidth, ImGuiHelpers.GetButtonSize(_textService.Translate("ConfirmationButtonWindow.Delete")).X)
            + MathF.Max(Constants.DialogButtonMinWidth, ImGuiHelpers.GetButtonSize(_textService.Translate("ConfirmationButtonWindow.Cancel")).X);

        ImGuiUtils.PushCursorX((ImGui.GetContentRegionAvail().X - combinedButtonWidths) / 2f);

        if (ImGui.Button(_textService.Translate("ConfirmationButtonWindow.Delete"), new Vector2(120, 0)))
        {
            var thumbPath = _thumbnailService.GetPortraitThumbnailPath(_preset.Id);
            if (File.Exists(thumbPath))
            {
                try
                {
                    File.Delete(thumbPath);
                }
                catch (Exception ex)
                {
                    _notificationManager.AddNotification(new()
                    {
                        Title = "Could not delete preset",
                        Content = ex.Message,
                    });
                }
            }

            _pluginConfig.Tweaks.PortraitHelper.Presets.Remove(_preset);
            _pluginConfig.Save();

            _preset = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SetItemDefaultFocus();
        ImGui.SameLine();
        if (ImGui.Button(_textService.Translate("ConfirmationButtonWindow.Cancel"), new Vector2(120, 0)))
        {
            _preset = null;
            ImGui.CloseCurrentPopup();
        }
    }
}
