namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xE60)]
public unsafe partial struct HaselRaptureTextModule
{
    [MemberFunction("E8 ?? ?? ?? ?? 48 8B D0 48 8B CB E8 ?? ?? ?? ?? 4C 8D 87")]
    public readonly partial byte* FormatAddonText2Int(ulong addonRowId, int value);
}
