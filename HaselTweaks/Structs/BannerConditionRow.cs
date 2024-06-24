namespace HaselTweaks.Structs;

[GenerateInterop]
public unsafe partial struct BannerConditionRow
{
    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 48 2B")]
    public static partial BannerConditionRow* GetByRowId(uint rowId);

    [MemberFunction("40 53 48 83 EC 20 0F B6 41 29")]
    public partial byte GetUnlockState();
}
