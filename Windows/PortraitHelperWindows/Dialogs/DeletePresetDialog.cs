using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeletePresetDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private SavedPreset? Preset;

    public DeletePresetDialog() : base("Delete Preset")
    {
        AddButton(new ConfirmationButton("Delete", OnDelete));
        AddButton(new ConfirmationButton("Cancel", Hide));
    }

    public void Open(SavedPreset? preset)
    {
        Preset = preset;
        Show();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && Preset != null;

    public override void InnerDraw()
        => ImGui.TextUnformatted($"Do you really want to delete the preset \"{Preset!.Name}\"?");

    private void OnDelete()
    {
        if (Preset == null)
            return;

        Config.Presets.Remove(Preset);
        Plugin.Config.Save();
    }
}
