using HaselTweaks.ImGuiComponents;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreateTagDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly ConfirmationButton _saveButton;

    private string? _name;

    public CreateTagDialog() : base(t("PortraitHelperWindows.CreateTagDialog.Title"))
    {
        AddButton(_saveButton = new ConfirmationButton(t("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(t("ConfirmationButtonWindow.Cancel"), Close));
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
        ImGui.TextUnformatted(t("PortraitHelperWindows.CreateTagDialog.Name.Label"));

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

        Config.PresetTags.Add(new(_name.Trim()));
        Plugin.Config.Save();

        Close();
    }
}
