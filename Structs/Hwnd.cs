namespace HaselTweaks.Structs;

public partial struct Hwnd
{
    [MemberFunction("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 49 8B F8 C6 05")]
    public static partial ulong WindowProcHandler(nint hwnd, int uMsg, int wParam);
}
