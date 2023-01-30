using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct ItemOrderModule
{
    public static ItemOrderModule* Instance => (ItemOrderModule*)Framework.Instance()->GetUiModule()->GetItemOrderModule();

    [FieldOffset(0x3D)] public bool IsLocked;

    [FieldOffset(0x40)] public ItemOrderModuleSorter* InventorySorter;
    [FieldOffset(0x48)] public ItemOrderModuleSorter* ArmouryBoardSorter;

    [FieldOffset(0xC8)] public ItemOrderModuleSorter* InventoryBuddySorter;
    [FieldOffset(0xD0)] public ItemOrderModuleSorter* InventoryBuddy2Sorter;

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct ItemOrderModuleSorter
    {
        [FieldOffset(0x38)] public int Status;
    }
}
