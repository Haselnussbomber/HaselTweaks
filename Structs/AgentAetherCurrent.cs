using System.Runtime.InteropServices;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? C6 43 30 01
[StructLayout(LayoutKind.Explicit, Size = 0x68)]
public struct AgentAetherCurrent
{
    [FieldOffset(0x64)] public byte TabIndex;
}
