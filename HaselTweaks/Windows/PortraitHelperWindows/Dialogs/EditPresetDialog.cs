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

[RegisterScoped, AutoConstruct]
public partial class EditPresetDialog : ConfirmationDialog
{
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;

    private ConfirmationButton _saveButton;
    private string? _name;
    private SavedPreset? _preset;
    private readonly List<Guid> _tags = [];

    private PortraitHelperConfiguration Config => _pluginConfig.Tweaks.PortraitHelper;

    [AutoPostConstruct]
    private void Initialize()
    {
        WindowName = _textService.Translate("PortraitHelperWindows.EditPresetDialog.Title");

        AddButton(_saveButton = new ConfirmationButton(_textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(_textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPreset preset)
    {
        _preset = preset;
        _name = preset.Name;
        _tags.Clear();
        _tags.AddRange(preset.Tags);
        Show();
    }

    public void Close()
    {
        Hide();
        _name = null;
        _preset = null;
        _tags.Clear();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _name != null && _preset != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.EditPresetDialog.Name.Label"));

        ImGui.Spacing();

        ImGui.InputText("##PresetName", ref _name, 30);

        var disabled = string.IsNullOrEmpty(_name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        if (Config.PresetTags.Count != 0)
        {
            ImGui.Spacing();
            ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.EditPresetDialog.Tags.Label"));

            var tagNames = _tags!
                .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : _textService.Translate("PortraitHelperWindows.EditPresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo)
            {
                foreach (var tag in Config.PresetTags)
                {
                    var isSelected = _tags!.Contains(tag.Id);

                    if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                    {
                        if (isSelected)
                        {
                            _tags.Remove(tag.Id);
                        }
                        else
                        {
                            _tags.Add(tag.Id);
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

        foreach (var tag in _tags)
            _preset.Tags.Add(tag);

        _pluginConfig.Save();

        Close();
    }
}
