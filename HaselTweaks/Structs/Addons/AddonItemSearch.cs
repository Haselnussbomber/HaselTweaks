using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs.Addons;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 33 ED 48 8D 05 ?? ?? ?? ?? 48 8D 8B"
[StructLayout(LayoutKind.Explicit, Size = 0x3EE0)]
public unsafe struct AddonItemSearch
{
    [FieldOffset(0)] public AtkUnitBase* AtkUnitBase;
    [FieldOffset(0x2DE0)] public AtkComponentList* SearchResultsList;
}
