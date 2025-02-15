namespace HaselTweaks.Structs;

[GenerateInterop]
public unsafe partial struct HaselExdModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 48 2B")]
    public static partial HaselExdModule* GetBannerConditionByIndex(uint rowId);
}
