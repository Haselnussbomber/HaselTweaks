using System;
using Dalamud.Game;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

public unsafe class NaviMapOpacity : Tweak
{
    public override string Name => "Navi Map Opacity";

    public Configuration Config => Plugin.Config.Tweaks.NaviMapOpacity;

    public class Configuration
    {
        [ConfigField(Description = "Changes collision box to square instead of round.", OnChange = nameof(OnChangeCollision))]
        public bool Square = false;

        [ConfigField(Max = 1)]
        public float DefaultOpacity = 0.8f;

        [ConfigField(Max = 1)]
        public float HoverOpacity = 1f;

        [ConfigField(OnChange = nameof(OnChangeVisibility))]
        public bool HideCoords = true;

        [ConfigField(OnChange = nameof(OnChangeVisibility))]
        public bool HideWeather = true;
    }

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
        SetVisibility(addon, true);

        // add back circular collision flag
        SetCollision(addon, false);
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

            SetCollision(addon, Config.Square);

            setupComplete = true;
        }

        SetVisibility(addon, isHovering);
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

    private void OnChangeCollision()
    {
        var addon = Utils.GetUnitBase("_NaviMap");
        if (addon == null) return;
        SetCollision(addon, Config.Square);
    }

    private void OnChangeVisibility()
    {
        var addon = Utils.GetUnitBase("_NaviMap");
        if (addon == null) return;
        Utils.SetVisibility(addon, (uint)NodeId.Coords, !Config.HideCoords);
        Utils.SetVisibility(addon, (uint)NodeId.Weather, !Config.HideWeather);
    }

    private void SetVisibility(AtkUnitBase* addon, bool hovered)
    {
        if (Config.HideCoords) Utils.SetVisibility(addon, (uint)NodeId.Coords, hovered);
        if (Config.HideWeather) Utils.SetVisibility(addon, (uint)NodeId.Weather, hovered);
        Utils.SetAlpha(addon, (uint)NodeId.Map, hovered ? Config.HoverOpacity : Config.DefaultOpacity);
    }

    private void SetCollision(AtkUnitBase* addon, bool square)
    {
        var collisionNode = Utils.GetNode(addon, (uint)NodeId.Collision);
        if (collisionNode == null) return;

        if (square)
        {
            // remove circular collision flag
            collisionNode->Flags_2 &= ~(uint)(1 << 23);
        }
        else
        {
            // add circular collision flag
            collisionNode->Flags_2 |= 1 << 23;
        }
    }
}
