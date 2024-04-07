using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 4B 48 33 C0 48 89 43 28"
[Agent(AgentId.MiragePrismPrismBox)]
[StructLayout(LayoutKind.Explicit, Size = 0x80)]
public partial struct AgentMiragePrismPrismBox
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x3A)] public byte PageIndex;

    [MemberFunction("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 0F B6 43 3A")]
    public readonly partial void UpdateItems(bool resetTabIndex, bool a2);
}
