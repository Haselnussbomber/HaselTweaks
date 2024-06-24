using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class EditPresetDialog : ConfirmationDialog
{
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    private readonly ConfirmationButton SaveButton;
    private string? Name;
    private SavedPreset? Preset;
    public readonly List<Guid> Tags = [];

    private PortraitHelperConfiguration Config => PluginConfig.Tweaks.PortraitHelper;

    public EditPresetDialog(PluginConfig pluginConfig, TextService textService)
        : base(textService.Translate("PortraitHelperWindows.EditPresetDialog.Title"))
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        AddButton(SaveButton = new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPreset preset)
    {
        Preset = preset;
        Name = preset.Name;
        Tags.Clear();
        Tags.AddRange(preset.Tags);
        Show();
    }

    public void Close()
    {
        Hide();
        Name = null;
        Preset = null;
        Tags.Clear();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && Name != null && Preset != null;

    public override void InnerDraw()
    {
        TextService.Draw("PortraitHelperWindows.EditPresetDialog.Name.Label");

        ImGui.Spacing();

        ImGui.InputText("##PresetName", ref Name, 30);

        var disabled = string.IsNullOrEmpty(Name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        if (Config.PresetTags.Count != 0)
        {
            ImGui.Spacing();
            TextService.Draw("PortraitHelperWindows.EditPresetDialog.Tags.Label");

            var tagNames = Tags!
                .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : TextService.Translate("PortraitHelperWindows.EditPresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo.Success)
            {
                foreach (var tag in Config.PresetTags)
                {
                    var isSelected = Tags!.Contains(tag.Id);

                    if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                    {
                        if (isSelected)
                        {
                            Tags.Remove(tag.Id);
                        }
                        else
                        {
                            Tags.Add(tag.Id);
                        }
                    }
                }
            }
        }

        SaveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (Preset == null || string.IsNullOrEmpty(Name?.Trim()))
        {
            Close();
            return;
        }

        Preset.Name = Name.Trim();
        Preset.Tags.Clear();

        foreach (var tag in Tags)
            Preset.Tags.Add(tag);

        PluginConfig.Save();

        Close();
    }
}
