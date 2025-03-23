using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = RaptureTextModule.StructSize)]
public unsafe partial struct HaselRaptureTextModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 41 8D 55 0B")]
    public partial byte* FormatAddonText2Int(uint addonRowId, int value);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B D8 EB 38")]
    public partial byte* FormatAddonText2IntIntUInt(uint addonRowId, int value1, int value2, uint value3);
}
