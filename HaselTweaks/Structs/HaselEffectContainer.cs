using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x50)]
public unsafe partial struct HaselEffectContainer
{
    [FieldOffset(0)] public ContainerInterface ContainerInterface;

    [FieldOffset(0x34)] public int UnkMountTiltField34;
    [FieldOffset(0x38)] public int UnkMountTiltField38;
    [FieldOffset(0x3C)] public byte Unk3C;
    [FieldOffset(0x40)] public byte TiltParam1Type;
    [FieldOffset(0x44)] public float TiltParam1Value;
    [FieldOffset(0x48)] public byte TiltParam2Type;
    [FieldOffset(0x4C)] public float TiltParam2Value;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B CE E8 ?? ?? ?? ?? 48 83 BE ?? ?? ?? ?? ?? 74 26")]
    public partial void Setup();
}
