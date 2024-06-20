using System.IO;
using Dalamud.Plugin.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private readonly INotificationManager _notificationManager;
    private readonly PluginConfig _pluginConfig;
    private PresetBrowserOverlay? _presetBrowserOverlay;

    private SavedPreset? _preset;

    public DeletePresetDialog(
        INotificationManager notificationManager,
        PluginConfig pluginConfig)
        : base(t("PortraitHelperWindows.DeletePresetDialog.Title"))
    {
        _notificationManager = notificationManager;
        _pluginConfig = pluginConfig;

        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Delete"), OnDelete));
        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(PresetBrowserOverlay presetBrowserOverlay, SavedPreset? preset)
    {
        _presetBrowserOverlay = presetBrowserOverlay;
        _preset = preset;
        Show();
    }

    public void Close()
    {
        Hide();
        _preset = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _presetBrowserOverlay != null && _preset != null;

    public override void InnerDraw()
        => ImGui.TextUnformatted(t("PortraitHelperWindows.DeletePresetDialog.Prompt", _preset!.Name));

    private void OnDelete()
    {
        if (_preset == null)
        {
            Close();
            return;
        }

        if (_presetBrowserOverlay!.PresetCards.TryGetValue(_preset.Id, out var card))
        {
            _presetBrowserOverlay.PresetCards.Remove(_preset.Id);
            card.Dispose();
        }

        var thumbPath = PortraitHelper.GetPortraitThumbnailPath(_preset.Id);
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

        Close();
    }
}
