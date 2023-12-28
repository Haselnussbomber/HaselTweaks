namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselActionTimelineManager
{
    [FieldOffset(0x10)] public HaselActionTimelineDriver Driver;

    [FieldOffset(0x318 + 0x00)] public float BannerRequestedTimestamp;
    [FieldOffset(0x33F)] public byte BannerFlags2;

    [MemberFunction("E8 ?? ?? ?? ?? EB 48 48 8B 46 08")]
    public readonly partial void PlayActionTimeline(ushort introId, ushort loopId = 0, nint a4 = 0);
}
