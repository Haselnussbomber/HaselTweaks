using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterSingleton, AutoConstruct]
public partial class EditPresetDialog
{
    private readonly TextService _textService;
    private readonly PluginConfig _pluginConfig;

    private bool _shouldOpen;
    private string _name;
    private SavedPreset? _preset;
    private readonly List<Guid> _tags = [];

    public void Open(SavedPreset preset)
    {
        _preset = preset;
        _name = preset.Name;
        _tags.Clear();
        _tags.AddRange(preset.Tags);
        _shouldOpen = true;
    }

    public void Draw()
    {
        if (_preset == null)
            return;

        var title = _textService.Translate("PortraitHelperWindows.EditPresetDialog.Title");

        if (_shouldOpen)
        {
            ImGui.OpenPopup(title);
            _shouldOpen = false;
        }

        if (!ImGui.IsPopupOpen(title))
            return;

        // Always center this window when appearing
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f, 0.5f));

        using var modal = ImRaiiExt.PopupModal(title, ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal) return;

        ImGui.Text(_textService.Translate("PortraitHelperWindows.EditPresetDialog.Name.Label"));

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        var name = _name;
        if (ImGui.InputText("##PresetName", ref name, Constants.PresetNameMaxLength))
            _name = name;

        var availableTags = _pluginConfig.Tweaks.PortraitHelper.PresetTags;
        if (availableTags.Count != 0)
        {
            ImGui.Spacing();
            ImGui.Text(_textService.Translate("PortraitHelperWindows.EditPresetDialog.Tags.Label"));

            var tagNames = _tags!
                .Select(id => availableTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : _textService.Translate("PortraitHelperWindows.EditPresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo)
            {
                foreach (var tag in availableTags)
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

        var disabled = string.IsNullOrWhiteSpace(_name);
        var shouldSave = !disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var combinedButtonWidths = ImGui.GetStyle().ItemSpacing.X
            + MathF.Max(Constants.DialogButtonMinWidth, ImGuiHelpers.GetButtonSize(_textService.Translate("ConfirmationButtonWindow.Save")).X)
            + MathF.Max(Constants.DialogButtonMinWidth, ImGuiHelpers.GetButtonSize(_textService.Translate("ConfirmationButtonWindow.Cancel")).X);

        ImGuiUtils.PushCursorX((ImGui.GetContentRegionAvail().X - combinedButtonWidths) / 2f);

        using (ImRaii.Disabled(disabled))
        {
            if (ImGui.Button(_textService.Translate("ConfirmationButtonWindow.Save"), new Vector2(120, 0)) || shouldSave)
            {
                _preset.Name = _name;
                _preset.Tags.Clear();

                foreach (var tag in _tags)
                    _preset.Tags.Add(tag);

                _pluginConfig.Save();
                _preset = null;
                _name = string.Empty;
                ImGui.CloseCurrentPopup();
            }
        }

        ImGui.SetItemDefaultFocus();
        ImGui.SameLine();
        if (ImGui.Button(_textService.Translate("ConfirmationButtonWindow.Cancel"), new Vector2(120, 0)))
        {
            _preset = null;
            ImGui.CloseCurrentPopup();
        }
    }
}
