namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct SchedulerTimeline
{
    [FieldOffset(0x34)] public float CurrentTimestamp;

    [FieldOffset(0x44)] public float TbmhLength;
    [FieldOffset(0x48)] public float AnimationLength; // C010
}
