using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 33 C0 48 89 43 28 48 89 43 30 88 43 38"
[Agent(AgentId.MJICraftSchedule)]
[StructLayout(LayoutKind.Explicit, Size = 0x38)]
public unsafe struct AgentMJICraftSchedule
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    [FieldOffset(0x28)] public MJICraftScheduleData* Data;

    [FieldOffset(0x38)] public byte TabIndex;

    // ctor "E8 ?? ?? ?? ?? 48 89 43 28 48 85 C0 74 26"
    [StructLayout(LayoutKind.Explicit, Size = 0x850)]
    public unsafe partial struct MJICraftScheduleData
    {
        [FieldOffset(0x84C)] public byte Flags;

        public readonly bool IsLoading => (Flags & 0x40) != 0;
    }
}
