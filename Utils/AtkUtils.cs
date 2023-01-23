using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Utils;

public static unsafe class AtkUtils
{
    public static AtkUnitBase* GetUnitBase(string name, int index = 1)
    {
        return (AtkUnitBase*)Service.GameGui.GetAddonByName(name, index);
    }

    public static AtkResNode* GetNode(AtkUnitBase* addon, uint nodeId)
    {
        return addon == null ? null : addon->UldManager.SearchNodeById(nodeId);
    }

    public static void SetAlpha(AtkUnitBase* addon, uint nodeId, float alpha)
    {
        SetAlpha(GetNode(addon, nodeId), alpha);
    }

    public static void SetAlpha(AtkResNode* node, float alpha)
    {
        if (node == null) return;
        var alphaByte = (byte)(alpha >= 1 ? 255 : Math.Floor(alpha * 255f));
        if (node->Color.A == alphaByte) return;
        node->Color.A = alphaByte;
    }

    public static void SetVisibility(AtkUnitBase* addon, uint nodeId, bool visible)
    {
        SetVisibility(GetNode(addon, nodeId), visible);
    }

    public static void SetVisibility(AtkResNode* node, bool visible)
    {
        if (node == null || (visible && node->IsVisible) || (!visible && !node->IsVisible)) return;
        node->ToggleVisibility(visible);
    }
}
