using HaselTweaks.ImGuiComponents;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreateTagDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly ConfirmationButton saveButton;

    private string? name;

    public CreateTagDialog() : base("Create Tag")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
        AddButton(new ConfirmationButton("Cancel", Close));
    }

    public void Open()
    {
        name = string.Empty;
        Show();
    }

    public void Close()
    {
        Hide();
        name = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && name != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted("Enter a name for the new tag:");

        ImGui.Spacing();

        ImGui.InputText("##TagName", ref name, 30);

        var disabled = string.IsNullOrEmpty(name.Trim());

        saveButton.Disabled = disabled;

        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }
    }

    private void OnSave()
    {
        if (string.IsNullOrEmpty(name?.Trim()))
        {
            Close();
            return;
        }

        Config.PresetTags.Add(new(name.Trim()));
        Plugin.Config.Save();

        Close();
    }
}
