namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselUIGlobals
{
    [MemberFunction("E8 ?? ?? ?? ?? 39 43 30 75 08")]
    public static partial uint GenerateEquippedItemsChecksum();
}
