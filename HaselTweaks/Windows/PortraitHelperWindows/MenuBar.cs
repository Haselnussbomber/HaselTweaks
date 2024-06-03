using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using HaselTweaks.Windows.PortraitHelperWindows.Overlays;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public unsafe class MenuBar : Window
{
    private static PortraitHelperConfiguration Config => Service.GetService<Configuration>().Tweaks.PortraitHelper;

    private static AgentBannerEditor* AgentBannerEditor => GetAgent<AgentBannerEditor>();
    private static AddonBannerEditor* AddonBannerEditor => GetAddon<AddonBannerEditor>(AgentId.BannerEditor);

    private PortraitPreset? _initialPreset;
    private string _portraitName = string.Empty;

    private readonly CreatePresetDialog _saveAsPresetDialog = new();

    public MenuBar() : base("Portrait Helper MenuBar")
    {
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;
        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override void OnClose()
    {
        _saveAsPresetDialog.Hide();
        _initialPreset = null;
        _portraitName = string.Empty;
    }

    public override bool DrawConditions()
        => AgentBannerEditor->EditorState != null;

    public override void PreDraw()
    {
        if (_initialPreset != null || !AgentBannerEditor->EditorState->CharaView->CharacterLoaded)
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
            _initialPreset.ToState(ImportFlags.All);
            AgentBannerEditor->EditorState->SetHasChanged(false);
        }

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("Copy", FontAwesomeIcon.Copy, t("PortraitHelperWindows.MenuBar.ExportToClipboard.Label"))) // GetAddonText(100) ?? "Copy"
        {
            PortraitPreset.FromState()?.ToClipboard();
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
                PortraitHelper.ClipboardPreset.ToState(ImportFlags.All);
                PortraitHelper.CloseOverlays();
            }
        }

        ImGui.SameLine();
        if (PortraitHelper.ClipboardPreset == null)
        {
            ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, t("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label"), disabled: true);
        }
        else if (Service.WindowManager.IsWindowOpen<AdvancedImportOverlay>())
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FileImport, t("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label")))
            {
                Service.WindowManager.CloseWindow<AdvancedImportOverlay>();
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedImport", FontAwesomeIcon.FileImport, t("PortraitHelperWindows.MenuBar.ToggleAdvancedImportMode.Label")))
        {
            PortraitHelper.CloseOverlays();
            Service.WindowManager.OpenWindow<AdvancedImportOverlay>();
        }

        ImGui.SameLine();
        if (Service.WindowManager.IsWindowOpen<AdvancedEditOverlay>())
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal", FontAwesomeIcon.FilePen, t("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label")))
            {
                Service.WindowManager.CloseWindow<AdvancedEditOverlay>();
            }
        }
        else if (ImGuiUtils.IconButton("ViewModeAdvancedEdit", FontAwesomeIcon.FilePen, t("PortraitHelperWindows.MenuBar.ToggleAdvancedEditMode.Label")))
        {
            PortraitHelper.CloseOverlays();
            Service.WindowManager.OpenWindow<AdvancedEditOverlay>();
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (ImGuiUtils.IconButton("SaveAsPreset", FontAwesomeIcon.Download, t("PortraitHelperWindows.MenuBar.SaveAsPreset.Label")))
        {
            _saveAsPresetDialog.Open(_portraitName, PortraitPreset.FromState(), PortraitHelper.GetCurrentCharaViewImage());
        }

        ImGui.SameLine();
        if (Service.WindowManager.IsWindowOpen<PresetBrowserOverlay>())
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ViewModeNormal2", FontAwesomeIcon.List, t("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label")))
            {
                Service.WindowManager.CloseWindow<PresetBrowserOverlay>();
            }
        }
        else if (ImGuiUtils.IconButton("ViewModePresetBrowser", FontAwesomeIcon.List, t("PortraitHelperWindows.MenuBar.TogglePresetBrowser.Label")))
        {
            PortraitHelper.CloseOverlays();
            Service.WindowManager.OpenWindow<PresetBrowserOverlay>();
        }

        // ----

        ImGuiUtils.VerticalSeparator();

        // ----

        ImGui.SameLine();
        if (Service.WindowManager.IsWindowOpen<AlignmentToolSettingsOverlay>())
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, 0xFFE19942)
                                     .Push(ImGuiCol.ButtonActive, 0xFFB06C2B)
                                     .Push(ImGuiCol.ButtonHovered, 0xFFCE8231);

            if (ImGuiUtils.IconButton("ToggleAlignmentToolOff", FontAwesomeIcon.Hashtag, t("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label.CloseSettings")))
            {
                if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                {
                    Service.WindowManager.CloseWindow<AlignmentToolSettingsOverlay>();
                }
                else
                {
                    Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                    Service.GetService<Configuration>().Save();
                }
            }
        }
        else if (ImGuiUtils.IconButton("ToggleAlignmentToolOn", FontAwesomeIcon.Hashtag, t("PortraitHelperWindows.MenuBar.ToggleAlignmentTool.Label.OpenSettings")))
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                PortraitHelper.CloseOverlays();
                Service.WindowManager.OpenWindow<AlignmentToolSettingsOverlay>();
            }
            else
            {
                Config.ShowAlignmentTool = !Config.ShowAlignmentTool;
                Service.GetService<Configuration>().Save();
            }
        }

        if (!string.IsNullOrEmpty(_portraitName))
        {
            ImGuiUtils.VerticalSeparator();
            ImGui.SameLine();
            ImGui.TextUnformatted(_portraitName);
        }

        UpdatePosition();

        _saveAsPresetDialog.Draw();
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

            if (!(ImGuiHelpers.GlobalScale <= 1 && (Service.WindowManager.IsWindowOpen<AdvancedImportOverlay>() || Service.WindowManager.IsWindowOpen<PresetBrowserOverlay>())))
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
