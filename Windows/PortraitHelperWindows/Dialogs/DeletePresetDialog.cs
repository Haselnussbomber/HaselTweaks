using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private readonly PresetBrowserOverlay _presetBrowserOverlay;

    private SavedPreset? _preset;

    public DeletePresetDialog(PresetBrowserOverlay presetBrowserOverlay) : base(t("PortraitHelperWindows.DeletePresetDialog.Title"))
    {
        _presetBrowserOverlay = presetBrowserOverlay;

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
        => ImGuiUtils.TextUnformattedDisabled(t("PortraitHelperWindows.DeletePresetDialog.Prompt", _preset!.Name));

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

        _preset.Delete();

        Close();
    }
}
