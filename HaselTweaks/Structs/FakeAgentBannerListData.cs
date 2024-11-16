using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x10)] // size not correct, but it's fine
public unsafe partial struct FakeAgentBannerListData
{
    [FieldOffset(0x08)] public UIModule* UIModule;

    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 74 11 48 8B D3 66 C7 43")]
    public partial bool LoadEquipmentData(uint* itemIds, byte* stainIds0, byte* stainIds1, ushort* glassesIds);
}
