using System;
using System.Runtime.InteropServices;
using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace HaselTweaks.Windows;

public unsafe class PortraitHelperWindow : Window
{
    private PortraitHelper? Tweak;
    private bool HasSavedData;

    private short Frame;
    private short Accent;
    private readonly ExportedPortraitData* ExportedPortraitData;

    private bool CopyBackground = true;
    private bool CopyFrame = true;
    private bool CopyAccent = true;
    private bool CopyPose = true;
    private bool CopyExpression = true;
    private bool CopyAmbientLightingBrightness = true;
    private bool CopyAmbientLightingColorRed = true;
    private bool CopyAmbientLightingColorGreen = true;
    private bool CopyAmbientLightingColorBlue = true;
    private bool CopyDirectionalLightingBrightness = true;
    private bool CopyDirectionalLightingColorRed = true;
    private bool CopyDirectionalLightingColorGreen = true;
    private bool CopyDirectionalLightingColorBlue = true;
    private bool CopyDirectionalLightingVerticalAngle = true;
    private bool CopyDirectionalLightingHorizontalAngle = true;
    private bool CopyAnimationProgress = true;
    private bool CopyCameraPosition = true;
    private bool CopyCameraTarget = true;
    private bool CopyHeadDirection = true;
    private bool CopyEyeDirection = true;
    private bool CopyZoom = true;
    private bool CopyRotation = true;

    private bool AdvancedMode = false;

    public PortraitHelperWindow() : base("[HaselTweaks] Portrait Helper")
    {
        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.NoDecoration;
        base.Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        ExportedPortraitData = (ExportedPortraitData*)MemoryHelper.GameAllocateUi(0x34);
    }

    ~PortraitHelperWindow()
    {
        var ptr = (IntPtr)ExportedPortraitData;
        MemoryHelper.GameFree(ref ptr, 0x34);
    }

    internal void SetTweak(PortraitHelper tweak)
    {
        Tweak = tweak;
    }

    public override bool DrawConditions()
    {
        return Tweak != null
            && PortraitHelper.AgentBannerEditor != null
            && PortraitHelper.AgentBannerEditor->AgentInterface.IsAgentActive()
            && PortraitHelper.AgentBannerEditor->PortraitState != null;
    }

    public override void Draw()
    {
        var labelCopy = Service.StringUtils.GetSheetText<Addon>(100, "Text") ?? "Copy";
        if (ImGui.Button(labelCopy))
        {
            Copy();
        }

        if (HasSavedData)
        {
            ImGui.SameLine();

            var labelPaste = Service.StringUtils.GetSheetText<Addon>(101, "Text") ?? "Paste";
            if (ImGui.Button(labelPaste))
            {
                Paste();
            }

            ImGui.SameLine();
            ImGui.Checkbox("Advanced Mode", ref AdvancedMode);

            if (AdvancedMode)
            {
                ImGuiUtils.DrawPaddedSeparator();

                var state = PortraitHelper.AgentBannerEditor->PortraitState;
                if (!ImGui.BeginTable("##HaselTweaks_PortraitData", 3))
                {
                    return;
                }

                static void AddRow(string key, ref bool copyValue, string label, string value)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Checkbox("##HaselTweaks_PortraitHelper_" + key, ref copyValue);
                    ImGui.TableNextColumn();
                    ImGui.Text(label);
                    ImGui.TableNextColumn();
                    ImGui.Text(value);
                }

                AddRow(
                    "Background",
                    ref CopyBackground,
                    Service.StringUtils.GetSheetText<Addon>(14687, "Text") ?? "Background",
                    Service.StringUtils.GetSheetText<BannerBg>((uint)ExportedPortraitData->Background, "Name") ?? ExportedPortraitData->Background.ToString()
                );

                AddRow(
                    "Frame",
                    ref CopyFrame,
                    Service.StringUtils.GetSheetText<Addon>(14688, "Text") ?? "Frame",
                    Service.StringUtils.GetSheetText<BannerFrame>((uint)Frame, "Name") ?? Frame.ToString()
                );

                AddRow(
                    "Accent",
                    ref CopyAccent,
                    Service.StringUtils.GetSheetText<Addon>(14689, "Text") ?? "Accent",
                    Service.StringUtils.GetSheetText<BannerDecoration>((uint)Accent, "Name") ?? Accent.ToString()
                );

                var poseName = Service.StringUtils.GetSheetText<BannerTimeline>((uint)ExportedPortraitData->Pose, "Name");
                if (string.IsNullOrEmpty(poseName))
                {
                    var poseRow = Service.Data.GetExcelSheet<BannerTimeline>()?.GetRow((uint)ExportedPortraitData->Pose);
                    if (poseRow != null)
                    {
                        switch (poseRow.Type)
                        {
                            case 2:
                                poseName = Service.StringUtils.GetSheetText<Action>(poseRow.AdditionalData, "Name");
                                break;

                            case 10:
                            case 11:
                                poseName = Service.StringUtils.GetSheetText<Emote>(poseRow.AdditionalData, "Name");
                                break;

                            case 20:
                                // TODO
                                break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(poseName))
                {
                    poseName = ExportedPortraitData->Pose.ToString();
                }

                AddRow(
                    "Pose",
                    ref CopyPose,
                    Service.StringUtils.GetSheetText<Addon>(14690, "Text") ?? "Pose",
                    poseName
                );

                AddRow(
                    "Expression",
                    ref CopyExpression,
                    Service.StringUtils.GetSheetText<Addon>(14691, "Text") ?? "Expression",
                    GetExpressionName(state->ExpressionItems, state->ExpressionItemsCount, ExportedPortraitData->Expression) ?? ExportedPortraitData->Expression.ToString()
                );

                var labelAmbientLighing = Service.StringUtils.GetSheetText<Addon>(14692, "Text") ?? "Ambient Lighting";
                var labelDirectionalLighing = Service.StringUtils.GetSheetText<Addon>(14693, "Text") ?? "Directional Lighting";
                var labelBrightness = Service.StringUtils.GetSheetText<Addon>(14694, "Text") ?? "Brightness";
                var labelVerticalAngle = Service.StringUtils.GetSheetText<Addon>(14696, "Text") ?? "Vertical Angle";
                var labelHorizontalAngle = Service.StringUtils.GetSheetText<Addon>(14695, "Text") ?? "Horizontal Angle";
                var labelRed = Service.StringUtils.GetSheetText<Addon>(5932, "Text") ?? "Red";
                var labelGreen = Service.StringUtils.GetSheetText<Addon>(5933, "Text") ?? "Green";
                var labelBlue = Service.StringUtils.GetSheetText<Addon>(5934, "Text") ?? "Blue";

                AddRow(
                    "AmbientLightingBrightness",
                    ref CopyAmbientLightingBrightness,
                    $"{labelAmbientLighing} - {labelBrightness}",
                    $"{ExportedPortraitData->AmbientLightingBrightness}"
                );

                AddRow(
                    "AmbientLightingColorRed",
                    ref CopyAmbientLightingColorRed,
                    $"{labelAmbientLighing} - {labelRed}",
                    $"{ExportedPortraitData->AmbientLightingColorRed}"
                );

                AddRow(
                    "AmbientLightingColorGreen",
                    ref CopyAmbientLightingColorGreen,
                    $"{labelAmbientLighing} - {labelGreen}",
                    $"{ExportedPortraitData->AmbientLightingColorGreen}"
                );

                AddRow(
                    "AmbientLightingColorBlue",
                    ref CopyAmbientLightingColorBlue,
                    $"{labelAmbientLighing} - {labelBlue}",
                    $"{ExportedPortraitData->AmbientLightingColorBlue}"
                );

                AddRow(
                    "DirectionalLightingBrightness",
                    ref CopyDirectionalLightingBrightness,
                    $"{labelDirectionalLighing} - {labelBrightness}",
                    $"{ExportedPortraitData->DirectionalLightingBrightness}"
                );

                AddRow(
                    "DirectionalLightingColorRed",
                    ref CopyDirectionalLightingColorRed,
                    $"{labelDirectionalLighing} - {labelRed}",
                    $"{ExportedPortraitData->DirectionalLightingColorRed}"
                );

                AddRow(
                    "DirectionalLightingColorGreen",
                    ref CopyDirectionalLightingColorGreen,
                    $"{labelDirectionalLighing} - {labelGreen}",
                    $"{ExportedPortraitData->DirectionalLightingColorGreen}"
                );

                AddRow(
                    "DirectionalLightingColorBlue",
                    ref CopyDirectionalLightingColorBlue,
                    $"{labelDirectionalLighing} - {labelBlue}",
                    $"{ExportedPortraitData->DirectionalLightingColorBlue}"
                );

                AddRow(
                    "DirectionalLightingVerticalAngle",
                    ref CopyDirectionalLightingVerticalAngle,
                    $"{labelDirectionalLighing} - {labelVerticalAngle}",
                    $"{ExportedPortraitData->DirectionalLightingVerticalAngle}"
                );

                AddRow(
                    "DirectionalLightingHorizontalAngle",
                    ref CopyDirectionalLightingHorizontalAngle,
                    $"{labelDirectionalLighing} - {labelHorizontalAngle}",
                    $"{ExportedPortraitData->DirectionalLightingHorizontalAngle}"
                );

                AddRow(
                    "AnimationProgress",
                    ref CopyAnimationProgress,
                    "Animation Progress",
                    ExportedPortraitData->AnimationProgress.ToString("0")
                );

                AddRow(
                    "CameraPosition",
                    ref CopyCameraPosition,
                    "Camera Position",
                    ExportedPortraitData->CameraPositionX != 0 || ExportedPortraitData->CameraPositionY != 0
                        ? (Service.StringUtils.GetSheetText<Addon>(4203, "Text") ?? "Custom")
                        : (Service.StringUtils.GetSheetText<Addon>(4202, "Text") ?? "Default")
                );

                AddRow(
                    "CameraTarget",
                    ref CopyCameraTarget,
                    "Camera Target",
                    ExportedPortraitData->CameraTargetX != 0 || ExportedPortraitData->CameraTargetY != 0 || ExportedPortraitData->CameraTargetZ != 0
                        ? (Service.StringUtils.GetSheetText<Addon>(4203, "Text") ?? "Custom")
                        : (Service.StringUtils.GetSheetText<Addon>(4202, "Text") ?? "Default")
                );

                AddRow(
                    "HeadDirection",
                    ref CopyHeadDirection,
                    "Head Direction",
                    ExportedPortraitData->HeadDirection1 != 0 || ExportedPortraitData->HeadDirection1 != 0
                        ? (Service.StringUtils.GetSheetText<Addon>(4203, "Text") ?? "Custom")
                        : (Service.StringUtils.GetSheetText<Addon>(4202, "Text") ?? "Default")
                );

                AddRow(
                    "EyeDirection",
                    ref CopyEyeDirection,
                    "Eye Direction",
                    ExportedPortraitData->EyeDirection1 != 0 || ExportedPortraitData->EyeDirection2 != 0
                        ? (Service.StringUtils.GetSheetText<Addon>(4203, "Text") ?? "Custom")
                        : (Service.StringUtils.GetSheetText<Addon>(4202, "Text") ?? "Default")
                );

                AddRow(
                    "CameraZoom",
                    ref CopyZoom,
                    Service.StringUtils.GetSheetText<Addon>(14711, "Text") ?? "Zoom",
                    $"{ExportedPortraitData->CameraZoom}"
                );

                AddRow(
                    "ImageRotation",
                    ref CopyRotation,
                    Service.StringUtils.GetSheetText<Addon>(14712, "Text") ?? "Rotation",
                    $"{ExportedPortraitData->ImageRotation}"
                );

                ImGui.EndTable();

                ImGui.TextColored(ImGuiUtils.ColorGrey, "Only checked rows will be pasted.");
            }
        }

        var addon = (AtkUnitBase*)PortraitHelper.AddonBannerEditor;
        if (addon == null) return;
        Position = new(addon->X - ImGui.GetWindowSize().X, addon->Y + 1);
    }

    public unsafe void Copy()
    {
        var state = PortraitHelper.AgentBannerEditor->PortraitState;

        Frame = state->Frame;
        Accent = state->Accent;

        state->CharaView->ExportPortraitData(ExportedPortraitData);
        HasSavedData = true;
    }

    public unsafe void Paste()
    {
        var state = PortraitHelper.AgentBannerEditor->PortraitState;
        var addon = PortraitHelper.AddonBannerEditor;

        var portraitData = (ExportedPortraitData*)MemoryHelper.Allocate(0x34);
        state->CharaView->ExportPortraitData(portraitData); // read current state

        if (!AdvancedMode || CopyBackground)
        {
            state->Background = ExportedPortraitData->Background;
            portraitData->Background = ExportedPortraitData->Background;

            addon->BackgroundDropdown->SetValue(FindListIndex(state->BannerItems, state->BannerItemsCount, ExportedPortraitData->Background));
        }

        if (!AdvancedMode || CopyFrame)
        {
            state->SetFrame(Frame);

            addon->FrameDropdown->SetValue(FindListIndex(state->FrameItems, state->FrameItemsCount, Frame));
        }

        if (!AdvancedMode || CopyAccent)
        {
            state->SetAccent(Accent);

            addon->AccentDropdown->SetValue(FindListIndex(state->AccentItems, state->AccentItemsCount, Accent));
        }

        if (!AdvancedMode || CopyBackground | CopyFrame | CopyAccent)
        {
            var presetIndex = state->GetPresetIndex(state->Background, state->Frame, state->Accent);
            if (presetIndex < 0)
            {
                presetIndex = addon->NumPresets - 1;

                GetDropdownList(addon->PresetDropdown)->SetListLength(addon->NumPresets); // increase to maximum, so "Custom" is displayed
            }

            addon->PresetDropdown->SetValue(presetIndex);
        }

        if (!AdvancedMode || CopyPose)
        {
            state->Pose = ExportedPortraitData->Pose;
            portraitData->Pose = ExportedPortraitData->Pose;

            addon->PoseDropdown->SetValue(FindListIndex(state->PoseItems, state->PoseItemsCount, ExportedPortraitData->Pose));
        }

        if (!AdvancedMode || CopyExpression)
        {
            state->Expression = ExportedPortraitData->Expression;
            portraitData->Expression = ExportedPortraitData->Expression;

            addon->ExpressionDropdown->SetValue(FindListIndex(state->ExpressionItems, state->ExpressionItemsCount, ExportedPortraitData->Expression));
        }

        if (!AdvancedMode || CopyAmbientLightingBrightness)
        {
            portraitData->AmbientLightingBrightness = ExportedPortraitData->AmbientLightingBrightness;

            addon->AmbientLightingBrightnessSlider->SetValue(ExportedPortraitData->AmbientLightingBrightness);
        }

        if (!AdvancedMode || CopyAmbientLightingColorRed)
        {
            portraitData->AmbientLightingColorRed = ExportedPortraitData->AmbientLightingColorRed;

            addon->AmbientLightingColorRedSlider->SetValue(ExportedPortraitData->AmbientLightingColorRed);
        }

        if (!AdvancedMode || CopyAmbientLightingColorGreen)
        {
            portraitData->AmbientLightingColorGreen = ExportedPortraitData->AmbientLightingColorGreen;

            addon->AmbientLightingColorGreenSlider->SetValue(ExportedPortraitData->AmbientLightingColorGreen);
        }

        if (!AdvancedMode || CopyAmbientLightingColorBlue)
        {
            portraitData->AmbientLightingColorBlue = ExportedPortraitData->AmbientLightingColorBlue;

            addon->AmbientLightingColorBlueSlider->SetValue(ExportedPortraitData->AmbientLightingColorBlue);
        }

        if (!AdvancedMode || CopyDirectionalLightingBrightness)
        {
            portraitData->DirectionalLightingBrightness = ExportedPortraitData->DirectionalLightingBrightness;

            addon->DirectionalLightingBrightnessSlider->SetValue(ExportedPortraitData->DirectionalLightingBrightness);
        }

        if (!AdvancedMode || CopyDirectionalLightingColorRed)
        {
            portraitData->DirectionalLightingColorRed = ExportedPortraitData->DirectionalLightingColorRed;

            addon->DirectionalLightingColorRedSlider->SetValue(ExportedPortraitData->DirectionalLightingColorRed);
        }

        if (!AdvancedMode || CopyDirectionalLightingColorGreen)
        {
            portraitData->DirectionalLightingColorGreen = ExportedPortraitData->DirectionalLightingColorGreen;

            addon->DirectionalLightingColorGreenSlider->SetValue(ExportedPortraitData->DirectionalLightingColorGreen);
        }

        if (!AdvancedMode || CopyDirectionalLightingColorBlue)
        {
            portraitData->DirectionalLightingColorBlue = ExportedPortraitData->DirectionalLightingColorBlue;

            addon->DirectionalLightingColorBlueSlider->SetValue(ExportedPortraitData->DirectionalLightingColorBlue);
        }

        if (!AdvancedMode || CopyDirectionalLightingVerticalAngle)
        {
            portraitData->DirectionalLightingVerticalAngle = ExportedPortraitData->DirectionalLightingVerticalAngle;

            addon->DirectionalLightingVerticalAngleSlider->SetValue(ExportedPortraitData->DirectionalLightingVerticalAngle);
        }

        if (!AdvancedMode || CopyDirectionalLightingHorizontalAngle)
        {
            portraitData->DirectionalLightingHorizontalAngle = ExportedPortraitData->DirectionalLightingHorizontalAngle;

            addon->DirectionalLightingHorizontalAngleSlider->SetValue(ExportedPortraitData->DirectionalLightingHorizontalAngle);
        }

        if (!AdvancedMode || CopyAnimationProgress)
        {
            portraitData->AnimationProgress = ExportedPortraitData->AnimationProgress;
        }

        if (!AdvancedMode || CopyCameraPosition)
        {
            portraitData->CameraPositionX = ExportedPortraitData->CameraPositionX;
            portraitData->CameraPositionY = ExportedPortraitData->CameraPositionY;
            portraitData->CameraPositionZ = ExportedPortraitData->CameraPositionZ;
            portraitData->CameraPositionW = ExportedPortraitData->CameraPositionW;
        }

        if (!AdvancedMode || CopyCameraTarget)
        {
            portraitData->CameraTargetX = ExportedPortraitData->CameraTargetX;
            portraitData->CameraTargetY = ExportedPortraitData->CameraTargetY;
            portraitData->CameraTargetZ = ExportedPortraitData->CameraTargetZ;
            portraitData->CameraTargetW = ExportedPortraitData->CameraTargetW;
        }

        if (!AdvancedMode || CopyHeadDirection)
        {
            portraitData->HeadDirection1 = ExportedPortraitData->HeadDirection1;
            portraitData->HeadDirection2 = ExportedPortraitData->HeadDirection2;
        }

        if (!AdvancedMode || CopyEyeDirection)
        {
            portraitData->EyeDirection1 = ExportedPortraitData->EyeDirection1;
            portraitData->EyeDirection2 = ExportedPortraitData->EyeDirection2;
        }

        if (!AdvancedMode || CopyZoom)
        {
            portraitData->CameraZoom = ExportedPortraitData->CameraZoom;

            addon->CameraZoomSlider->SetValue(ExportedPortraitData->CameraZoom);
        }

        if (!AdvancedMode || CopyRotation)
        {
            portraitData->ImageRotation = ExportedPortraitData->ImageRotation;

            addon->ImageRotation->SetValue(ExportedPortraitData->ImageRotation);
        }

        state->CharaView->ImportPortraitData(portraitData);

        addon->PlayAnimationCheckbox->SetValue(false);
        addon->HeadFacingCameraCheckbox->SetValue(false);
        addon->EyesFacingCameraCheckbox->SetValue(false);

        state->SetHasChanged(true);

        MemoryHelper.Free((IntPtr)portraitData);
    }

    private HaselAtkComponentList* GetDropdownList(HaselAtkComponentDropDownList* dropdown)
    {
        return *(HaselAtkComponentList**)((IntPtr)dropdown + 0xC8);
    }

    private int FindListIndex(IntPtr list, uint numItems, short value)
    {
        var i = 0;
        while (i < numItems)
        {
            var entry = *(IntPtr*)list;
            if (*(short*)(entry + 0x10) == value)
            {
                break;
            }

            list += 8;
            i++;
        }

        return i;
    }

    private string? GetExpressionName(IntPtr list, uint numItems, short value)
    {
        if (value == 0)
        {
            return Service.StringUtils.GetSheetText<Addon>(14727, "Text");
        }

        var i = 0;
        list = *(IntPtr*)list + 0x10;
        while (i < numItems)
        {
            if (*(short*)list == value - 1)
            {
                return Marshal.PtrToStringUTF8(*(IntPtr*)(list + 0x10) + 0x28);
            }

            list += 0x18;
            i++;
        }
        return string.Empty;
    }
}
