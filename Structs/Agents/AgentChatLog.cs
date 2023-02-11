using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? EB 03 48 8B C5 33 D2 48 89 47 48"
[Agent(AgentId.ChatLog)]
[StructLayout(LayoutKind.Explicit, Size = 0xB28)]
public unsafe struct AgentChatLog
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x948)] public uint ContextItemId;
}
