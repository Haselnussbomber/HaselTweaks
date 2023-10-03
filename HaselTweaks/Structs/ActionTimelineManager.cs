namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct ActionTimelineManager
{
    [FieldOffset(0x80)] public SchedulerTimeline** BaseAnimation;

    [MemberFunction("E8 ?? ?? ?? ?? EB 48 48 8B 46 08")]
    public readonly partial void PlayActionTimeline(ushort introId, ushort loopId = 0, nint a4 = 0);
}
