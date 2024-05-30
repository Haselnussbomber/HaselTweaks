using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x10)] // real size: 0x88
public unsafe partial struct FakeAgentBannerListData
{
    [FieldOffset(0x08)] public UIModule* UIModule;

    [MemberFunction("E8 ?? ?? ?? ?? 84 C0 75 15 33 C0")]
    public partial bool LoadEquipmentData(uint* itemIds, byte* stainIds);
}
