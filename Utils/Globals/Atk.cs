using System.Numerics;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils.Globals;

#pragma warning disable CS8500

public static unsafe class Atk
{
    public static string GetAddonName(AtkUnitBase* addon)
        => addon == null ? "" : MemoryHelper.ReadString((nint)addon->Name, 0x20);

    public static AtkResNode* GetNode(AtkUnitBase* addon, uint nodeId)
        => addon == null ? null : addon->UldManager.SearchNodeById(nodeId);

    public static bool TryGetNode(AtkUnitBase* addon, uint nodeId, out AtkResNode* node)
        => (node = GetNode(addon, nodeId)) != null;

    public static T* GetNode<T>(AtkUnitBase* addon, uint nodeId)
        => (T*)GetNode(addon, nodeId);

    public static T* GetNode<T>(AtkComponentBase* component, uint nodeId)
        => component == null ? null : (T*)component->UldManager.SearchNodeById(nodeId);

    public static T* GetNode<T>(AtkComponentNode* node, uint nodeId)
        => node == null ? null : (T*)node->Component->UldManager.SearchNodeById(nodeId);

    public static void SetAlpha(AtkResNode* node, float alpha)
    {
        if (node == null)
            return;

        var alphaByte = (byte)(alpha >= 1 ? 255 : Math.Floor(alpha * 255f));
        if (node->Color.A == alphaByte)
            return;

        node->Color.A = alphaByte;
    }

    public static void SetAlpha(AtkUnitBase* addon, uint nodeId, float alpha)
        => SetAlpha(GetNode(addon, nodeId), alpha);

    public static void SetVisibility(AtkResNode* node, bool visible)
    {
        if (node == null || (visible && node->IsVisible) || (!visible && !node->IsVisible))
            return;

        node->ToggleVisibility(visible);
    }

    public static void SetVisibility(AtkUnitBase* addon, uint nodeId, bool visible)
        => SetVisibility(GetNode(addon, nodeId), visible);

    public static Vector2 GetNodeScale(AtkResNode* node)
    {
        if (node == null)
            return Vector2.One;

        var scale = new Vector2(node->ScaleX, node->ScaleY);

        while (node->ParentNode != null)
        {
            node = node->ParentNode;
            scale *= new Vector2(node->ScaleX, node->ScaleY);
        }

        return scale;
    }
}

#pragma warning restore CS8500
