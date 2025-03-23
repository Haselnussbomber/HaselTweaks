using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterScoped, AutoConstruct]
public partial class RenameTagDialog : ConfirmationDialog
{
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;

    private ConfirmationButton _saveButton;
    private SavedPresetTag? _tag = null;
    private string? _name;

    [AutoPostConstruct]
    public void Initialize()
    {
        WindowName = _textService.Translate("PortraitHelperWindows.RenameTagDialog.Title");

        AddButton(_saveButton = new ConfirmationButton(_textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(_textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPresetTag tag)
    {
        _tag = tag;
        _name = tag.Name;
        Show();
    }

    public void Close()
    {
        Hide();
        _tag = null;
        _name = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _tag != null && _name != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.RenameTagDialog.Name.Label", _tag!.Name));

        ImGui.Spacing();

        ImGui.InputText("##TagName", ref _name, 30);

        var disabled = string.IsNullOrEmpty(_name.Trim()) && _name.Trim() != _tag!.Name.Trim();

        _saveButton.Disabled = disabled;

        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }
    }

    private void OnSave()
    {
        if (_tag == null || string.IsNullOrEmpty(_name?.Trim()))
        {
            Close();
            return;
        }

        _tag.Name = _name.Trim();
        _pluginConfig.Save();

        Close();
    }
}
