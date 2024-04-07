namespace HaselTweaks.Structs;

public unsafe partial struct HaselExdModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 48 2B")]
    public static partial BannerConditionRow* GetBannerCondition(uint index);
}

public partial struct BannerConditionRow
{
    [MemberFunction("40 53 48 83 EC 20 0F B6 41 29")]
    public readonly partial uint GetBannerConditionUnlockState();
}
