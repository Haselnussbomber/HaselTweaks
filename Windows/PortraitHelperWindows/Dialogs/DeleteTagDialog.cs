using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeleteTagDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly PresetBrowserOverlay presetBrowserOverlay;
    private SavedPresetTag? tag;

    public DeleteTagDialog(PresetBrowserOverlay presetBrowserOverlay) : base("Delete Tag")
    {
        this.presetBrowserOverlay = presetBrowserOverlay;

        AddButton(new ConfirmationButton("Delete", OnDelete));
        AddButton(new ConfirmationButton("Cancel", Hide));
    }

    public void Open(SavedPresetTag tag)
    {
        this.tag = tag;
        Show();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && tag != null;

    public override void InnerDraw()
        => ImGui.TextUnformatted($"Do you really want to delete the tag \"{tag!.Name}\"?");

    private void OnDelete()
    {
        foreach (var preset in Config.Presets)
        {
            preset.Tags.Remove(tag!.Id);
        }

        Config.PresetTags.Remove(tag!);
        Plugin.Config.Save();

        if (presetBrowserOverlay.SelectedTagId == tag!.Id)
            presetBrowserOverlay.SelectedTagId = null;

        presetBrowserOverlay.PresetCards.Remove(tag!.Id);
    }
}
