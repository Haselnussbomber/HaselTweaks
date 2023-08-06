using System.Collections.Generic;
using System.Reflection;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Utils.Globals;

public static unsafe class Agent
{
    private static readonly Dictionary<Type, AgentId> AgentIdCache = new();

    public static T* GetAgent<T>(AgentId id) where T : unmanaged
        => (T*)AgentModule.Instance()->GetAgentByInternalID((uint)id);

    public static T* GetAgent<T>() where T : unmanaged
    {
        var type = typeof(T);

        if (!AgentIdCache.TryGetValue(type, out var id))
        {
            var attr = type.GetCustomAttribute<AgentAttribute>(false)
                ?? throw new Exception($"Agent {type.FullName} is missing AgentAttribute");

            AgentIdCache.Add(type, id = attr.ID);
        }

        return GetAgent<T>(id);
    }
}
