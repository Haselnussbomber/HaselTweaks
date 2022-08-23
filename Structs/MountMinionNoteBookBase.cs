using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor 48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 48 8B D9 E8
// used by MinionNoteBook and MountNoteBook
[StructLayout(LayoutKind.Explicit, Size = 0xBE0)]
public unsafe partial struct MountMinionNoteBookBase
{
    public enum ViewType
    {
        Favorites = 1,
        Normal,
        Search,
    }

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x298)] public TabSwitcher TabSwitcher;

    [FieldOffset(0x8A8)] public ViewType CurrentView;
}
