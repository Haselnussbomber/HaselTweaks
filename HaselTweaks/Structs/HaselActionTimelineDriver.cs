using FFXIVClientStructs.FFXIV.Client.Game;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselActionTimelineDriver
{
    [FixedSizeArray<Pointer<Pointer<HaselSchedulerTimeline>>>(ActionTimelineDriver.TimelineSlotCount)]
    [FieldOffset(0x70)] public unsafe fixed byte SchedulerTimelines[ActionTimelineDriver.TimelineSlotCount * 0x8];

    public HaselSchedulerTimeline* GetSchedulerTimeline(ActionTimelineSlots slot)
    {
        var baseTimelineSlot = SchedulerTimelinesSpan[(int)slot].Value;
        return baseTimelineSlot == null ? null : baseTimelineSlot->Value;
    }
}
