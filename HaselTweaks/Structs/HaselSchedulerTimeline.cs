namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x280)]
public unsafe partial struct HaselSchedulerTimeline
{
    [FieldOffset(0x34)] public float CurrentTimestamp; // inside base class (TimelineController)

    [FieldOffset(0xA8)] public byte* ActionTimelineKey;

    public readonly bool IsIdleAnimation => ActionTimelineKey != null && Marshal.PtrToStringUTF8((nint)ActionTimelineKey) == "normal/idle";

    [MemberFunction("E8 ?? ?? ?? ?? EB CA 48 8B 4C 24")]
    public readonly partial void UpdateBanner(float delta, byte a3 = 0);
}
