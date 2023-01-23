using System.Reflection;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;

namespace HaselTweaks.Utils;

#pragma warning disable CS8500

public static unsafe class AtkUtils
{
    #region GetAddonByName

    public static AtkUnitBase* GetAddon(string name, int index = 1)
        => (AtkUnitBase*)Service.GameGui.GetAddonByName(name, index);

    public static T* GetAddon<T>(string name, int index = 1) => (T*)GetAddon(name, index);

    #endregion

    #region GetAddonById

    public static AtkUnitBase* GetAddon(ushort id)
        => IsAddonReady(id) ? AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonById(id) : null;
    public static T* GetAddon<T>(ushort id) => (T*)GetAddon(id);
    public static T* GetAddon<T>(uint id) => GetAddon<T>((ushort)id);
    public static T* GetAddon<T>(AgentInterface* agent)
        => agent->IsAgentActive() ? GetAddon<T>((ushort)agent->GetAddonID()) : null;
    public static T* GetAddon<T>(AgentId id) => GetAddon<T>(GetAgent(id));

    #endregion

    #region IsAddonReady

    public static bool IsAddonReady(ushort id)
        => RaptureAtkModule.Instance->IsAddonReady(id);
    public static bool IsAddonReady(uint id) => IsAddonReady((ushort)id);
    public static bool IsAddonReady(AtkUnitBase* unitBase) => IsAddonReady(unitBase->ID);
    public static bool IsAddonReady(AgentInterface* agent)
        => agent->IsAgentActive() ? IsAddonReady(agent->GetAddonID()) : false;
    public static bool IsAddonReady(AgentId id) => IsAddonReady(GetAgent(id));

    #endregion

    #region GetAgent

    public static AgentInterface* GetAgent(uint id)
        => AgentModule.Instance()->GetAgentByInternalID(id);

    public static AgentInterface* GetAgent(AgentId id)
        => AgentModule.Instance()->GetAgentByInternalId(id);

    /*
     * too slow
    public static T* GetAgent<T>()
    {
        var attr = typeof(T).GetCustomAttribute<AgentAttribute>();
        return attr == null ? null : (T*)GetAgent(attr.ID);
    }
    */

    public static T* GetAgent<T>(AgentId id) => (T*)GetAgent(id);

    #endregion

    #region AtkUnitBase

    public static AtkResNode* GetNode(AtkUnitBase* addon, uint nodeId)
        => addon == null ? null : addon->UldManager.SearchNodeById(nodeId);

    public static T* GetNode<T>(AtkUnitBase* addon, uint nodeId)
        => (T*)GetNode(addon, nodeId);

    public static T* GetNode<T>(AtkComponentBase* node, uint nodeId)
        => node == null ? null : (T*)node->UldManager.SearchNodeById(nodeId);

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
