using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

[RegisterScoped]
public class RenameTagDialog : ConfirmationDialog
{
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;

    private readonly ConfirmationButton SaveButton;
    private SavedPresetTag? Tag = null;
    private string? Name;

    public RenameTagDialog(PluginConfig pluginConfig, TextService textService)
        : base(textService.Translate("PortraitHelperWindows.RenameTagDialog.Title"))
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        AddButton(SaveButton = new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Save"), OnSave));
        AddButton(new ConfirmationButton(textService.Translate("ConfirmationButtonWindow.Cancel"), Close));
    }

    public void Open(SavedPresetTag tag)
    {
        Tag = tag;
        Name = tag.Name;
        Show();
    }

    public void Close()
    {
        Hide();
        Tag = null;
        Name = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && Tag != null && Name != null;

    public override void InnerDraw()
    {
        TextService.Draw("PortraitHelperWindows.RenameTagDialog.Name.Label", Tag!.Name);

        ImGui.Spacing();

        ImGui.InputText("##TagName", ref Name, 30);

        var disabled = string.IsNullOrEmpty(Name.Trim()) && Name.Trim() != Tag!.Name.Trim();

        SaveButton.Disabled = disabled;

        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }
    }

    private void OnSave()
    {
        if (Tag == null || string.IsNullOrEmpty(Name?.Trim()))
        {
            Close();
            return;
        }

        Tag.Name = Name.Trim();
        PluginConfig.Save();

        Close();
    }
}
