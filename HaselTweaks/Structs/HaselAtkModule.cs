using FFXIVClientStructs.FFXIV.Client.UI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x8250)]
public unsafe struct HaselAtkModule
{
    public static HaselAtkModule* Instance()
        => (HaselAtkModule*)RaptureAtkModule.Instance();

    [FieldOffset(0x5CA0)] public byte ActiveColorThemeType;
}
