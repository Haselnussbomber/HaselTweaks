using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace HaselTweaks;

public static unsafe class Utils
{
    public static AtkUnitBase* GetUnitBase(string name, int index = 1)
    {
        return (AtkUnitBase*)Service.GameGui.GetAddonByName(name, index);
    }

    public static AtkResNode* GetNode(AtkUnitBase* addon, uint nodeId)
    {
        if (addon == null) return null;
        return addon->UldManager.SearchNodeById(nodeId);
    }

    public static void SetAlpha(AtkUnitBase* addon, uint nodeId, float alpha)
    {
        SetAlpha(GetNode(addon, nodeId), alpha);
    }

    public static void SetAlpha(AtkResNode* node, float alpha)
    {
        if (node == null) return;
        var alphaByte = (byte)Math.Floor(alpha >= 1.0 ? 255 : alpha * 256.0);
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
