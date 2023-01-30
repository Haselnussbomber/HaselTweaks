using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct RaptureAtkModule
{
    public static RaptureAtkModule* Instance => (RaptureAtkModule*)Framework.Instance()->GetUiModule()->GetRaptureAtkModule();

    #region inherited from AtkModule

    [FieldOffset(0x1B10)] public AtkUnitBase* IntersectingAddon;
    [FieldOffset(0x1B18)] public AtkCollisionNode* IntersectingCollisionNode;

    [VirtualFunction(26)]
    public partial bool IsAddonReady(uint addonId);

    #endregion
}
