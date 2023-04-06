using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class RenameTagDialog : ConfirmationDialog
{
    private readonly ConfirmationButton saveButton;

    private SavedPresetTag? tag = null;
    private string name = string.Empty;

    public RenameTagDialog() : base("Rename Tag")
    {
        AddButton(saveButton = new ConfirmationButton("Save", OnSave));
        AddButton(new ConfirmationButton("Cancel", Hide));
    }

    public void Open(SavedPresetTag tag)
    {
        this.tag = tag;
        name = tag.Name;
        Show();
    }

    public override bool DrawCondition()
        => base.DrawCondition() && tag != null;

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
        tag!.Name = name.Trim();
        Plugin.Config.Save();
    }
}
