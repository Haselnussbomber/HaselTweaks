using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
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
        UpdatePosition();
    }

    public override void Draw()
    {
        if (_state.InitialPreset == null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 2);
            ImGui.Text(_textService.Translate("PortraitHelperWindows.MenuBar.Initializing"));
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

        _createPresetDialog.Draw();
    }

    public void UpdatePosition()
    {
        if (!TryGetAddon<AtkUnitBase>(AgentId.BannerEditor, out var addon))
            return;

        var scale = ImGuiHelpers.GlobalScale;
        var inverseScale = 1 / scale;
        var addonWidth = addon->GetScaledWidth(true);
        var width = (addonWidth - 8) * inverseScale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * inverseScale;
        var offset = new Vector2(4, 3 - height * scale);

        Position = ImGui.GetMainViewport().Pos + addon->Position + offset;
        Size = new(width, height);
    }

    public override void PostDraw()
    {
        if (IsOpen)
            _alignmentToolRenderer.Draw();
    }
}
