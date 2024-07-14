using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x178)]
public unsafe struct HaselAgentGatheringNote
{
    public static HaselAgentGatheringNote* Instance() => (HaselAgentGatheringNote*)AgentModule.Instance()->GetAgentByInternalId(AgentId.GatheringNote);

    [FieldOffset(0xA0)] public uint ContextMenuItemId;
}
