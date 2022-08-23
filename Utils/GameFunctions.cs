using Dalamud.Utility.Signatures;

namespace HaselTweaks.Utils;

public unsafe class GameFunctions
{
    public GameFunctions()
    {
        SignatureHelper.Initialise(this);
    }

    [Signature("4C 89 7C 24 ?? 48 8D 1D ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
    private readonly byte* AetherCurrentUnlocksPtr = null!; // UIState.Instance().PlayerState.AetherCurrentUnlocks => (IntPtr)UIState.Instance() + 0xA38 + 0x4E1

    // see: E8 ?? ?? ?? ?? 8B F8 85 C0 74 33
    public bool IsAetherCurrentUnlocked(uint rowId)
    {
        var id = rowId - 0x2B0000;
        var pos = id >> 3;
        var flag = (byte)(1 << (int)(id - 8 * pos));
        return (flag & AetherCurrentUnlocksPtr[pos]) != 0;
    }
}
