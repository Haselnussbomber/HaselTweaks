using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private readonly PresetBrowserOverlay presetBrowserOverlay;

    private SavedPreset? preset;

    public DeletePresetDialog(PresetBrowserOverlay presetBrowserOverlay) : base("Delete Preset")
    {
        this.presetBrowserOverlay = presetBrowserOverlay;

        AddButton(new ConfirmationButton("Delete", OnDelete));
        AddButton(new ConfirmationButton("Cancel", Close));
    }

    public void Open(SavedPreset? preset)
    {
        this.preset = preset;
        Show();
    }

    public void Close()
    {
        Hide();
        preset = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && preset != null;

    public override void InnerDraw()
        => ImGuiUtils.TextUnformattedDisabled($"Do you really want to delete the preset \"{preset!.Name}\"?");

    private void OnDelete()
    {
        if (preset == null)
        {
            Close();
            return;
        }

        if (presetBrowserOverlay.PresetCards.TryGetValue(preset.Id, out var card))
        {
            presetBrowserOverlay.PresetCards.Remove(preset.Id);
            card.Dispose();
        }

        preset.Delete();

        Close();
    }
}
