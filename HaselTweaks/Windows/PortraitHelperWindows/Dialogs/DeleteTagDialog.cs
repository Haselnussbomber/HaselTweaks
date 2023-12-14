using System.Linq;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class DeleteTagDialog : ConfirmationDialog
{
    private static PortraitHelperConfiguration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly PresetBrowserOverlay _presetBrowserOverlay;

    private SavedPresetTag? _tag;
    private bool _deletePortraits;

    public DeleteTagDialog(PresetBrowserOverlay presetBrowserOverlay) : base(t("PortraitHelperWindows.DeleteTagDialog.Title"))
    {
        _presetBrowserOverlay = presetBrowserOverlay;

        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Delete"), OnDelete));
        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPresetTag tag)
    {
        _tag = tag;
        _deletePortraits = false;
        Show();
    }

    public void Close()
    {
        Hide();
        _tag = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _tag != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted(t("PortraitHelperWindows.DeleteTagDialog.Prompt", _tag!.Name));
        ImGui.Spacing();
        ImGui.Checkbox(t("PortraitHelperWindows.DeleteTagDialog.DeletePortraitsToo.Label"), ref _deletePortraits);
    }

    private void OnDelete()
    {
        if (_tag == null)
        {
            Close();
            return;
        }

        var presets = Config.Presets
            .Where((preset) => preset.Tags.Any((t) => t == _tag.Id))
            .ToArray();

        if (_deletePortraits)
        {
            // remove presets with tag
            foreach (var preset in presets)
            {
                if (_presetBrowserOverlay.PresetCards.TryGetValue(preset.Id, out var card))
                {
                    card.Dispose();
                    _presetBrowserOverlay.PresetCards.Remove(preset.Id);
                }

                Config.Presets.Remove(preset);
            }
        }
        else
        {
            // remove tag from presets
            foreach (var preset in presets)
            {
                preset.Tags.Remove(_tag.Id);
            }
        }

        Config.PresetTags.Remove(_tag);
        Plugin.Config.Save();

        if (_presetBrowserOverlay.SelectedTagId == _tag.Id)
            _presetBrowserOverlay.SelectedTagId = null;

        Close();
    }
}
