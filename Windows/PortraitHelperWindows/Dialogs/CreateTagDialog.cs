using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreateTagDialog : ConfirmationDialog
{
    private static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    private readonly PresetBrowserOverlay presetBrowserOverlay;
    private readonly ConfirmationButton saveButton;

    private string? name;

    public CreateTagDialog(PresetBrowserOverlay overlay) : base("Create Tag")
    {
        presetBrowserOverlay = overlay;

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

    public override void InnerDraw()
    {
        ImGui.Text("Enter a name for the new tag:");

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
        if (name == null)
        {
            Close();
            return;
        }

        var tag = new SavedPresetTag(name.Trim());
        Config.PresetTags.Add(tag);
        Plugin.Config.Save();

        Close();
    }
}
