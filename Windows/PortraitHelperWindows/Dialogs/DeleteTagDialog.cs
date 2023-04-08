using System.Linq;
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
    private bool deletePortraits;

    public DeleteTagDialog(PresetBrowserOverlay presetBrowserOverlay) : base("Delete Tag")
    {
        this.presetBrowserOverlay = presetBrowserOverlay;

        AddButton(new ConfirmationButton("Delete", OnDelete));
        AddButton(new ConfirmationButton("Cancel", Close));
    }

    public void Open(SavedPresetTag tag)
    {
        this.tag = tag;
        deletePortraits = false;
        Show();
    }

    public void Close()
    {
        Hide();
        tag = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && tag != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted($"Do you really want to delete the tag \"{tag!.Name}\"?");
        ImGui.Spacing();
        ImGui.Checkbox("Delete portraits too", ref deletePortraits);
    }

    private void OnDelete()
    {
        if (tag == null)
        {
            Close();
            return;
        }

        var presets = Config.Presets
            .Where((preset) => preset.Tags.Any((t) => t == tag.Id))
            .ToArray();

        if (deletePortraits)
        {
            // remove presets with tag
            foreach (var preset in presets)
            {
                if (presetBrowserOverlay.PresetCards.TryGetValue(preset.Id, out var card))
                {
                    card.Dispose();
                    presetBrowserOverlay.PresetCards.Remove(preset.Id);
                }

                Config.Presets.Remove(preset);
            }
        }
        else
        {
            // remove tag from presets
            foreach (var preset in presets)
            {
                preset.Tags.Remove(tag.Id);
            }
        }

        Config.PresetTags.Remove(tag);
        Plugin.Config.Save();

        if (presetBrowserOverlay.SelectedTagId == tag.Id)
            presetBrowserOverlay.SelectedTagId = null;

        Close();
    }
}
