using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;

namespace HaselTweaks.Windows.PortraitHelperWindows;

[AutoConstruct]
public abstract unsafe partial class Overlay : SimpleWindow, IDisposable, IOverlay
{
    private readonly PluginConfig _pluginConfig;
    private readonly ExcelService _excelService;
    private readonly MenuBarState _state;

    private readonly ImRaii.Style _windowPadding = new();
    private readonly ImRaii.Color _windowBg = new();
    private readonly ImRaii.Color _windowText = new();

    protected uint DefaultImGuiTextColor { get; set; }

    private bool _isDisposed;

    public bool IsWindow { get; set; }
    public virtual OverlayType Type => OverlayType.Full;

    [AutoPostConstruct]
    private void Initialize()
    {
        DisableWindowSounds = true;
        RespectCloseHotkey = false;

        IsWindow = ImGuiHelpers.GlobalScaleSafe > 1;
        UpdateWindow();
    }

    public override void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true; // do this first, or else... recursion
            OnClose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public override void OnClose()
    {
        _windowPadding.Dispose();
        _windowBg.Dispose();
        _windowText.Dispose();

        ToggleUiVisibility(true);

        _state.CloseOverlay(GetType());

        base.OnClose();
    }

    public override bool DrawConditions()
    {
        var agnet = AgentBannerEditor.Instance();
        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        if (agnet == null || addon == null || agnet->EditorState == null || !addon->IsReady)
            return false;

        var isContextMenuOpen = addon->NumOpenPopups != 0;
        var isCloseDialogOpen = agnet->EditorState->CloseDialogAddonId != 0;

        return IsOpen && !isContextMenuOpen && !isCloseDialogOpen;
    }

    public override void Update()
    {
        if (!IsOpen)
            return;

        IsWindow = ImGuiHelpers.GlobalScale > 1;

        var agent = AgentBannerEditor.Instance();
        var isCloseDialogOpen =
            agent != null
            && agent->EditorState != null
            && agent->EditorState->CloseDialogAddonId != 0;

        ToggleUiVisibility(IsWindow || isCloseDialogOpen);
    }

    public override void PreDraw()
    {
        DefaultImGuiTextColor = ImGui.GetColorU32(ImGuiCol.Text);

        if (!IsWindow)
        {
            if (Type == OverlayType.LeftPane)
                _windowPadding.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            if (Misc.IsLightTheme && _excelService.TryGetRow<UIColor>(2, out var uiColor))
                _windowText.Push(ImGuiCol.Text, Color.FromABGR(uiColor.Dark).ToUInt());

            _windowBg.Push(ImGuiCol.WindowBg, 0);
        }
    }

    public override void Draw()
    {
        _windowPadding.Dispose();
        _windowBg.Dispose();
    }

    public override void PostDraw()
    {
        _windowText.Dispose();

        UpdateWindow();
    }

    private void UpdateWindow()
    {
        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);
        if (addon == null)
            return;

        if (!IsWindow)
        {
            Flags |= ImGuiWindowFlags.NoSavedSettings;
            Flags |= ImGuiWindowFlags.NoDecoration;
            Flags |= ImGuiWindowFlags.NoMove;
            SizeCondition = ImGuiCond.Always;

            var scale = addon->Scale;

            if (Type == OverlayType.Full)
            {
                var windowNode = addon->WindowNode;

                Position = new Vector2(
                    addon->X + (windowNode->X + 8) * scale,
                    addon->Y + (windowNode->Y + 40) * scale
                );

                Size = new Vector2(
                    (windowNode->GetWidth() - 16) * scale,
                    (windowNode->GetHeight() - 56) * scale
                );
            }
            else if (Type == OverlayType.LeftPane)
            {
                Flags |= ImGuiWindowFlags.AlwaysAutoResize;

                var leftPane = addon->GetNodeById(20);

                Position = new Vector2(
                    addon->X + leftPane->X * scale,
                    addon->Y + leftPane->Y * scale
                );

                Size = new Vector2(
                    leftPane->GetWidth() * scale,
                    leftPane->GetHeight() * scale
                );

                SizeConstraints = null;
            }
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoSavedSettings;
            Flags &= ~ImGuiWindowFlags.NoDecoration;
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.AlwaysAutoResize;

            SizeCondition = ImGuiCond.Appearing;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(400, 500),
                MaximumSize = new Vector2(4069),
            };

            Position = null;
            Size = null;
        }
    }

    private void ToggleUiVisibility(bool visible)
    {
        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);
        if (addon == null)
            return;

        addon->GetNodeById(20)->ToggleVisibility(visible); // LeftPane

        if (Type != OverlayType.LeftPane)
        {
            addon->GetNodeById(107)->ToggleVisibility(visible); // RightPane
            addon->GetNodeById(135)->ToggleVisibility(visible); // VerticalSeparatorNode
            addon->GetNodeById(2)->ToggleVisibility(visible); // ControlsHint
            addon->GetNodeById(131)->ToggleVisibility(visible); // CopyEquimentButton
            addon->GetNodeById(133)->ToggleVisibility(visible); // SaveButton
            addon->GetNodeById(134)->ToggleVisibility(visible); // CloseButton
            addon->GetNodeById(136)->ToggleVisibility(visible); // LowerHorizontalLine
        }
    }
}
