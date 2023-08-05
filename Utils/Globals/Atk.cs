using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils.Globals;

public static unsafe class Atk
{
    public static T* GetNode<T>(AtkUnitBase* addon, uint nodeId) where T : unmanaged
        => addon == null ? null : (T*)addon->UldManager.SearchNodeById(nodeId);

    public static T* GetNode<T>(AtkComponentBase* component, uint nodeId) where T : unmanaged
        => component == null ? null : (T*)component->UldManager.SearchNodeById(nodeId);

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
        => SetAlpha(GetNode<AtkResNode>(addon, nodeId), alpha);

    public static void SetVisibility(AtkResNode* node, bool visible)
    {
        if (node == null || (visible && node->IsVisible) || (!visible && !node->IsVisible))
            return;

        node->ToggleVisibility(visible);
    }

    public static void SetVisibility(AtkUnitBase* addon, uint nodeId, bool visible)
        => SetVisibility(GetNode<AtkResNode>(addon, nodeId), visible);

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
