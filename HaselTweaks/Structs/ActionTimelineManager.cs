namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct ActionTimelineManager
{
    [MemberFunction("E8 ?? ?? ?? ?? EB 55 48 8B 4F 08")]
    public readonly partial void PlayActionTimeline(ushort introId, ushort loopId = 0, nint a4 = 0);
}
