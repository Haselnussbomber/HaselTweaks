using FFXIVClientStructs.FFXIV.Common.Math;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct CharacterCB0 // Character + 0xCB0
{
    [FieldOffset(0x604)] public Vector2 HeadDirection;
    [FieldOffset(0x60C)] public Vector2 EyeDirection;
}
