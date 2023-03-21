using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? EB 03 48 8B C5 33 D2 48 89 47 58"
[Agent(AgentId.ScenarioTree)]
[StructLayout(LayoutKind.Explicit, Size = 0x30)]
public unsafe struct AgentScenarioTree
{
    [FieldOffset(0)] public AgentInterface AgentInterface;
    [FieldOffset(0x28)] public ScenarioTreeData* Data;

    [StructLayout(LayoutKind.Explicit, Size = 0x38)]
    public struct ScenarioTreeData
    {
        [FieldOffset(0)] public ushort NextId; // probably?

        [FieldOffset(0xA)] public ushort CurrentId; // probably? ScenarioTree rowId = CurrentId | 0x10000
    }
}
