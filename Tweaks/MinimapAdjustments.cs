using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

public unsafe class MinimapAdjustments : Tweak
{
    public override string Name => "Minimap Adjustments";
    public override string Description => "Mini changes for the Minimap. :)";

    public Configuration Config => Plugin.Config.Tweaks.MinimapAdjustments;

    public class Configuration
    {
        [ConfigField(Label = "Square Collision", Description = "Changes collision box to from round to square.")]
        public bool Square = false;

        [ConfigField(Label = "Default Opacity", Max = 1)]
        public float DefaultOpacity = 0.8f;

        [ConfigField(Label = "Hover Opacity", Max = 1)]
        public float HoverOpacity = 1f;

        [ConfigField(Label = "Hide Coordinates", Description = "Visible on hover.", OnChange = nameof(UpdateVisibility))]
        public bool HideCoords = true;

        [ConfigField(Label = "Hide Weather", Description = "Visible on hover.", OnChange = nameof(UpdateVisibility))]
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

    // AddonNaviMap_ReceiveEvent
    [AutoHook, Signature("48 89 5C 24 ?? 57 48 83 EC 30 0F B7 C2 49 8B F9 83 C0 FB", DetourName = nameof(OnEvent))]
    private Hook<OnAtkEventDelegate> Hook { get; init; } = null!;
    private delegate void* OnAtkEventDelegate(AtkUnitBase* addon, AtkEventType eventType, int eventParam, AtkEventListener* listener, AtkResNode* nodeParam);

    private bool isHovering = false;

    public override void Disable()
    {
        var addon = Utils.GetUnitBase("_NaviMap");
        if (addon == null) return;

        // reset visibility
        SetVisibility(addon, true);

        // add back circular collision flag
        SetCollision(addon, false);
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        var addon = Utils.GetUnitBase("_NaviMap");
        if (addon == null) return;
        SetVisibility(addon, isHovering);
        SetCollision(addon, Config.Square);
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

    private void UpdateVisibility()
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
        var hasCircularCollisionFlag = (collisionNode->Flags_2 & (1 << 23)) != 0;

        if (square && hasCircularCollisionFlag)
            collisionNode->Flags_2 &= ~(uint)(1 << 23); // remove circular collision flag
        else if (!square && !hasCircularCollisionFlag)
            collisionNode->Flags_2 |= 1 << 23; // add circular collision flag
    }
}
