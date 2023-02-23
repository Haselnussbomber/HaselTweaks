using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 C7 43 ?? ?? ?? ?? ?? 48 89 03 48 8B C3 C6 43 30 00 48 83 C4 20 5B C3 CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 48 8D 05 ?? ?? ?? ?? 48 89 01 E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ??"
[Agent(AgentId.MJICraftSchedule)]
[StructLayout(LayoutKind.Explicit, Size = 0x38)]
public unsafe struct AgentMJICraftSchedule
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x28)] public MJICraftScheduleData* Data;
    [FieldOffset(0x30)] public sbyte TabIndex;

    // ctor "E8 ?? ?? ?? ?? 48 89 43 28 48 85 C0 74 26"
    [StructLayout(LayoutKind.Explicit, Size = 0x850)]
    public unsafe partial struct MJICraftScheduleData
    {
        [FieldOffset(0x84C)] public byte Flags;

        public bool IsLoading => (Flags & 0x40) != 0;
    }
}
