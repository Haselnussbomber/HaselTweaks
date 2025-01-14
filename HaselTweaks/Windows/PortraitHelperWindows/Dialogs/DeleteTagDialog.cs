using System.Linq;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterScoped]
public class DeleteTagDialog : ConfirmationDialog
{
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    private PresetBrowserOverlay? PresetBrowserOverlay;

    private SavedPresetTag? Tag;
    private bool DeletePortraits;

    public DeleteTagDialog(PluginConfig pluginConfig, TextService textService)
        : base(textService.Translate("PortraitHelperWindows.DeleteTagDialog.Title"))
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Delete"), OnDelete));
        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(PresetBrowserOverlay presetBrowserOverlay, SavedPresetTag tag)
    {
        PresetBrowserOverlay = presetBrowserOverlay;
        Tag = tag;
        DeletePortraits = false;
        Show();
    }

    public void Close()
    {
        Hide();
        Tag = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && Tag != null;

    public override void InnerDraw()
    {
        TextService.Draw("PortraitHelperWindows.DeleteTagDialog.Prompt", Tag!.Name);
        ImGui.Spacing();
        ImGui.Checkbox(TextService.Translate("PortraitHelperWindows.DeleteTagDialog.DeletePortraitsToo.Label"), ref DeletePortraits);
    }

    private void OnDelete()
    {
        if (Tag == null)
        {
            Close();
            return;
        }

        var config = PluginConfig.Tweaks.PortraitHelper;

        var presets = config.Presets
            .Where((preset) => preset.Tags.Any((t) => t == Tag.Id))
            .ToArray();

        if (DeletePortraits)
        {
            // remove presets with tag
            foreach (var preset in presets)
            {
                if (PresetBrowserOverlay!.PresetCards.TryGetValue(preset.Id, out var card))
                {
                    card.Dispose();
                    PresetBrowserOverlay.PresetCards.Remove(preset.Id);
                }

                config.Presets.Remove(preset);
            }
        }
        else
        {
            // remove tag from presets
            foreach (var preset in presets)
            {
                preset.Tags.Remove(Tag.Id);
            }
        }

        config.PresetTags.Remove(Tag);
        PluginConfig.Save();

        if (PresetBrowserOverlay!.SelectedTagId == Tag.Id)
            PresetBrowserOverlay.SelectedTagId = null;

        Close();
    }
}
