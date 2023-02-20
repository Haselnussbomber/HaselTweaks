using Dalamud.Interface.Windowing;
using Dalamud.Memory;
using HaselTweaks.Structs;
using HaselTweaks.Utils;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;
using HaselAtkComponentDropDownList = HaselTweaks.Structs.AtkComponentDropDownList;
using HaselAtkComponentList = HaselTweaks.Structs.AtkComponentList;

namespace HaselTweaks.Windows;

public unsafe class PortraitHelperWindow : Window
{
    public AgentBannerEditor* AgentBannerEditor { get; internal set; }
    public AddonBannerEditor* AddonBannerEditor { get; internal set; }

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

    public override bool DrawConditions()
    {
        return AgentBannerEditor != null
            && AddonBannerEditor != null
            && AgentBannerEditor->PortraitState != null;
    }

    public override void Draw()
    {
        var labelCopy = StringUtils.GetAddonText(100) ?? "Copy";
        if (ImGui.Button(labelCopy))
        {
            Copy();
        }

        if (HasSavedData)
        {
            ImGui.SameLine();

            var labelPaste = StringUtils.GetAddonText(101) ?? "Paste";
            if (ImGui.Button(labelPaste))
            {
                Paste();
            }

            ImGui.SameLine();
            ImGui.Checkbox("Advanced Mode", ref AdvancedMode);

            if (AdvancedMode)
            {
                ImGuiUtils.DrawPaddedSeparator();

                var state = AgentBannerEditor->PortraitState;
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
                    StringUtils.GetAddonText(14687) ?? "Background",
                    StringUtils.GetSheetText<BannerBg>((uint)ExportedPortraitData->BannerBg, "Name") ?? ExportedPortraitData->BannerBg.ToString()
                );

                AddRow(
                    "Frame",
                    ref CopyFrame,
                    StringUtils.GetAddonText(14688) ?? "Frame",
                    StringUtils.GetSheetText<BannerFrame>((uint)Frame, "Name") ?? Frame.ToString()
                );

                AddRow(
                    "Accent",
                    ref CopyAccent,
                    StringUtils.GetAddonText(14689) ?? "Accent",
                    StringUtils.GetSheetText<BannerDecoration>((uint)Accent, "Name") ?? Accent.ToString()
                );

                var poseName = StringUtils.GetSheetText<BannerTimeline>((uint)ExportedPortraitData->Pose, "Name");
                if (string.IsNullOrEmpty(poseName))
                {
                    var poseRow = Service.Data.GetExcelSheet<BannerTimeline>()?.GetRow((uint)ExportedPortraitData->Pose);
                    if (poseRow != null)
                    {
                        switch (poseRow.Type)
                        {
                            case 2:
                                poseName = StringUtils.GetSheetText<Action>(poseRow.AdditionalData, "Name");
                                break;

                            case 10:
                            case 11:
                                poseName = StringUtils.GetSheetText<Emote>(poseRow.AdditionalData, "Name");
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
                    StringUtils.GetAddonText(14690) ?? "Pose",
                    poseName
                );

                AddRow(
                    "Expression",
                    ref CopyExpression,
                    StringUtils.GetAddonText(14691) ?? "Expression",
                    GetExpressionName(state->ExpressionItems, state->ExpressionItemsCount, ExportedPortraitData->Expression) ?? ExportedPortraitData->Expression.ToString()
                );

                var labelAmbientLighing = StringUtils.GetAddonText(14692) ?? "Ambient Lighting";
                var labelDirectionalLighing = StringUtils.GetAddonText(14693) ?? "Directional Lighting";
                var labelBrightness = StringUtils.GetAddonText(14694) ?? "Brightness";
                var labelVerticalAngle = StringUtils.GetAddonText(14696) ?? "Vertical Angle";
                var labelHorizontalAngle = StringUtils.GetAddonText(14695) ?? "Horizontal Angle";
                var labelRed = StringUtils.GetAddonText(5932) ?? "Red";
                var labelGreen = StringUtils.GetAddonText(5933) ?? "Green";
                var labelBlue = StringUtils.GetAddonText(5934) ?? "Blue";

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
                    ExportedPortraitData->CameraPosition1 != Half.Zero || ExportedPortraitData->CameraPosition2 != Half.Zero // || ExportedPortraitData->CameraPosition3 != (Half)1.2f
                        ? (StringUtils.GetAddonText(4203) ?? "Custom")
                        : (StringUtils.GetAddonText(4202) ?? "Default")
                );
#if DEBUG
                ImGui.Text($"{ExportedPortraitData->CameraPosition1:0.00}, {ExportedPortraitData->CameraPosition2:0.00}, {ExportedPortraitData->CameraPosition3:0.00}, {ExportedPortraitData->CameraPosition4:0.00}");
#endif

                AddRow(
                    "CameraTarget",
                    ref CopyCameraTarget,
                    "Camera Target",
                    ExportedPortraitData->CameraTarget1 != Half.Zero || ExportedPortraitData->CameraTarget2 != Half.Zero || ExportedPortraitData->CameraTarget3 != Half.Zero
                        ? (StringUtils.GetAddonText(4203) ?? "Custom")
                        : (StringUtils.GetAddonText(4202) ?? "Default")
                );
#if DEBUG
                ImGui.Text($"{ExportedPortraitData->CameraTarget1:0.00}, {ExportedPortraitData->CameraTarget2:0.00}, {ExportedPortraitData->CameraTarget3:0.00}, {ExportedPortraitData->CameraTarget4:0.00}");
#endif

                AddRow(
                    "HeadDirection",
                    ref CopyHeadDirection,
                    "Head Direction",
                    ExportedPortraitData->HeadDirection1 != Half.Zero || ExportedPortraitData->HeadDirection2 != Half.Zero
                        ? (StringUtils.GetAddonText(4203) ?? "Custom")
                        : (StringUtils.GetAddonText(4202) ?? "Default")
                );
#if DEBUG
                ImGui.Text($"{ExportedPortraitData->HeadDirection1:0.00}, {ExportedPortraitData->HeadDirection2:0.00}");
#endif

                AddRow(
                    "EyeDirection",
                    ref CopyEyeDirection,
                    "Eye Direction",
                    ExportedPortraitData->EyeDirection1 != Half.Zero || ExportedPortraitData->EyeDirection2 != Half.Zero
                        ? (StringUtils.GetAddonText(4203) ?? "Custom")
                        : (StringUtils.GetAddonText(4202) ?? "Default")
                );
#if DEBUG
                ImGui.Text($"{ExportedPortraitData->EyeDirection1:0.00}, {ExportedPortraitData->EyeDirection2:0.00}");
#endif

                AddRow(
                    "CameraZoom",
                    ref CopyZoom,
                    StringUtils.GetAddonText(14711) ?? "Zoom",
                    $"{ExportedPortraitData->CameraZoom}"
                );

                AddRow(
                    "ImageRotation",
                    ref CopyRotation,
                    StringUtils.GetAddonText(14712) ?? "Rotation",
                    $"{ExportedPortraitData->ImageRotation}"
                );

                ImGui.EndTable();

                ImGui.TextColored(ImGuiUtils.ColorGrey, "Only checked rows will be pasted.");
            }
        }

        Position = new(
            AddonBannerEditor->AtkUnitBase.X - ImGui.GetWindowSize().X,
            AddonBannerEditor->AtkUnitBase.Y + 1
        );
    }

    public void Copy()
    {
        var state = AgentBannerEditor->PortraitState;

        Frame = state->BannerFrame;
        Accent = state->BannerDecoration;

        state->CharaView->ExportPortraitData(ExportedPortraitData);
        HasSavedData = true;
    }

    public void Paste()
    {
        var state = AgentBannerEditor->PortraitState;
        var addon = AddonBannerEditor;

        var portraitData = (ExportedPortraitData*)MemoryHelper.Allocate(0x34);
        state->CharaView->ExportPortraitData(portraitData); // read current state

        if (!AdvancedMode || CopyBackground)
        {
            state->BannerBg = ExportedPortraitData->BannerBg;
            portraitData->BannerBg = ExportedPortraitData->BannerBg;

            addon->BackgroundDropdown->SetValue(FindListIndex(state->BannerItems, state->BannerItemsCount, ExportedPortraitData->BannerBg));
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
            var presetIndex = state->GetPresetIndex(state->BannerBg, state->BannerFrame, state->BannerDecoration);
            if (presetIndex < 0)
            {
                presetIndex = addon->NumPresets - 1;

                GetDropdownList(addon->PresetDropdown)->SetListLength(addon->NumPresets); // increase to maximum, so "Custom" is displayed
            }

            addon->PresetDropdown->SetValue(presetIndex);
        }

        if (!AdvancedMode || CopyPose)
        {
            state->BannerTimeline = ExportedPortraitData->Pose;
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
            portraitData->CameraPosition1 = ExportedPortraitData->CameraPosition1;
            portraitData->CameraPosition2 = ExportedPortraitData->CameraPosition2;
            portraitData->CameraPosition3 = ExportedPortraitData->CameraPosition3;
            portraitData->CameraPosition4 = ExportedPortraitData->CameraPosition4;
        }

        if (!AdvancedMode || CopyCameraTarget)
        {
            portraitData->CameraTarget1 = ExportedPortraitData->CameraTarget1;
            portraitData->CameraTarget2 = ExportedPortraitData->CameraTarget2;
            portraitData->CameraTarget3 = ExportedPortraitData->CameraTarget3;
            portraitData->CameraTarget4 = ExportedPortraitData->CameraTarget4;
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

    private static HaselAtkComponentList* GetDropdownList(HaselAtkComponentDropDownList* dropdown)
    {
        return *(HaselAtkComponentList**)((IntPtr)dropdown + 0xC8);
    }

    private static int FindListIndex(IntPtr list, uint numItems, short value)
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

    private static string? GetExpressionName(IntPtr list, uint numItems, short value)
    {
        if (value == 0)
        {
            return StringUtils.GetAddonText(14727) ?? "None";
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
