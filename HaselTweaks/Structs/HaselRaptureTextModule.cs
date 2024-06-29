using FFXIVClientStructs.FFXIV.Component.Text;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0xE60)]
public unsafe partial struct HaselRaptureTextModule
{
    [FieldOffset(0x00)] public TextModule TextModule;

    [MemberFunction("E8 ?? ?? ?? ?? 41 8D 55 0B")]
    public partial byte* FormatAddonText2Int(uint addonRowId, int value);
}
