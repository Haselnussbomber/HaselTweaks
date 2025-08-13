using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = AgentHousingPlant.StructSize)]
public struct HaselAgentHousingPlant
{
    [FieldOffset(0x38)] public uint State; // which UI dialog is shown
}
