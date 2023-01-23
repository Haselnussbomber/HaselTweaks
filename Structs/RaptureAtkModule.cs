using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct RaptureAtkModule
{
    public static RaptureAtkModule* Instance => (RaptureAtkModule*)Framework.Instance()->GetUiModule()->GetRaptureAtkModule();

    // actually inherited from AtkModule
    [VirtualFunction(26)]
    public partial bool IsAddonReady(uint addonId);
}
