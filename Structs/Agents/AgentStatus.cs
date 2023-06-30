using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 33 F6 C7 47 ?? ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 89 77 28"
[Agent(AgentId.Status)]
[StructLayout(LayoutKind.Explicit, Size = 0x358)]
public unsafe partial struct AgentStatus
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [MemberFunction("E8 ?? ?? ?? ?? 49 8B CD E8 ?? ?? ?? ?? E8 ?? ?? ?? ??")]
    public readonly partial void UpdateGearVisibilityInNumberArray();
}
