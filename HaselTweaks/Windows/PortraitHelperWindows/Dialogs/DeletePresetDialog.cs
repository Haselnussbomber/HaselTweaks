using System.IO;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Extensions;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterScoped]
public class DeletePresetDialog : ConfirmationDialog
{
    private readonly IDalamudPluginInterface PluginInterface;
    private readonly INotificationManager NotificationManager;
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    private PresetBrowserOverlay? PresetBrowserOverlay;
    private SavedPreset? Preset;

    public DeletePresetDialog(
        IDalamudPluginInterface pluginInterface,
        INotificationManager notificationManager,
        PluginConfig pluginConfig,
        TextService textService)
        : base(textService.Translate("PortraitHelperWindows.DeletePresetDialog.Title"))
    {
        PluginInterface = pluginInterface;
        NotificationManager = notificationManager;
        PluginConfig = pluginConfig;
        TextService = textService;

        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Delete"), OnDelete));
        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(PresetBrowserOverlay presetBrowserOverlay, SavedPreset? preset)
    {
        PresetBrowserOverlay = presetBrowserOverlay;
        Preset = preset;
        Show();
    }

    public void Close()
    {
        Hide();
        Preset = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && PresetBrowserOverlay != null && Preset != null;

    public override void InnerDraw()
        => TextService.Draw("PortraitHelperWindows.DeletePresetDialog.Prompt", Preset!.Name);

    private void OnDelete()
    {
        if (Preset == null)
        {
            Close();
            return;
        }

        if (PresetBrowserOverlay!.PresetCards.TryGetValue(Preset.Id, out var card))
        {
            PresetBrowserOverlay.PresetCards.Remove(Preset.Id);
            card.Dispose();
        }

        var thumbPath = PluginInterface.GetPortraitThumbnailPath(Preset.Id);
        if (File.Exists(thumbPath))
        {
            try
            {
                File.Delete(thumbPath);
            }
            catch (Exception ex)
            {
                NotificationManager.AddNotification(new()
                {
                    Title = "Could not delete preset",
                    Content = ex.Message,
                });
            }
        }

        PluginConfig.Tweaks.PortraitHelper.Presets.Remove(Preset);
        PluginConfig.Save();

        Close();
    }
}
