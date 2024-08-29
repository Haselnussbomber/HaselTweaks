namespace HaselTweaks.Structs;

// TODO: Remove when https://github.com/aers/FFXIVClientStructs/pull/1096 is merged
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0xB808)]
public unsafe partial struct HaselRaptureGearsetModule
{
    [MemberFunction("48 89 6C 24 ?? 57 48 83 EC 20 48 8B F9 48 63 EA")]
    public partial int UpdateGearset(int gearsetId);
}
