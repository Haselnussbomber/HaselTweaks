namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public partial struct HaselActionTimelineManager
{
    [MemberFunction("E8 ?? ?? ?? ?? EB 48 48 8B 46 08")]
    public readonly partial void PlayActionTimeline(ushort introId, ushort loopId = 0, nint a4 = 0);
}
