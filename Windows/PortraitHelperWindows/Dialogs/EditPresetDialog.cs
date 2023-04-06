using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
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

    private string name = string.Empty;
    private SavedPreset? preset;
    public List<Guid> SelectedTags = new();

    public EditPresetDialog() : base("Edit Preset")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
        AddButton(new ConfirmationButton("Cancel", Hide));
    }

    public void Open(SavedPreset preset)
    {
        this.preset = preset;
        name = preset.Name;
        SelectedTags.Clear();
        SelectedTags.AddRange(preset.Tags);
        Show();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && preset != null;

    public override void InnerDraw()
    {
        ImGui.Text("Name:");

        ImGui.Spacing();

        ImGui.InputText("##PresetName", ref name, 30);

        var disabled = string.IsNullOrEmpty(name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        ImGui.Spacing();

        ImGui.Text("Tags:");
        ImGui.Spacing();

        var tagNames = SelectedTags
            .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
            .Where(name => !string.IsNullOrEmpty(name));

        var preview = tagNames.Any() ? string.Join(", ", tagNames) : "None";

        using var tagsCombo = ImRaii.Combo("##PresetTag", preview);
        if (tagsCombo.Success)
        {
            foreach (var tag in Config.PresetTags)
            {
                var isSelected = SelectedTags.Contains(tag.Id);

                if (ImGui.TreeNodeEx($"{tag.Name}##PresetTag{tag.Id}", (isSelected ? ImGuiTreeNodeFlags.Selected : 0) | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth))
                {
                    if (ImGui.IsItemClicked())
                    {
                        if (isSelected)
                        {
                            SelectedTags.Remove(tag.Id);
                        }
                        else
                        {
                            SelectedTags.Add(tag.Id);
                        }
                    }

                    if (isSelected)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(8);

                        using (ImRaii.PushFont(UiBuilder.IconFont))
                        {
                            ImGui.TextUnformatted(FontAwesomeIcon.Check.ToIconString());
                        }
                    }

                    ImGui.TreePop();
                }
            }
        }
        tagsCombo.Dispose();

        saveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (preset == null || name == null || string.IsNullOrEmpty(name.Trim()))
            return;

        preset.Name = name.Trim();
        preset.Tags.Clear();
        preset.Tags.AddRange(SelectedTags);
        Plugin.Config.Save();
    }
}
