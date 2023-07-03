using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private readonly PresetBrowserOverlay _presetBrowserOverlay;

    private SavedPreset? _preset;

    public DeletePresetDialog(PresetBrowserOverlay presetBrowserOverlay) : base("Delete Preset")
    {
        _presetBrowserOverlay = presetBrowserOverlay;

        AddButton(new ConfirmationButton("Delete", OnDelete));
        AddButton(new ConfirmationButton("Cancel", Close));
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
        => ImGuiUtils.TextUnformattedDisabled($"Do you really want to delete the preset \"{_preset!.Name}\"?");

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
