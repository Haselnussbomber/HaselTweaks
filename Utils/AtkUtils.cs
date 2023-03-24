using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils;

#pragma warning disable CS8500

public static unsafe class AtkUtils
{
    #region GetAddonByName

    public static AtkUnitBase* GetAddon(string name, int index = 1)
        => (AtkUnitBase*)Service.GameGui.GetAddonByName(name, index);

    public static bool GetAddon(string name, int index, out AtkUnitBase* addon)
        => (addon = (AtkUnitBase*)Service.GameGui.GetAddonByName(name, index)) != null;

    public static bool GetAddon(string name, out AtkUnitBase* addon)
        => GetAddon(name, 1, out addon);

    public static T* GetAddon<T>(string name, int index = 1)
        => (T*)GetAddon(name, index);

    public static bool GetAddon<T>(string name, int index, out T* addon)
        => (addon = (T*)GetAddon(name, index)) != null;

    public static bool GetAddon<T>(string name, out T* addon)
        => GetAddon(name, 1, out addon);

    #endregion

    #region GetAddonById

    public static AtkUnitBase* GetAddon(ushort id)
        => IsAddonReady(id) ? AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonById(id) : null;

    public static bool GetAddon(ushort id, out AtkUnitBase* addon)
        => (addon = GetAddon(id)) != null;

    public static AtkUnitBase* GetAddon(uint id)
        => GetAddon((ushort)id);

    public static bool GetAddon(uint id, out AtkUnitBase* addon)
        => GetAddon((ushort)id, out addon);

    public static AtkUnitBase* GetAddon(AgentInterface* agent)
        => agent != null && agent->IsAgentActive() ? GetAddon((ushort)agent->GetAddonID()) : null;

    public static bool GetAddon(AgentInterface* agent, out AtkUnitBase* addon)
        => (addon = GetAddon(agent)) != null;

    public static AtkUnitBase* GetAddon(AgentId id)
        => GetAddon(GetAgent(id));

    public static bool GetAddon(AgentId id, out AtkUnitBase* addon)
        => GetAddon(GetAgent(id), out addon);

    public static T* GetAddon<T>(ushort id)
        => (T*)GetAddon(id);

    public static bool GetAddon<T>(ushort id, out T* addon)
        => (addon = (T*)GetAddon(id)) != null;

    public static T* GetAddon<T>(uint id)
        => GetAddon<T>((ushort)id);

    public static bool GetAddon<T>(uint id, out T* addon)
        => GetAddon((ushort)id, out addon);

    public static T* GetAddon<T>(AgentInterface* agent)
        => agent != null && agent->IsAgentActive() ? GetAddon<T>((ushort)agent->GetAddonID()) : null;

    public static bool GetAddon<T>(AgentInterface* agent, out T* addon)
        => (addon = GetAddon<T>(agent)) != null;

    public static T* GetAddon<T>(AgentId id)
        => GetAddon<T>(GetAgent(id));

    public static bool GetAddon<T>(AgentId id, out T* addon)
        => GetAddon(GetAgent(id), out addon);

    #endregion

    #region IsAddonReady

    public static bool IsAddonReady(ushort id)
        => Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.IsAddonReady(id);
    public static bool IsAddonReady(uint id) => IsAddonReady((ushort)id);
    public static bool IsAddonReady(AtkUnitBase* unitBase) => IsAddonReady(unitBase->ID);
    public static bool IsAddonReady(AgentInterface* agent)
        => agent != null && agent->IsAgentActive() && IsAddonReady(agent->GetAddonID());
    public static bool IsAddonReady(AgentId id) => IsAddonReady(GetAgent(id));

    #endregion

    #region GetAgent

    public static AgentInterface* GetAgent(uint id)
        => AgentModule.Instance()->GetAgentByInternalID(id);

    public static bool GetAgent(uint id, out AgentInterface* agent)
        => (agent = GetAgent(id)) != null;

    public static AgentInterface* GetAgent(AgentId id)
        => AgentModule.Instance()->GetAgentByInternalId(id);

    public static bool GetAgent(AgentId id, out AgentInterface* agent)
        => (agent = GetAgent(id)) != null;

    /*
     * too slow
    public static T* GetAgent<T>()
    {
        var attr = typeof(T).GetCustomAttribute<AgentAttribute>();
        return attr == null ? null : (T*)GetAgent(attr.ID);
    }
    */

    public static T* GetAgent<T>(AgentId id)
        => (T*)GetAgent(id);

    public static bool GetAgent<T>(AgentId id, out T* agent)
        => (agent = GetAgent<T>(id)) != null;

    #endregion

    #region AtkUnitBase

    public static string GetAddonName(AtkUnitBase* addon)
        => addon == null ? "" : MemoryHelper.ReadString((nint)addon->Name, 0x20).Split("\0", 1)[0];

    public static AtkResNode* GetNode(AtkUnitBase* addon, uint nodeId)
        => addon == null ? null : addon->UldManager.SearchNodeById(nodeId);

    public static bool GetNode(AtkUnitBase* addon, uint nodeId, out AtkResNode* node)
        => (node = GetNode(addon, nodeId)) != null;

    public static T* GetNode<T>(AtkUnitBase* addon, uint nodeId)
        => (T*)GetNode(addon, nodeId);

    public static bool GetNode<T>(AtkUnitBase* addon, uint nodeId, out T* node)
        => (node = GetNode<T>(addon, nodeId)) != null;

    public static T* GetNode<T>(AtkComponentBase* component, uint nodeId)
        => component == null ? null : (T*)component->UldManager.SearchNodeById(nodeId);

    public static bool GetNode<T>(AtkComponentBase* component, uint nodeId, out T* node)
        => (node = GetNode<T>(component, nodeId)) != null;

    public static T* GetNode<T>(AtkComponentNode* node, uint nodeId)
        => node == null ? null : (T*)node->Component->UldManager.SearchNodeById(nodeId);

    public static bool GetNode<T>(AtkComponentNode* node, uint nodeId, out T* outNode)
        => (outNode = GetNode<T>(node, nodeId)) != null;

    #endregion

    #region SetAlpha

    public static void SetAlpha(AtkResNode* node, float alpha)
    {
        if (node == null) return;
        var alphaByte = (byte)(alpha >= 1 ? 255 : Math.Floor(alpha * 255f));
        if (node->Color.A == alphaByte) return;
        node->Color.A = alphaByte;
    }
    public static void SetAlpha(AtkUnitBase* addon, uint nodeId, float alpha) => SetAlpha(GetNode(addon, nodeId), alpha);

    #endregion

    #region SetVisibility

    public static void SetVisibility(AtkResNode* node, bool visible)
    {
        if (node == null || (visible && node->IsVisible) || (!visible && !node->IsVisible)) return;
        node->ToggleVisibility(visible);
    }
    public static void SetVisibility(AtkUnitBase* addon, uint nodeId, bool visible) => SetVisibility(GetNode(addon, nodeId), visible);

    #endregion
}

#pragma warning restore CS8500
