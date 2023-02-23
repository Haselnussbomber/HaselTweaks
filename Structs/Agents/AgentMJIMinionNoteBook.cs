using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? C6 47 28 00"
[Agent(AgentId.MJIMinionNoteBook)]
[StructLayout(LayoutKind.Explicit, Size = 0x208)]
public unsafe partial struct AgentMJIMinionNoteBook
{
    [FieldOffset(0)] public AgentInterface AgentInterface;

    public enum ViewType : byte
    {
        Favorites = 1,
        Normal,
        Search,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x4)]
    public unsafe partial struct SelectedMinionInfo
    {
        [FieldOffset(0)] public ushort MinionId;
        [FieldOffset(2)] public byte TabIndex;
        [FieldOffset(3)] public byte SlotIndex;
    }

    [FieldOffset(0x1DC)] public SelectedMinionInfo SelectedFavoriteMinion;
    [FieldOffset(0x1E0)] public SelectedMinionInfo SelectedNormalMinion;
    [FieldOffset(0x1E0)] public SelectedMinionInfo SelectedSearchMinion;
    [FieldOffset(0x1E8)] public SelectedMinionInfo* SelectedMinion;
    [FieldOffset(0x1F0)] public ViewType CurrentView;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 86 ?? ?? ?? ?? 88 58 03")]
    public partial void UpdateTabFlags(int* flags);

    // from Update (vf6):
    // ViewType 1 = 0x407
    // ViewType 2 = 0x40B
    // ViewType 3 = 0x413
    public void UpdateTabFlags(int flags)
    {
        var ptr = (int*)Marshal.AllocHGlobal(4);
        *ptr = flags;
        UpdateTabFlags(ptr);
        Marshal.FreeHGlobal((nint)ptr);
    }

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 D8 85 DB")]
    public partial ushort GetSelectedMinionId(byte* viewType, byte* currentTabIndex, byte* currentSlotIndex);

    public ushort GetSelectedMinionId()
    {
        if (SelectedMinion != null)
        {
            var ptr = (byte*)Marshal.AllocHGlobal(3);

            *ptr = (byte)CurrentView;
            *(ptr + 1) = SelectedMinion->TabIndex;
            *(ptr + 2) = SelectedMinion->SlotIndex;

            var value = GetSelectedMinionId(ptr, ptr + 1, ptr + 2);

            Marshal.FreeHGlobal((nint)ptr);

            return value;
        }

        return 0;
    }
}
