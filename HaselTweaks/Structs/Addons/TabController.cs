using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? BF ?? ?? ?? ?? 48 8D AB"
[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
public unsafe struct TabController
{
    [FieldOffset(0x80)] public int TabIndex;
    [FieldOffset(0x84)] public int TabCount;

    [FieldOffset(0x90)] public delegate* unmanaged<int, AtkUnitBase*, void> CallbackFunction; // (int tabIndex, AtkUnitBase* addon)
    [FieldOffset(0x98)] public nint Addon;

    [FieldOffset(0xA8)] public bool Enabled;
}
