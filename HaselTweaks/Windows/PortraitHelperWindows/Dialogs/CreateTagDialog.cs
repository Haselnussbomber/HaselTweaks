using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreateTagDialog : ConfirmationDialog
{
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;
    private readonly ConfirmationButton _saveButton;
    private string? _name;

    public CreateTagDialog(PluginConfig pluginConfig, TextService textService)
        : base(textService.Translate("PortraitHelperWindows.CreateTagDialog.Title"))
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        AddButton(_saveButton = new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
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
        TextService.Draw("PortraitHelperWindows.CreateTagDialog.Name.Label");

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

        PluginConfig.Tweaks.PortraitHelper.PresetTags.Add(new(_name.Trim()));
        PluginConfig.Save();

        Close();
    }
}
