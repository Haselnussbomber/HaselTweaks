using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public unsafe class MenuBar : SimpleWindow
{
    private static AgentBannerEditor* AgentBannerEditor => GetAgent<AgentBannerEditor>();
    private static AddonBannerEditor* AddonBannerEditor => GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

    private PortraitHelperConfiguration Config => PluginConfig.Tweaks.PortraitHelper;

    private PortraitPreset? _initialPreset;
    private string _portraitName = string.Empty;

    private readonly CreatePresetDialog SaveAsPresetDialog;
    private readonly ILogger<PortraitHelper> Logger;
    private readonly DalamudPluginInterface PluginInterface;
    private readonly PluginConfig PluginConfig;
    private readonly AdvancedImportOverlay AdvancedImportOverlay;
    private readonly AdvancedEditOverlay AdvancedEditOverlay;
    private readonly PresetBrowserOverlay PresetBrowserOverlay;
    private readonly AlignmentToolSettingsOverlay AlignmentToolSettingsOverlay;

    public MenuBar(
        ILogger<PortraitHelper> logger,
        DalamudPluginInterface pluginInterface,
        WindowManager windowManager,
        PluginConfig pluginConfig,
        TranslationManager translationManager,
        AdvancedImportOverlay advancedImportOverlay,
        AdvancedEditOverlay advancedEditOverlay,
        PresetBrowserOverlay presetBrowserOverlay,
        AlignmentToolSettingsOverlay alignmentToolSettingsOverlay
        ) : base(windowManager, "Portrait Helper MenuBar")
    {
        Logger = logger;
        PluginInterface = pluginInterface;
        PluginConfig = pluginConfig;
        AdvancedImportOverlay = advancedImportOverlay;
        AdvancedEditOverlay = advancedEditOverlay;
        PresetBrowserOverlay = presetBrowserOverlay;
        AlignmentToolSettingsOverlay = alignmentToolSettingsOverlay;

        SaveAsPresetDialog = new(pluginConfig, translationManager);

        PresetBrowserOverlay.MenuBar = this;
        advancedImportOverlay.MenuBar = this;

        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override void OnClose()
    {
        base.OnClose();
        SaveAsPresetDialog.Hide();
        _initialPreset = null;
        _portraitName = string.Empty;
        CloseOverlays();
    }

    public void CloseOverlays()
    {
        AdvancedImportOverlay.Close();
        AdvancedEditOverlay.Close();
        PresetBrowserOverlay.Close();
        AlignmentToolSettingsOverlay.Close();
    }

    public override bool DrawConditions()
        => AgentBannerEditor->EditorState != null && AgentBannerEditor->IsAddonReady();

    public override void PreDraw()
    {
        if (_initialPreset != null || !AgentBannerEditor->EditorState->CharaView->CharaViewPortraitCharacterLoaded)
            return;

        _initialPreset = PortraitPreset.FromState();

        if (AgentBannerEditor->EditorState->OpenType == AgentBannerEditorState.EditorOpenType.AdventurerPlate)
        {
            _portraitName = GetAddonText(14761) ?? "Adventurer Plate";
        }
        else if (AgentBannerEditor->EditorState->GearsetId > -1)
        {
            var gearset = RaptureGearsetModule.Instance()->GetGearset(AgentBannerEditor->EditorState->GearsetId);
            if (gearset != null)
            {
                _portraitName = $"{GetAddonText(756) ?? "Gear Set"} #{gearset->Id + 1}: {gearset->NameString}";
            }
        }
    }

    public override void Draw()
    {
        if (_initialPreset == null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 2);
            ImGui.TextUnformatted(t("PortraitHelperWindows.MenuBar.Initializing"));
            UpdatePosition();
            return;
        }

        using var id = ImRaii.PushId("##PortraitHelper_PortraitHelper");

        if (!AgentBannerEditor->EditorState->HasDataChanged)
        {
            ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, GetAddonText(4830) ?? "Reset", disabled: true);
        }
        else if (ImGuiUtils.IconButton("Reset", FontAwesomeIcon.Undo, GetAddonText(4830) ?? "Reset"))
        {
            _initialPreset.ToState(Logger, ImportFlags.All);
            AgentBannerEditor->EditorState->SetHasChanged(false);
        }

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("Copy", FontAwesomeIcon.Copy, t("PortraitHelperWindows.MenuBar.ExportToClipboard.Label"))) // GetAddonText(100) ?? "Copy"
        {
            PortraitPreset.FromState()?.ToClipboard(Logger);
        }

        ImGui.SameLine();
        if (PortraitHelper.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("Paste", FontAwesomeIcon.Paste, t("PortraitHelperWindows.MenuBar.ImportFromClipboard.Label"), disabled: true); // GetAddonText(101) ?? "Paste"
        }
        else
        {
            if (ImGuiUtils.IconButton("Paste", FontAwesomeIcon.Paste, t("PortraitHelperWindows.MenuBar.ImportFromClipboardAllSettings.Label")))
            {
                PortraitHelper.ClipboardPreset.ToState(Logger, ImportFlags.All);
                CloseOverlays();
            }
        }

        ImGui.SameLine();
        if (PortraitHelper.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, t("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label"), disabled: true);
        }
        else if (AdvancedImportOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FileImport, t("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label")))
            {
                AdvancedImportOverlay.Close();
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, t("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label")))
        {
            CloseOverlays();
            AdvancedImportOverlay.Open();
        }

        ImGui.SameLine();
        if (AdvancedEditOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FilePen, t("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label")))
            {
                AdvancedEditOverlay.Close();
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedEdit", FontAwesomeIcon.FilePen, t("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label")))
        {
            CloseOverlays();
            AdvancedEditOverlay.Open();
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("SaveAsPreset", FontAwesomeIcon.Download, t("PortraitHelperWindows.MenuBar.SaveAsPreset.Label")))
        {
            SaveAsPresetDialog.Open(_portraitName, PortraitPreset.FromState(), PortraitHelper.GetCurrentCharaViewImage(PluginInterface));
        }

        ImGui.SameLine();
        if (PresetBrowserOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal2", FontAwesomeIcon.List, t("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label")))
            {
                PresetBrowserOverlay.Close();
            }
        }
        else if (ImGuiUtils.IconButton("ViewModePresetBrowser", FontAwesomeIcon.List, t("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label")))
        {
            CloseOverlays();
            PresetBrowserOverlay.Open();
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (AlignmentToolSettingsOverlay.IsOpen)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ToggleAlignmentToolOff", FontAwesomeIcon.Hashtag, t("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label.CloseSettings")))
            {
                if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                {
                    AlignmentToolSettingsOverlay.Close();
                }
                else
                {
                    Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                    PluginConfig.Save();
                }
            }
        }
        else if (ImGuiUtils.IconButton("ToggleAlignmentToolOn", FontAwesomeIcon.Hashtag, t("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label.OpenSettings")))
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                CloseOverlays();
                AlignmentToolSettingsOverlay.Open();
            }
            else
            {
                Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                PluginConfig.Save();
            }
        }

        if (!string.IsNullOrEmpty(_portraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.TextUnformatted(_portraitName);
        }

        UpdatePosition();

        SaveAsPresetDialog.Draw();
    }

    public void UpdatePosition()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var scaledown = 1 / scale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * scaledown;

        Position = new(
            AddonBannerEditor->AtkUnitBase.X + 4,
            AddonBannerEditor->AtkUnitBase.Y + 3 - height * scale
        );

        Size = new(
            (AddonBannerEditor->AtkUnitBase.GetScaledWidth(true) - 8) * scaledown,
            height
        );
    }

    public override void PostDraw()
    {
        if (IsOpen && DrawConditions() && Config.ShowAlignmentTool)
        {
            var rightPanel = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 107);
            var charaView = GetNode<AtkResNode>(&AddonBannerEditor->AtkUnitBase, 130);
            var scale = GetNodeScale(charaView);

            var position = new Vector2(
                AddonBannerEditor->AtkUnitBase.X + rightPanel->X * scale.X,
                AddonBannerEditor->AtkUnitBase.Y + rightPanel->Y * scale.Y
            );

            var size = new Vector2(
                charaView->GetWidth() * scale.X,
                charaView->GetHeight() * scale.Y
            );

            ImGui.SetNextWindowPos(position);
            ImGui.SetNextWindowSize(size);

            if (!(ImGuiHelpers.GlobalScale <= 1 && (AdvancedImportOverlay.IsOpen || PresetBrowserOverlay.IsOpen)))
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
