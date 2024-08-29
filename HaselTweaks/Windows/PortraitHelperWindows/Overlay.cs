using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselCommon.Windowing;
using HaselTweaks.Config;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Interfaces;
using HaselTweaks.Tweaks;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public abstract unsafe class Overlay : SimpleWindow, IDisposable, IOverlay
{
    protected readonly PluginConfig PluginConfig;
    protected readonly ExcelService ExcelService;

    private readonly ImRaii.Style WindowPadding = new();
    private readonly ImRaii.Color WindowBg = new();
    private readonly ImRaii.Color WindowText = new();

    protected uint DefaultImGuiTextColor { get; set; }

    protected PortraitHelperConfiguration Config => PluginConfig.Tweaks.PortraitHelper;

    public bool IsWindow { get; set; }
    public virtual OverlayType Type => OverlayType.Window;

    public Overlay(
        WindowManager windowManager,
        PluginConfig pluginConfig,
        ExcelService excelService,
        string name)
        : base(windowManager, name)
    {
        PluginConfig = pluginConfig;
        ExcelService = excelService;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;

        UpdateWindow();
    }

    public override void OnClose()
    {
        WindowPadding.Dispose();
        WindowBg.Dispose();
        WindowText.Dispose();

        ToggleUiVisibility(true);

        base.OnClose();
    }

    public override bool DrawConditions()
    {
        var agnet = AgentBannerEditor.Instance();
        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        if (agnet == null || addon == null || agnet->EditorState == null || !addon->AtkUnitBase.IsReady)
            return false;

        var isContextMenuOpen = addon->AtkUnitBase.NumOpenPopups != 0;
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
            {
                WindowPadding.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            }

            if (Colors.IsLightTheme)
            {
                WindowText.Push(ImGuiCol.Text, (uint)ExcelService.GetRow<UIColor>(2)!.GetForegroundColor());
            }

            WindowBg.Push(ImGuiCol.WindowBg, 0);
        }
    }

    public override void Draw()
    {
        WindowPadding.Dispose();
        WindowBg.Dispose();
    }

    public override void PostDraw()
    {
        WindowText.Dispose();

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

            if (Type == OverlayType.Window)
            {
                var windowNode = (AtkResNode*)((AtkUnitBase*)addon)->WindowNode;
                var scale = GetNodeScale(windowNode);

                Position = new Vector2(
                    addon->AtkUnitBase.X + (windowNode->X + 8) * scale.X,
                    addon->AtkUnitBase.Y + (windowNode->Y + 40) * scale.Y
                );

                Size = new Vector2(
                    (windowNode->GetWidth() - 16) * scale.X,
                    (windowNode->GetHeight() - 56) * scale.Y
                );
            }
            else if (Type == OverlayType.LeftPane)
            {
                Flags |= ImGuiWindowFlags.AlwaysAutoResize;

                var leftPane = GetNode<AtkResNode>(&addon->AtkUnitBase, 20);
                var scale = GetNodeScale(leftPane);

                Position = new Vector2(
                    addon->AtkUnitBase.X + leftPane->X * scale.X,
                    addon->AtkUnitBase.Y + leftPane->Y * scale.Y
                );

                Size = new Vector2(
                    leftPane->GetWidth() * scale.X,
                    leftPane->GetHeight() * scale.Y
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

        var leftPane = GetNode<AtkResNode>(&addon->AtkUnitBase, 20);
        leftPane->ToggleVisibility(visible);

        if (Type != OverlayType.LeftPane)
        {
            var rightPane = GetNode<AtkResNode>(&addon->AtkUnitBase, 107);
            rightPane->ToggleVisibility(visible);

            var verticalSeparatorNode = GetNode<AtkResNode>(&addon->AtkUnitBase, 135);
            verticalSeparatorNode->ToggleVisibility(visible);

            var controlsHint = GetNode<AtkResNode>(&addon->AtkUnitBase, 2);
            controlsHint->ToggleVisibility(visible);

            var copyEquimentButton = GetNode<AtkResNode>(&addon->AtkUnitBase, 131);
            copyEquimentButton->ToggleVisibility(visible);

            var saveButton = GetNode<AtkResNode>(&addon->AtkUnitBase, 133);
            saveButton->ToggleVisibility(visible);

            var closeButton = GetNode<AtkResNode>(&addon->AtkUnitBase, 134);
            closeButton->ToggleVisibility(visible);

            var lowerHorizontalLine = GetNode<AtkResNode>(&addon->AtkUnitBase, 136);
            lowerHorizontalLine->ToggleVisibility(visible);
        }
    }
}
