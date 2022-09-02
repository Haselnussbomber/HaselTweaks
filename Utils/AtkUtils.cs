using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

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

    public static AtkUnitBase* GetHighestAtkUnitBaseAtPosition()
    {
        var position = ImGui.GetMousePos() - ImGuiHelpers.MainViewport.Pos;

        var atkUnitList = &AtkStage.GetSingleton()->RaptureAtkUnitManager->AtkUnitManager.DepthLayerFiveList;
        var unitBaseArray = &atkUnitList->AtkUnitEntries;

        for (var i = 0; i < atkUnitList->Count; i++)
        {
            var unitBase = unitBaseArray[i];
            if (unitBase->RootNode == null) continue;
            if (!(unitBase->IsVisible && unitBase->RootNode->IsVisible)) continue;

            if (unitBase->X > position.X || unitBase->Y > position.Y) continue;
            if (unitBase->X + unitBase->RootNode->Width < position.X) continue;
            if (unitBase->Y + unitBase->RootNode->Height < position.Y) continue;

            var name = Marshal.PtrToStringAnsi((IntPtr)unitBase->Name);
            if (name == null) continue;

            return unitBase;
        }

        return null;
    }

    public static Vector2 GetNodePosition(AtkResNode* node)
    {
        var pos = new Vector2(node->X, node->Y);
        var par = node->ParentNode;
        while (par != null)
        {
            pos *= new Vector2(par->ScaleX, par->ScaleY);
            pos += new Vector2(par->X, par->Y);
            par = par->ParentNode;
        }
        return pos;
    }

    public static bool IsNodeVisible(AtkResNode* node)
    {
        if (node == null) return false;
        while (node != null)
        {
            if ((node->Flags & (short)NodeFlags.Visible) != (short)NodeFlags.Visible)
                return false;
            node = node->ParentNode;
        }
        return true;
    }

    public static Vector2 GetNodeScale(AtkResNode* node)
    {
        if (node == null) return new Vector2(1, 1);
        var scale = new Vector2(node->ScaleX, node->ScaleY);
        while (node->ParentNode != null)
        {
            node = node->ParentNode;
            scale *= new Vector2(node->ScaleX, node->ScaleY);
        }
        return scale;
    }

    public static Vector2 GetNodeScaledSize(AtkResNode* node)
    {
        return new Vector2(node->Width, node->Height) * GetNodeScale(node);
    }

    public static bool IsCursorIntersecting(AtkUldManager UldManager, AtkCollisionNode* collisionNode)
    {
        var position = ImGui.GetMousePos() - ImGuiHelpers.MainViewport.Pos;

        for (var i = 0; i < UldManager.NodeListCount; i++)
        {
            var node = UldManager.NodeList[i];

            if (node == null || !IsNodeVisible(node)) continue;

            var pos1 = GetNodePosition(node); // top/left
            if (pos1.X > position.X) continue;
            if (pos1.Y > position.Y) continue;

            var pos2 = GetNodePosition(node) + GetNodeScaledSize(node); // bottom/right
            if (pos2.X < position.X) continue;
            if (pos2.Y < position.Y) continue;

            if ((IntPtr)node == (IntPtr)collisionNode)
            {
                return true;
            }

            if ((int)node->Type >= 1000 && IsCursorIntersecting(((AtkComponentNode*)node)->Component->UldManager, collisionNode))
            {
                return true;
            }
        }

        return false;
    }
}
