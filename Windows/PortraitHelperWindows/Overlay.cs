using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public abstract unsafe class Overlay : Window
{
    public PortraitHelper Tweak { get; init; }

    private bool? IsWindow { get; set; } = null;

    public AgentBannerEditor* AgentBannerEditor => Tweak.AgentBannerEditor;
    public AddonBannerEditor* AddonBannerEditor => Tweak.AddonBannerEditor;

    protected static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    protected enum OverlayType
    {
        Window,
        LeftPane
    }

    protected virtual OverlayType Type => OverlayType.Window;

    public Overlay(string name, PortraitHelper tweak) : base(name)
    {
        Tweak = tweak;

        base.DisableWindowSounds = true;
        base.RespectCloseHotkey = false;
    }

    public override bool DrawConditions()
    {
        if (AgentBannerEditor == null || AddonBannerEditor == null || AgentBannerEditor->EditorState == null)
            return false;

        var isContextMenuOpen = *(byte*)((nint)AddonBannerEditor + 0x1A1) != 0;
        var isCloseDialogOpen = AgentBannerEditor->EditorState->CloseDialogAddonId != 0;

        return IsOpen && !isContextMenuOpen && !isCloseDialogOpen;
    }

    public override void OnClose()
    {
        ToggleUiVisibility(true);
    }

    public override void Update()
    {
        var isWindow = ImGui.GetIO().FontGlobalScale > 1;

        ToggleUiVisibility(!DrawConditions() || isWindow);

        if (IsWindow == null || IsWindow != isWindow)
        {
            if (!isWindow)
            {
                base.Flags |= ImGuiWindowFlags.NoSavedSettings;
                base.Flags |= ImGuiWindowFlags.NoDecoration;
                base.Flags |= ImGuiWindowFlags.NoMove;

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
                    var leftPane = GetNode((AtkUnitBase*)AddonBannerEditor, 20);
                    var scale = GetNodeScale(leftPane);

                    Position = new Vector2(
                        AddonBannerEditor->AtkUnitBase.X + leftPane->X * scale.X,
                        AddonBannerEditor->AtkUnitBase.Y + leftPane->Y * scale.Y
                    );

                    Size = new Vector2(
                        leftPane->GetWidth() * scale.X,
                        leftPane->GetHeight() * scale.Y
                    );
                }
            }
            else
            {
                base.Flags &= ImGuiWindowFlags.NoSavedSettings;
                base.Flags &= ImGuiWindowFlags.NoDecoration;
                base.Flags &= ImGuiWindowFlags.NoMove;

                SizeCondition = ImGuiCond.Appearing;
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(400, 500),
                    MaximumSize = new Vector2(4069),
                };

                Position = null;
                Size = null;
            }

            IsWindow = isWindow;
        }
    }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xFF313131);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 4));
    }

    public override void PostDraw()
    {
        // ImGui.PopStyleVar(); // call in Draw()!!!!
        ImGui.PopStyleColor();
    }

    public void ToggleUiVisibility(bool visible)
    {
        if (visible || ImGui.GetIO().FontGlobalScale <= 1)
        {
            var leftPane = GetNode((AtkUnitBase*)AddonBannerEditor, 20);
            SetVisibility(leftPane, visible);

            if (Type != OverlayType.LeftPane)
            {
                var verticalSeparatorNode = GetNode((AtkUnitBase*)AddonBannerEditor, 135);
                SetVisibility(verticalSeparatorNode, visible);

                var controlsHint = GetNode((AtkUnitBase*)AddonBannerEditor, 2);
                SetVisibility(controlsHint, visible);
            }
        }
    }
}
