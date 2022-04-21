using Dalamud.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;

namespace HaselTweaks.Tweaks;

public unsafe class NaviMapOpacity : BaseTweak
{
    public override string Name => "Navi Map Opacity";

    private enum NodeId : uint
    {
        Collision = 19,
        Base = 18,
        Coords = 5,
        Weather = 14,
        Map = 17,
    }

    private Hook<OnAtkEventDelegate>? Hook = null;
    private delegate void* OnAtkEventDelegate(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEventListener* listener, AtkResNode* nodeParam);

    private bool setupComplete = false;
    private bool isHovering = false;

    public override void Enable()
    {
        Hook?.Enable();
    }

    public override void Disable()
    {
        Hook?.Disable();

        var addon = Utils.GetUnitBase("_NaviMap");
        if (addon == null) return;

        // reset visibility
        UpdateVisibility(addon, true);

        // add back circular collision flag
        var collisionNode = Utils.GetNode(addon, (uint)NodeId.Collision);
        collisionNode->Flags_2 &= 1 << 23;
    }

    public override void Dispose()
    {
        Hook?.Dispose();
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = Utils.GetUnitBase("_NaviMap");
        if (addon == null) return;

        if (!setupComplete)
        {
            if (Hook == null)
            {
                // https://github.com/daemitus/ClickIt/blob/ee39b1a2/ClickIt/Clicks/ClickBase.cs#L62-L63
                var vtbl = (void**)addon->AtkEventListener.vtbl;
                var receiveEventAddress = new IntPtr(vtbl[2]);
                Hook = new Hook<OnAtkEventDelegate>(receiveEventAddress, OnEvent);
                Hook?.Enable();
            }

            // remove circular collision flag
            var collisionNode = Utils.GetNode(addon, (uint)NodeId.Collision);
            if (collisionNode != null)
            {
                collisionNode->Flags_2 &= ~(uint)(1 << 23);
            }

            setupComplete = true;
        }

        UpdateVisibility(addon, isHovering);
    }

    private void* OnEvent(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEventListener* listener, AtkResNode* nodeParam)
    {
        if ((eventType == AtkEventType.MouseMove || eventType == AtkEventType.MouseOver) && !isHovering)
        {
            isHovering = true;
        }
        else if (eventType == AtkEventType.MouseOut && isHovering)
        {
            isHovering = false;
        }

        return Hook!.Original(addon, eventType, eventParam, listener, nodeParam);
    }

    private static void UpdateVisibility(AtkUnitBase* addon, bool visible)
    {
        Utils.SetVisibility(addon, (uint)NodeId.Coords, visible);
        Utils.SetVisibility(addon, (uint)NodeId.Weather, visible);
        Utils.SetAlpha(addon, (uint)NodeId.Map, visible ? 1f : 0.8f);
    }
}
