namespace HaselTweaks.Structs;

// made-up name because I have no idea what else it does
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0)]
public unsafe partial struct PreloadManger
{
    [StaticAddress("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 41 80 7F", 3)]
    public static partial PreloadManger* Instance();

    [MemberFunction("E8 ?? ?? ?? ?? 41 80 7F ?? ?? 75 16")]
    public partial void PreloadTerritory(int a2, nint bg, byte a4, uint level, uint territoryType);
}
