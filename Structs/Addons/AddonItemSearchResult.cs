using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 33 C0 48 89 83 ?? ?? ?? ?? 89 83 ?? ?? ?? ??"
[StructLayout(LayoutKind.Explicit, Size = 0x3D0)]
public unsafe partial struct AddonItemSearchResult
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [VirtualFunction(16)]
    public readonly partial void Hide2();
}
