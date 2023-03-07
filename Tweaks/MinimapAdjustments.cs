using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

public unsafe class MinimapAdjustments : Tweak
{
    public override string Name => "Minimap Adjustments";
    public override string Description => "Mini changes for the Minimap. :)";

    public static Configuration Config => Plugin.Config.Tweaks.MinimapAdjustments;

    public class Configuration
    {
        [ConfigField(Label = "Square Collision", Description = "Changes collision box to from round to square.")]
        public bool Square = false;

        [ConfigField(Label = "Default Opacity", Max = 1, DefaultValue = 0.8f)]
        public float DefaultOpacity = 0.8f;

        [ConfigField(Label = "Hover Opacity", Max = 1, DefaultValue = 1f)]
        public float HoverOpacity = 1f;

        [ConfigField(Label = "Hide Coordinates", Description = "Visible on hover.", OnChange = nameof(OnConfigChange))]
        public bool HideCoords = true;

        [ConfigField(Label = "Hide Weather", Description = "Visible on hover.", OnChange = nameof(OnConfigChange))]
        public bool HideWeather = true;
    }

    private enum NodeId : uint
    {
        Collision = 19,
        Coords = 5,
        Weather = 14,
        Map = 17,
    }

    public override void Disable()
    {
        var addon = GetAddon("_NaviMap");
        if (addon == null) return;

        // reset visibility
        UpdateVisibility(addon, true);

        // add back circular collision flag
        UpdateCollision(addon, false);
    }

    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        var addon = GetAddon("_NaviMap");
        if (addon == null) return;
        UpdateVisibility(addon, Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.IntersectingAddon == addon);
        UpdateCollision(addon, Config.Square);
    }

    private static void OnConfigChange()
    {
        var addon = GetAddon("_NaviMap");
        SetVisibility(addon, (uint)NodeId.Coords, !Config.HideCoords);
        SetVisibility(addon, (uint)NodeId.Weather, !Config.HideWeather);
    }

    private static void UpdateVisibility(AtkUnitBase* addon, bool hovered)
    {
        if (Config.HideCoords) SetVisibility(addon, (uint)NodeId.Coords, hovered);
        if (Config.HideWeather) SetVisibility(addon, (uint)NodeId.Weather, hovered);
        SetAlpha(addon, (uint)NodeId.Map, hovered ? Config.HoverOpacity : Config.DefaultOpacity);
    }

    private static void UpdateCollision(AtkUnitBase* addon, bool square)
    {
        var collisionNode = GetNode(addon, (uint)NodeId.Collision);
        if (collisionNode == null) return;

        var hasCircularCollisionFlag = (collisionNode->Flags_2 & (1 << 23)) != 0;

        if (square && hasCircularCollisionFlag)
            collisionNode->Flags_2 &= ~(uint)(1 << 23); // remove circular collision flag
        else if (!square && !hasCircularCollisionFlag)
            collisionNode->Flags_2 |= 1 << 23; // add circular collision flag
    }
}
