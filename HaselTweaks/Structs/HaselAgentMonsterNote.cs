using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x60)]
public unsafe partial struct HaselAgentMonsterNote
{
    [FieldOffset(0)] public AgentMonsterNote BaseClass;
    [FieldOffset(0x50)] public int Flags;

    [MemberFunction("E8 ?? ?? ?? ?? 41 C6 46 ?? ?? 48 8B 5C 24 ?? 49 8B C6 48 8B 74 24")]
    public partial void OpenWithData(byte classIndex, byte rank, byte a4, byte a5);
}
