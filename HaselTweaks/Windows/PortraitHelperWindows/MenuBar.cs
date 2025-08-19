using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using HaselTweaks.Windows.PortraitHelperWindows.MenuBarButtons;

namespace HaselTweaks.Windows.PortraitHelperWindows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class MenuBar : SimpleWindow
{
    private readonly MenuBarState _state;
    private readonly TextService _textService;
    private readonly ResetButton _resetButton;
    private readonly CopyButton _copyButton;
    private readonly PasteButton _pasteButton;
    private readonly AdvancedImportButton _advancedImportButton;
    private readonly AdvancedEditButton _advancedEditButton;
    private readonly SaveAsPresetButton _saveAsPresetButton;
    private readonly PresetBrowserButton _presetBrowserButton;
    private readonly AlignmentToolButton _alignmentToolButton;
    private readonly CreatePresetDialog _createPresetDialog;
    private readonly AlignmentToolRenderer _alignmentToolRenderer;

    [AutoPostConstruct]
    private void Initialize()
    {
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override void OnClose()
    {
        _state.Reset();
        base.OnClose();
    }

    public override bool DrawConditions()
    {
        var agent = AgentBannerEditor.Instance();
        return agent->EditorState != null && agent->IsAddonReady();
    }

    public override void PreDraw()
    {
        _state.SaveInitialPreset();
    }

    public override void Draw()
    {
        if (_state.InitialPreset == null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 2);
            ImGui.Text(_textService.Translate("PortraitHelperWindows.MenuBar.Initializing"));
            UpdatePosition();
            return;
        }

        using var id = ImRaii.PushId("##PortraitHelper_PortraitHelper");

        _resetButton.Draw();
        ImGui.SameLine();
        _copyButton.Draw();
        ImGui.SameLine();
        _pasteButton.Draw();
        ImGui.SameLine();
        _advancedImportButton.Draw();
        ImGui.SameLine();
        _advancedEditButton.Draw();

        ImGuiUtils.VerticalSeparator();

        ImGui.SameLine();
        _saveAsPresetButton.Draw();
        ImGui.SameLine();
        _presetBrowserButton.Draw();

        ImGuiUtils.VerticalSeparator();

        ImGui.SameLine();
        _alignmentToolButton.Draw();

        if (!string.IsNullOrEmpty(_state.PortraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.Text(_state.PortraitName);
        }

        // ---

        UpdatePosition();
        _createPresetDialog.Draw();
    }

    public void UpdatePosition()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var scaledown = 1 / scale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * scaledown;

        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        Position = new(
            addon->X + 4,
            addon->Y + 3 - height * scale
        );

        Size = new(
            (addon->GetScaledWidth(true) - 8) * scaledown,
            height
        );
    }

    public override void PostDraw()
    {
        if (IsOpen)
            _alignmentToolRenderer.Draw();
    }
}
