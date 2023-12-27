using FFXIVClientStructs.FFXIV.Client.Game;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselActionTimelineDriver
{
    [FixedSizeArray<Pointer<HaselSchedulerTimelineSlot>>(ActionTimelineDriver.TimelineSlotCount)]
    [FieldOffset(0x70)] public unsafe fixed byte SchedulerTimelineSlots[ActionTimelineDriver.TimelineSlotCount * 0x8];
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public unsafe struct HaselSchedulerTimelineSlot
{
    [FieldOffset(0)] public HaselSchedulerTimeline* Ptr;
    // [FieldOffset(8)] public int; // refcount? allocation status?
}
