namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct TimelineController
{
    [FieldOffset(0x34)] public float CurrentTimestamp;
}
