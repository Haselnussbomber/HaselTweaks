using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private readonly ILogger _logger;
    private readonly Configuration _pluginConfig;
    private readonly PresetBrowserOverlay _presetBrowserOverlay;

    private SavedPreset? _preset;

    public DeletePresetDialog(PresetBrowserOverlay presetBrowserOverlay, ILogger logger, Configuration pluginConfig)
        : base(t("PortraitHelperWindows.DeletePresetDialog.Title"))
    {
        _presetBrowserOverlay = presetBrowserOverlay;
        _logger = logger;
        _pluginConfig = pluginConfig;

        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Delete"), OnDelete));
        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPreset? preset)
    {
        _preset = preset;
        Show();
    }

    public void Close()
    {
        Hide();
        _preset = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _preset != null;

    public override void InnerDraw()
        => ImGui.TextUnformatted(t("PortraitHelperWindows.DeletePresetDialog.Prompt", _preset!.Name));

    private void OnDelete()
    {
        if (_preset == null)
        {
            Close();
            return;
        }

        if (_presetBrowserOverlay.PresetCards.TryGetValue(_preset.Id, out var card))
        {
            _presetBrowserOverlay.PresetCards.Remove(_preset.Id);
            card.Dispose();
        }

        _preset.Delete(_logger, _pluginConfig);

        Close();
    }
}
