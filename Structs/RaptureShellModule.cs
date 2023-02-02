using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct RaptureShellModule
{
    public static RaptureShellModule* Instance => (RaptureShellModule*)Framework.Instance()->GetUiModule()->GetRaptureShellModule();

    [FieldOffset(0x1200)] public uint Flags; // set by E8 ?? ?? ?? ?? 83 FE 1C

    public bool IsTextCommandUnavailable => (Flags & 1) != 0;
}
