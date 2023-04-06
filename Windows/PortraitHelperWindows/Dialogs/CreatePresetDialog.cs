using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreatePresetDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly ConfirmationButton saveButton;

    private string? name;
    private PortraitPreset? Preset;
    private SavedTexture? Texture;
    private readonly List<Guid> SelectedTags = new();

    public CreatePresetDialog() : base("Save as Preset")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
    }

    public void Open(string name, PortraitPreset? preset, SavedTexture? texture)
    {
        this.name = name;
        Preset = preset;
        Texture = texture;
        SelectedTags.Clear();
        Show();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && Preset != null;

    public override void InnerDraw()
    {
        ImGui.Text("Enter a name for the new preset:");
        ImGui.Spacing();
        ImGui.InputText("##PresetName", ref name, 100);

        var disabled = string.IsNullOrEmpty(name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        ImGui.Spacing();

        ImGui.Text("Select Tags (optional):");
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
        if (Preset == null || Texture == null || name == null || string.IsNullOrEmpty(name.Trim()))
            return;

        Config.Presets.Add(new(name.Trim(), Preset, SelectedTags, Texture));
        Plugin.Config.Save();
    }
}
