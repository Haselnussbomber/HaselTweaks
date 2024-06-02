using FFXIVClientStructs.FFXIV.Component.Text;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0xE60)]
public unsafe partial struct HaselRaptureTextModule
{
    [FieldOffset(0x00)] public TextModule TextModule;

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B D0 48 8B CB E8 ?? ?? ?? ?? 4C 8D 87")]
    public partial byte* FormatAddonText2Int(uint addonRowId, int value);
}
