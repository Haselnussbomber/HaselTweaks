namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct CIEXYZTRIPLE
{
    public CIEXYZ ciexyzRed;
    public CIEXYZ ciexyzGreen;
    public CIEXYZ ciexyzBlue;
}
