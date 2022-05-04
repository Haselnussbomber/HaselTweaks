using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 48 89 03 E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 33 D2
[StructLayout(LayoutKind.Explicit, Size = 0xCD0)]
public unsafe partial struct AddonAOZNotebook
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0xCB8)] public int TabIndex;
    [FieldOffset(0xCBC)] public int NumTabs;
}
