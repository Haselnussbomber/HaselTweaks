using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility.Raii;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class EditPresetDialog : ConfirmationDialog
{
    private static PortraitHelperConfiguration Config => Service.GetService<Configuration>().Tweaks.PortraitHelper;

    private readonly ConfirmationButton _saveButton;

    private string? _name;
    private SavedPreset? _preset;
    public readonly List<Guid> tags = [];

    public EditPresetDialog() : base(t("PortraitHelperWindows.EditPresetDialog.Title"))
    {
        AddButton(_saveButton = new ConfirmationButton(t("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPreset preset)
    {
        _preset = preset;
        _name = preset.Name;
        tags.Clear();
        tags.AddRange(preset.Tags);
        Show();
    }

    public void Close()
    {
        Hide();
        _name = null;
        _preset = null;
        tags.Clear();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _name != null && _preset != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted(t("PortraitHelperWindows.EditPresetDialog.Name.Label"));

        ImGui.Spacing();

        ImGui.InputText("##PresetName", ref _name, 30);

        var disabled = string.IsNullOrEmpty(_name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        if (Config.PresetTags.Any())
        {
            ImGui.Spacing();
            ImGui.TextUnformatted(t("PortraitHelperWindows.EditPresetDialog.Tags.Label"));

            var tagNames = tags!
                .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : t("PortraitHelperWindows.EditPresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo.Success)
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

        _saveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (_preset == null || string.IsNullOrEmpty(_name?.Trim()))
        {
            Close();
            return;
        }

        _preset.Name = _name.Trim();
        _preset.Tags.Clear();

        foreach (var tag in tags)
            _preset.Tags.Add(tag);

        Service.GetService<Configuration>().Save();

        Close();
    }
}
