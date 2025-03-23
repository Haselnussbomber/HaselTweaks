using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = TimelineContainer.StructSize)]
public partial struct HaselTimelineContainer
{
    [MemberFunction("E8 ?? ?? ?? ?? EB 48 48 8B 45 08")]
    public partial void PlayActionTimeline(ushort introId, ushort loopId = 0, nint a4 = 0);
}
