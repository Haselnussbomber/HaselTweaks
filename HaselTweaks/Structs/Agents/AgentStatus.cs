using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Agents;

// ctor "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 33 F6 C7 47 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 89 77 28"
[Agent(AgentId.Status)]
[VTableAddress("48 8D 05 ?? ?? ?? ?? 89 77 28", 3)]
[StructLayout(LayoutKind.Explicit, Size = 0x358)]
public partial struct AgentStatus
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x3C)] public byte TabIndex;
}
