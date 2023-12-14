using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public abstract unsafe class Overlay : Window, IDisposable
{
    private ImRaii.Color? _windowBg;
    private ImRaii.Style? _windowPadding;
    private ImRaii.Color? _windowText;

    protected bool IsWindow { get; set; }

    protected uint DefaultImGuiTextColor { get; set; }

    private static AgentBannerEditor* AgentBannerEditor => GetAgent<AgentBannerEditor>();
    private static AddonBannerEditor* AddonBannerEditor => GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

    protected static PortraitHelperConfiguration Config => Plugin.Config.Tweaks.PortraitHelper;

    protected enum OverlayType
    {
        Window,
        LeftPane
    }

    protected virtual OverlayType Type => OverlayType.Window;

    public Overlay(string name) : base(name)
    {
        DisableWindowSounds = true;
        RespectCloseHotkey = false;

        Update();
        UpdateWindow();
    }

    public void Dispose()
    {
        OnClose();
        GC.SuppressFinalize(this);
    }

    public override void OnClose()
    {
        _windowPadding?.Dispose();
        _windowPadding = null;
        _windowText?.Dispose();
        _windowText = null;
        _windowBg?.Dispose();
        _windowBg = null;

        ToggleUiVisibility(true);

        IsOpen = false;
    }

    public override bool DrawConditions()
    {
        if (AgentBannerEditor == null || AddonBannerEditor == null || AgentBannerEditor->EditorState == null)
            return false;

        var isContextMenuOpen = *(byte*)((nint)AddonBannerEditor + 0x1A1) != 0;
        var isCloseDialogOpen = AgentBannerEditor->EditorState->CloseDialogAddonId != 0;

        return IsOpen && !isContextMenuOpen && !isCloseDialogOpen;
    }

    public override void Update()
    {
        var isWindow = ImGuiHelpers.GlobalScale > 1;
        var isCloseDialogOpen = AgentBannerEditor->EditorState->CloseDialogAddonId != 0;

        ToggleUiVisibility(isWindow || isCloseDialogOpen);

        IsWindow = isWindow;
    }

    public override void PreDraw()
    {
        DefaultImGuiTextColor = ImGui.GetColorU32(ImGuiCol.Text);

        if (!IsWindow)
        {
            if (Type == OverlayType.LeftPane)
            {
                _windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            }

            if (Colors.IsLightTheme)
            {
                _windowText = ImRaii.PushColor(ImGuiCol.Text, (uint)Colors.GetUIColor(2));
            }

            _windowBg = ImRaii.PushColor(ImGuiCol.WindowBg, 0);
        }
    }

    public override void Draw()
    {
        _windowPadding?.Dispose();
        _windowPadding = null;
        _windowBg?.Dispose();
        _windowBg = null;
    }

    public override void PostDraw()
    {
        _windowText?.Dispose();
        _windowText = null;

        UpdateWindow();
    }

    private void UpdateWindow()
    {
        if (!IsWindow)
        {
            Flags |= ImGuiWindowFlags.NoSavedSettings;
            Flags |= ImGuiWindowFlags.NoDecoration;
            Flags |= ImGuiWindowFlags.NoMove;

            if (Type == OverlayType.Window)
            {
                var windowNode = (AtkResNode*)((AtkUnitBase*)AddonBannerEditor)->WindowNode;
                var scale = GetNodeScale(windowNode);

                Position = new Vector2(
                    AddonBannerEditor->AtkUnitBase.X + (windowNode->X + 8) * scale.X,
                    AddonBannerEditor->AtkUnitBase.Y + (windowNode->Y + 40) * scale.Y
                );

                Size = new Vector2(
                    (windowNode->GetWidth() - 16) * scale.X,
                    (windowNode->GetHeight() - 56) * scale.Y
                );
            }
            else if (Type == OverlayType.LeftPane)
            {
                Flags |= ImGuiWindowFlags.AlwaysAutoResize;

                var leftPane = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 20);
                var scale = GetNodeScale(leftPane);

                Position = new Vector2(
                    AddonBannerEditor->AtkUnitBase.X + leftPane->X * scale.X,
                    AddonBannerEditor->AtkUnitBase.Y + leftPane->Y * scale.Y
                );

                Size = new Vector2(
                    leftPane->GetWidth() * scale.X,
                    leftPane->GetHeight() * scale.Y
                );

                SizeCondition = ImGuiCond.Always;
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

    public void ToggleUiVisibility(bool visible)
    {
        var leftPane = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 20);
        leftPane->ToggleVisibility(visible);

        if (Type != OverlayType.LeftPane)
        {
            var rightPane = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 107);
            rightPane->ToggleVisibility(visible);

            var verticalSeparatorNode = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 135);
            verticalSeparatorNode->ToggleVisibility(visible);

            var controlsHint = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 2);
            controlsHint->ToggleVisibility(visible);

            var copyEquimentButton = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 131);
            copyEquimentButton->ToggleVisibility(visible);

            var saveButton = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 133);
            saveButton->ToggleVisibility(visible);

            var closeButton = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 134);
            closeButton->ToggleVisibility(visible);

            var lowerHorizontalLine = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 136);
            lowerHorizontalLine->ToggleVisibility(visible);
        }
    }
}
