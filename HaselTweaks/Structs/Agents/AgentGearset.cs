using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Agents;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC 60 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 48 8B F1"
[Agent(AgentId.Gearset)]
[StructLayout(LayoutKind.Explicit, Size = 0xB00)]
public unsafe partial struct AgentGearset
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [MemberFunction("40 53 48 83 EC 20 8B DA 41 83 F8 14")]
    public readonly partial void ContextMenuGlamourCallback(uint gearsetId, ContextMenuGlamourCallbackAction action);

    public enum ContextMenuGlamourCallbackAction
    {
        Link = 20,
        ChangeLink = 21,
        Unlink = 22,
    }
}
