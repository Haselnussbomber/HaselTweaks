using FFXIVClientStructs.FFXIV.Common.Math;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
public struct HaselCharacter
{
    [FieldOffset(0xCB0)] public GazeContainer Gaze;

    [StructLayout(LayoutKind.Explicit, Size = 0x620)]
    public struct GazeContainer
    {
        [FieldOffset(0x604)] public Vector2 BannerHeadDirection;
        [FieldOffset(0x60C)] public Vector2 BannerEyesDirection;
    }
}
