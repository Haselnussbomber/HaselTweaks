using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? C6 43 30 01"
[Agent(AgentId.AetherCurrent)]
[VTableAddress("C6 43 30 01 48 8D 05 ?? ?? ?? ??", 7)]
[StructLayout(LayoutKind.Explicit, Size = 0x68)]
public unsafe partial struct AgentAetherCurrent
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x64)] public byte TabIndex;
}
