using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x10)] // size not correct, but it's fine
public unsafe partial struct FakeAgentBannerListData
{
    [FieldOffset(0x08)] public UIModule* UIModule;

    [MemberFunction("4C 89 4C 24 ?? 48 89 54 24 ?? 53 57")]
    public partial bool LoadEquipmentData(uint* itemIds, byte* stainIds, ushort* glassesIds);
}
