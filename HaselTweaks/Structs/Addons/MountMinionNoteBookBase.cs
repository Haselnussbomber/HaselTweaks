using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 48 8B D9 E8"
// used by MinionNoteBook and MountNoteBook
[StructLayout(LayoutKind.Explicit, Size = 0xBF0)]
public partial struct MountMinionNoteBookBase
{
    public enum ViewType
    {
        Favorites = 1,
        Normal,
        Search,
    }

    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x2A0)] public TabSwitcher TabSwitcher;

    [FieldOffset(0x8C0)] public ViewType CurrentView;

    [MemberFunction("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 8D 42 D3 83 F8 08")]
    public readonly partial void SwitchToFavorites();
}
