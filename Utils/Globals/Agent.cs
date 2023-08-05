using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils.Globals;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

public static unsafe class Agent
{
    public static AgentInterface* GetAgent(AgentId id)
        => AgentModule.Instance()->GetAgentByInternalId(id);

    public static T* GetAgent<T>(AgentId id)
        => (T*)GetAgent(id);

    public static bool TryGetAgent<T>(AgentId id, out T* agent)
        => (agent = GetAgent<T>(id)) != null;

}

#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
