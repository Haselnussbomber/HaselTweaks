using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8B CB 48 89 03 E8 ?? ?? ?? ?? 48 8B D0 48 8D 8B ?? ?? ?? ?? E8 ?? ?? ?? ?? B9
[StructLayout(LayoutKind.Explicit, Size = 0x4E0)]
public unsafe struct AddonBannerEditor
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x2D8)] public HaselAtkComponentDropDownList* PresetDropdown;
    [FieldOffset(0x2F8)] public HaselAtkComponentDropDownList* BackgroundDropdown;
    [FieldOffset(0x318)] public HaselAtkComponentDropDownList* FrameDropdown;
    [FieldOffset(0x338)] public HaselAtkComponentDropDownList* AccentDropdown;
    [FieldOffset(0x358)] public HaselAtkComponentDropDownList* PoseDropdown;
    [FieldOffset(0x378)] public HaselAtkComponentDropDownList* ExpressionDropdown;

    [FieldOffset(0x3B8)] public HaselAtkComponentCheckBox* PlayAnimationCheckbox;
    [FieldOffset(0x3C0)] public HaselAtkComponentCheckBox* HeadFacingCameraCheckbox;
    [FieldOffset(0x3C8)] public HaselAtkComponentCheckBox* EyesFacingCameraCheckbox;

    [FieldOffset(0x3F8)] public AtkComponentButton* ApplyEquipmentButton;
    [FieldOffset(0x400)] public AtkComponentButton* SaveButton;
    [FieldOffset(0x408)] public AtkComponentButton* CloseButton;

    [FieldOffset(0x410)] public HaselAtkComponentSlider* AmbientLightingColorRedSlider;
    [FieldOffset(0x418)] public HaselAtkComponentSlider* AmbientLightingColorGreenSlider;
    [FieldOffset(0x420)] public HaselAtkComponentSlider* AmbientLightingColorBlueSlider;
    [FieldOffset(0x428)] public HaselAtkComponentSlider* AmbientLightingBrightnessSlider;
    [FieldOffset(0x430)] public HaselAtkComponentSlider* DirectionalLightingColorRedSlider;
    [FieldOffset(0x438)] public HaselAtkComponentSlider* DirectionalLightingColorGreenSlider;
    [FieldOffset(0x440)] public HaselAtkComponentSlider* DirectionalLightingColorBlueSlider;
    [FieldOffset(0x448)] public HaselAtkComponentSlider* DirectionalLightingBrightnessSlider;
    [FieldOffset(0x450)] public HaselAtkComponentSlider* DirectionalLightingVerticalAngleSlider;
    [FieldOffset(0x458)] public HaselAtkComponentSlider* DirectionalLightingHorizontalAngleSlider;
    [FieldOffset(0x460)] public HaselAtkComponentSlider* CameraZoomSlider;
    [FieldOffset(0x468)] public HaselAtkComponentSlider* ImageRotation;

    [FieldOffset(0x4C4)] public short NumPresets;
}
