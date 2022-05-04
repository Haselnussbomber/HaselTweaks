using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

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

    public static AtkUnitBase* GetHighestAtkUnitBaseAtPosition(List<string>? allowList = null, List<string>? ignoreList = null)
    {
        var position = ImGui.GetMousePos() - ImGuiHelpers.MainViewport.Pos;

        var getName = delegate (byte* ptr) { return Marshal.PtrToStringAnsi((IntPtr)ptr); };

        var stage = AtkStage.GetSingleton();
        var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
        var unitManager = &unitManagers[4];
        var unitBaseArray = &unitManager->AtkUnitEntries;

        for (var j = 0; j < unitManager->Count; j++)
        {
            var unitBase = unitBaseArray[j];
            if (unitBase->RootNode == null) continue;
            if (!(unitBase->IsVisible && unitBase->RootNode->IsVisible)) continue;

            if (unitBase->X > position.X || unitBase->Y > position.Y) continue;
            if (unitBase->X + unitBase->RootNode->Width < position.X) continue;
            if (unitBase->Y + unitBase->RootNode->Height < position.Y) continue;
            if (allowList != null && !allowList.Contains(getName(unitBase->Name))) continue;
            if (ignoreList != null && ignoreList.Contains(getName(unitBase->Name))) continue;

            return unitBase;
        }

        return null;
    }
}
