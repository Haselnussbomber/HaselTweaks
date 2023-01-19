namespace HaselTweaks.Structs;

// ctor "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? C6 47 28 00"
[StructLayout(LayoutKind.Explicit, Size = 0x208)]
public unsafe partial struct AgentMJIMinionNoteBook
{
    public enum ViewType
    {
        Favorites = 1,
        Normal,
        Search,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x4)]
    public unsafe partial struct SelectedMinionInfo
    {
        [FieldOffset(0)] public ushort Id;
        [FieldOffset(2)] public byte TabIndex;
        [FieldOffset(3)] public byte Index;
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
        Marshal.FreeHGlobal((IntPtr)ptr);
    }
}
