using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8B D3 48 8D 4F 28 48 89 07 E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 33 C0 48 89 47 60 89 47 74"
[Agent(AgentId.DailyQuestSupply)]
[StructLayout(LayoutKind.Explicit, Size = 0x80)]
public struct AgentDailyQuestSupply
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x54)] public uint ContextMenuItemId;
}
