using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private SavedPreset? preset;

    public DeletePresetDialog() : base("Delete Preset")
    {
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
        => ImGui.TextUnformatted($"Do you really want to delete the preset \"{preset!.Name}\"?");

    private void OnDelete()
    {
        if (preset == null)
        {
            Close();
            return;
        }

        preset.Delete();

        Close();
    }
}
