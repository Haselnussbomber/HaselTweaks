namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x280)]
public unsafe partial struct HaselSchedulerTimeline
{
    [FieldOffset(0x34)] public float CurrentTimestamp; // inside base class (TimelineController)

    [MemberFunction("E8 ?? ?? ?? ?? EB CA 48 8B 4C 24")]
    public partial void Update(float delta, byte a3);
}
