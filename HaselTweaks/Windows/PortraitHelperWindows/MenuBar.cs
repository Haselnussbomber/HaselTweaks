using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselCommon.Windowing;
using HaselCommon.Windowing.Interfaces;
using HaselTweaks.Config;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public unsafe class MenuBar : SimpleWindow
{
    private PortraitHelperConfiguration Config => PluginConfig.Tweaks.PortraitHelper;

    private PortraitPreset? InitialPreset;
    private string PortraitName = string.Empty;

    private readonly IServiceProvider ServiceProvider;
    private readonly ILogger Logger;
    private readonly IDalamudPluginInterface PluginInterface;
    private readonly PluginConfig PluginConfig;
    private readonly TextService TextService;
    private readonly BannerUtils BannerUtils;
    private IServiceScope? ServiceScope;

    private CreatePresetDialog? CreatePresetDialog;
    private AdvancedImportOverlay? AdvancedImportOverlay;
    private AdvancedEditOverlay? AdvancedEditOverlay;
    private PresetBrowserOverlay? PresetBrowserOverlay;
    private AlignmentToolSettingsOverlay? AlignmentToolSettingsOverlay;

    public MenuBar(
        ILogger<MenuBar> logger,
        IServiceProvider serviceProvider,
        IDalamudPluginInterface pluginInterface,
        IWindowManager windowManager,
        PluginConfig pluginConfig,
        TextService textService,
        BannerUtils bannerUtils)
        : base(windowManager, "Portrait Helper MenuBar")
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        PluginInterface = pluginInterface;
        PluginConfig = pluginConfig;
        TextService = textService;
        BannerUtils = bannerUtils;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override void OnOpen()
    {
        ServiceScope = ServiceProvider.CreateScope();
        base.OnOpen();
    }

    public override void OnClose()
    {
        CreatePresetDialog?.Hide();
        InitialPreset = null;
        PortraitName = string.Empty;
        CloseOverlays();
        ServiceScope?.Dispose();
        ServiceScope = null;
        base.OnClose();
    }

    public void CloseOverlays()
    {
        AdvancedImportOverlay?.Dispose();
        AdvancedImportOverlay = null;
        AdvancedEditOverlay?.Dispose();
        AdvancedEditOverlay = null;
        PresetBrowserOverlay?.Dispose();
        PresetBrowserOverlay = null;
        AlignmentToolSettingsOverlay?.Dispose();
        AlignmentToolSettingsOverlay = null;
    }

    public override bool DrawConditions()
    {
        var agent = AgentBannerEditor.Instance();
        return agent->EditorState != null && agent->IsAddonReady();
    }

    public override void PreDraw()
    {
        var agent = AgentBannerEditor.Instance();

        if (InitialPreset != null || !agent->EditorState->CharaView->CharaViewPortraitCharacterLoaded)
            return;

        InitialPreset = PortraitPreset.FromState();

        if (agent->EditorState->OpenType == AgentBannerEditorState.EditorOpenType.AdventurerPlate)
        {
            PortraitName = TextService.GetAddonText(14761) ?? "Adventurer Plate";
        }
        else if (agent->EditorState->GearsetId > -1)
        {
            var gearset = RaptureGearsetModule.Instance()->GetGearset(agent->EditorState->GearsetId);
            if (gearset != null)
            {
                PortraitName = $"{TextService.GetAddonText(756) ?? "Gear Set"} #{gearset->Id + 1}: {gearset->NameString}";
            }
        }
    }

    public override void Draw()
    {
        if (InitialPreset == null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 2);
            TextService.Draw("PortraitHelperWindows.MenuBar.Initializing");
            UpdatePosition();
            return;
        }

        using var id = ImRaii.PushId("##PortraitHelper_PortraitHelper");

        var agent = AgentBannerEditor.Instance();

        if (!agent->EditorState->HasDataChanged)
        {
            ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, TextService.GetAddonText(4830) ?? "Reset", disabled: true);
        }
        else if (ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, TextService.GetAddonText(4830) ?? "Reset"))
        {
            InitialPreset.ToState(Logger, BannerUtils, ImportFlags.All);
            agent->EditorState->SetHasChanged(false);
        }

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("Copy", FontAwesomeIcon.Copy, TextService.Translate("PortraitHelperWindows.MenuBar.ExportToClipboard.Label"))) // GetAddonText(100) ?? "Copy"
        {
            PortraitPreset.FromState()?.ToClipboard(Logger);
        }

        ImGui.SameLine();
        if (PortraitHelper.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("Paste", FontAwesomeIcon.Paste, TextService.Translate("PortraitHelperWindows.MenuBar.ImportFromClipboard.Label"), disabled: true); // GetAddonText(101) ?? "Paste"
        }
        else
        {
            if (ImGuiUtils.IconButton("Paste", FontAwesomeIcon.Paste, TextService.Translate("PortraitHelperWindows.MenuBar.ImportFromClipboardAllSettings.Label")))
            {
                PortraitHelper.ClipboardPreset.ToState(Logger, BannerUtils, ImportFlags.All);
                CloseOverlays();
            }
        }

        ImGui.SameLine();
        if (PortraitHelper.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label"), disabled: true);
        }
        else if (AdvancedImportOverlay != null && AdvancedImportOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FileImport, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label")))
            {
                AdvancedImportOverlay?.Dispose();
                AdvancedImportOverlay = null;
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label")))
        {
            CloseOverlays();
            AdvancedImportOverlay ??= ServiceScope!.ServiceProvider.GetRequiredService<AdvancedImportOverlay>();
            AdvancedImportOverlay.Open(this);
        }

        ImGui.SameLine();
        if (AdvancedEditOverlay != null && AdvancedEditOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FilePen, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label")))
            {
                AdvancedEditOverlay?.Dispose();
                AdvancedEditOverlay = null;
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedEdit", FontAwesomeIcon.FilePen, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label")))
        {
            CloseOverlays();
            AdvancedEditOverlay ??= ServiceScope!.ServiceProvider.GetRequiredService<AdvancedEditOverlay>();
            AdvancedEditOverlay.Open();
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("SaveAsPreset", FontAwesomeIcon.Download, TextService.Translate("PortraitHelperWindows.MenuBar.SaveAsPreset.Label")))
        {
            CreatePresetDialog ??= ServiceScope!.ServiceProvider.GetRequiredService<CreatePresetDialog>();
            CreatePresetDialog.Open(PortraitName, PortraitPreset.FromState(), BannerUtils.GetCurrentCharaViewImage());
        }

        ImGui.SameLine();
        if (PresetBrowserOverlay != null && PresetBrowserOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal2", FontAwesomeIcon.List, TextService.Translate("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label")))
            {
                PresetBrowserOverlay?.Dispose();
                PresetBrowserOverlay = null;
            }
        }
        else if (ImGuiUtils.IconButton("ViewModePresetBrowser", FontAwesomeIcon.List, TextService.Translate("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label")))
        {
            CloseOverlays();
            PresetBrowserOverlay ??= ServiceScope!.ServiceProvider.GetRequiredService<PresetBrowserOverlay>();
            PresetBrowserOverlay.Open(this);
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (AlignmentToolSettingsOverlay != null && AlignmentToolSettingsOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ToggleAlignmentToolOff", FontAwesomeIcon.Hashtag, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label.CloseSettings")))
            {
                if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                {
                    AlignmentToolSettingsOverlay?.Dispose();
                    AlignmentToolSettingsOverlay = null;
                }
                else
                {
                    Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                    PluginConfig.Save();
                }
            }
        }
        else if (ImGuiUtils.IconButton("ToggleAlignmentToolOn", FontAwesomeIcon.Hashtag, TextService.Translate("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label.OpenSettings")))
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                CloseOverlays();
                AlignmentToolSettingsOverlay ??= ServiceScope!.ServiceProvider.GetRequiredService<AlignmentToolSettingsOverlay>();
                AlignmentToolSettingsOverlay.Open();
            }
            else
            {
                Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                PluginConfig.Save();
            }
        }

        if (!string.IsNullOrEmpty(PortraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.TextUnformatted(PortraitName);
        }

        UpdatePosition();

        CreatePresetDialog?.Draw();
    }

    public void UpdatePosition()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var scaledown = 1 / scale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * scaledown;

        var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

        Position = new(
            addon->AtkUnitBase.X + 4,
            addon->AtkUnitBase.Y + 3 - height * scale
        );

        Size = new(
            (addon->AtkUnitBase.GetScaledWidth(true) - 8) * scaledown,
            height
        );
    }

    public override void PostDraw()
    {
        if (IsOpen && DrawConditions() && Config.ShowAlignmentTool)
        {
            var addon = GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

            var rightPanel = GetNode<AtkResNode>(&addon->AtkUnitBase, 107);
            var charaView = GetNode<AtkResNode>(&addon->AtkUnitBase, 130);
            var scale = GetNodeScale(charaView);

            var position = new Vector2(
                addon->AtkUnitBase.X + rightPanel->X * scale.X,
                addon->AtkUnitBase.Y + rightPanel->Y * scale.Y
            );

            var size = new Vector2(
                charaView->GetWidth() * scale.X,
                charaView->GetHeight() * scale.Y
            );

            ImGui.SetNextWindowPos(position);
            ImGui.SetNextWindowSize(size);

            if (!(ImGuiHelpers.GlobalScale <= 1 && ((AdvancedImportOverlay != null && AdvancedImportOverlay.IsOpen) || (PresetBrowserOverlay != null && PresetBrowserOverlay.IsOpen))))
            {
                ImGui.Begin("AlignmentTool", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs);

                var drawList = ImGui.GetWindowDrawList();

                if (Config.AlignmentToolVerticalLines > 0)
                {
                    var x = size.X / (Config.AlignmentToolVerticalLines + 1);

                    for (var i = 1; i <= Config.AlignmentToolVerticalLines + 1; i++)
                    {
                        drawList.AddLine(
                            position + new Vector2(i * x, 0),
                            position + new Vector2(i * x, size.Y),
                            ImGui.ColorConvertFloat4ToU32(Config.AlignmentToolVerticalColor)
                        );
                    }
                }

                if (Config.AlignmentToolHorizontalLines > 0)
                {
                    var y = size.Y / (Config.AlignmentToolHorizontalLines + 1);

                    for (var i = 1; i <= Config.AlignmentToolHorizontalLines + 1; i++)
                    {
                        drawList.AddLine(
                            position + new Vector2(0, i * y),
                            position + new Vector2(size.X, i * y),
                            ImGui.ColorConvertFloat4ToU32(Config.AlignmentToolHorizontalColor)
                        );
                    }
                }

                ImGui.End();
            }
        }
    }
}
