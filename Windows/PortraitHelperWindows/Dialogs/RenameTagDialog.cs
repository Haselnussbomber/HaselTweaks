using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class RenameTagDialog : ConfirmationDialog
{
    private readonly ConfirmationButton saveButton;

    private SavedPresetTag? tag = null;
    private string? name;

    public RenameTagDialog() : base("Rename Tag")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
        AddButton(new ConfirmationButton("Cancel", Close));
    }

    public void Open(SavedPresetTag tag)
    {
        this.tag = tag;
        name = tag.Name;
        Show();
    }

    public void Close()
    {
        Hide();
        tag = null;
        name = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && tag != null && name != null;

    public override void InnerDraw()
    {
        ImGui.Text($"Enter a new name for tag \"{tag!.Name}\":");

        ImGui.Spacing();

        ImGui.InputText("##TagName", ref name, 30);

        var disabled = string.IsNullOrEmpty(name.Trim()) && name.Trim() != tag.Name.Trim();

        saveButton.Disabled = disabled;

        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }
    }

    private void OnSave()
    {
        if (tag == null || string.IsNullOrEmpty(name?.Trim()))
        {
            Close();
            return;
        }

        tag.Name = name.Trim();
        Plugin.Config.Save();

        Close();
    }
}
