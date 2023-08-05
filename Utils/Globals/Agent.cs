using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Utils.Globals;

public static unsafe class Agent
{
    public static T* GetAgent<T>(AgentId id) where T : unmanaged
        => (T*)AgentModule.Instance()->GetAgentByInternalID((uint)id);
}
