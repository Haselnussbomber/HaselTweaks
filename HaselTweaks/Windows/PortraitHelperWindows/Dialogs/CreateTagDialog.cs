using HaselTweaks.ImGuiComponents;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterScoped, AutoConstruct]
public partial class CreateTagDialog : ConfirmationDialog
{
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private ConfirmationButton _saveButton;
    private string? _name;

    [AutoPostConstruct]
    private void Initialize()
    {
        WindowName = _textService.Translate("PortraitHelperWindows.CreateTagDialog.Title");

        AddButton(_saveButton = new ConfirmationButton(_textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(_textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open()
    {
        _name = string.Empty;
        Show();
    }

    public void Close()
    {
        Hide();
        _name = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _name != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.CreateTagDialog.Name.Label"));

        ImGui.Spacing();

        ImGui.InputText("##TagName", ref _name, 30);

        var disabled = string.IsNullOrEmpty(_name.Trim());

        _saveButton.Disabled = disabled;

        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }
    }

    private void OnSave()
    {
        if (string.IsNullOrEmpty(_name?.Trim()))
        {
            Close();
            return;
        }

        _pluginConfig.Tweaks.PortraitHelper.PresetTags.Add(new(_name.Trim()));
        _pluginConfig.Save();

        Close();
    }
}
