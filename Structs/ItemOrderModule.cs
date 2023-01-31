using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xD8)]
public unsafe partial struct ItemOrderModule
{
    public static ItemOrderModule* Instance => (ItemOrderModule*)Framework.Instance()->GetUiModule()->GetItemOrderModule();

    //[FieldOffset(0)] public UserFileEvent UserFileEvent;

    [FieldOffset(0x3D)] public bool IsEventPending;

    [FieldOffset(0x40)] public ItemOrderModuleSorter* InventorySorter;

    [FieldOffset(0x48), FixedSizeArray<Pointer<ItemOrderModuleSorter>>(12)]
    public fixed byte ArmourySorter[12 * 8];

    [FieldOffset(0x48)] public ItemOrderModuleSorter* ArmouryMainHandSorter;
    [FieldOffset(0x50)] public ItemOrderModuleSorter* ArmouryHeadSorter;
    [FieldOffset(0x58)] public ItemOrderModuleSorter* ArmouryBodySorter;
    [FieldOffset(0x60)] public ItemOrderModuleSorter* ArmouryHandsSorter;
    [FieldOffset(0x68)] public ItemOrderModuleSorter* ArmouryLegsSorter;
    [FieldOffset(0x70)] public ItemOrderModuleSorter* ArmouryFeetSorter;
    [FieldOffset(0x78)] public ItemOrderModuleSorter* ArmouryOffHandSorter;
    [FieldOffset(0x80)] public ItemOrderModuleSorter* ArmouryEarsSorter;
    [FieldOffset(0x88)] public ItemOrderModuleSorter* ArmouryNeckSorter;
    [FieldOffset(0x90)] public ItemOrderModuleSorter* ArmouryWristsSorter;
    [FieldOffset(0x98)] public ItemOrderModuleSorter* ArmouryRingsSorter;
    [FieldOffset(0xA0)] public ItemOrderModuleSorter* ArmourySoulCrystalSorter;
    [FieldOffset(0xA8)] public ItemOrderModuleSorter* ArmouryWaistSorter;

    [FieldOffset(0xC8)] public ItemOrderModuleSorter* SaddleBagSorter;
    [FieldOffset(0xD0)] public ItemOrderModuleSorter* PremiumSaddleBagSorter;

    [StructLayout(LayoutKind.Explicit, Size = 0x60)]
    public unsafe struct ItemOrderModuleSorter
    {
        [FieldOffset(0)] public InventoryType InventoryType;
        [FieldOffset(0x38)] public int Status;
    }
}
