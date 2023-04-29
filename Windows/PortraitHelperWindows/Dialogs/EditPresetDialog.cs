using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Raii;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class EditPresetDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly ConfirmationButton saveButton;

    private string? name;
    private SavedPreset? preset;
    public readonly List<Guid> tags = new();

    public EditPresetDialog() : base("Edit Preset")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
        AddButton(new ConfirmationButton("Cancel", Close));
    }

    public void Open(SavedPreset preset)
    {
        this.preset = preset;
        name = preset.Name;
        tags.Clear();
        tags.AddRange(preset.Tags);
        Show();
    }

    public void Close()
    {
        Hide();
        name = null;
        preset = null;
        tags.Clear();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && name != null && preset != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted("Name:");

        ImGui.Spacing();

        ImGui.InputText("##PresetName", ref name, 30);

        var disabled = string.IsNullOrEmpty(name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        if (Config.PresetTags.Any())
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("Tags:");

            var tagNames = tags!
                .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : "None";

            ImGui.Spacing();
            using (var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge))
            {
                if (tagsCombo != null && tagsCombo.Success)
                {
                    foreach (var tag in Config.PresetTags)
                    {
                        var isSelected = tags!.Contains(tag.Id);

                        if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                        {
                            if (isSelected)
                            {
                                tags.Remove(tag.Id);
                            }
                            else
                            {
                                tags.Add(tag.Id);
                            }
                        }
                    }
                }
            }
        }

        saveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (preset == null || string.IsNullOrEmpty(name?.Trim()))
        {
            Close();
            return;
        }

        preset.Name = name.Trim();
        preset.Tags.Clear();

        foreach (var tag in tags)
            preset.Tags.Add(tag);

        Plugin.Config.Save();

        Close();
    }
}
