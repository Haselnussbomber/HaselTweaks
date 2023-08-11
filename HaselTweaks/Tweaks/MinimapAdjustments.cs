using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe class MinimapAdjustments : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.MinimapAdjustments;

    public class Configuration
    {
        [BoolConfig]
        public bool Square = false;

        [FloatConfig(Max = 1, DefaultValue = 0.8f)]
        public float DefaultOpacity = 0.8f;

        [FloatConfig(Max = 1, DefaultValue = 1)]
        public float HoverOpacity = 1f;

        [BoolConfig]
        public bool HideCoords = true;

        [BoolConfig]
        public bool HideWeather = true;
    }

    private static AtkUnitBase* NaviMap => GetAddon<AtkUnitBase>("_NaviMap");
    private static AtkResNode* CollisionNode => GetNode<AtkResNode>(NaviMap, 19);
    private static AtkResNode* CoordsNode => GetNode<AtkResNode>(NaviMap, 5);
    private static AtkResNode* WeatherNode => GetNode<AtkResNode>(NaviMap, 14);
    private static AtkResNode* MapNode => GetNode<AtkResNode>(NaviMap, 17);

    public override void Disable()
    {
        if (!IsAddonOpen("_NaviMap"))
            return;

        // reset visibility
        UpdateVisibility(true);

        // add back circular collision flag
        UpdateCollision(false);
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName is nameof(Configuration.HideCoords)
                      or nameof(Configuration.HideWeather))
        {
            if (!IsAddonOpen("_NaviMap"))
                return;

            CoordsNode->ToggleVisibility(!Config.HideCoords);
            WeatherNode->ToggleVisibility(!Config.HideWeather);
        }
    }

    public override void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        if (!IsAddonOpen("_NaviMap"))
            return;

        UpdateVisibility(RaptureAtkModule.Instance()->AtkModule.IntersectingAddon == NaviMap);
        UpdateCollision(Config.Square);
    }

    private static void UpdateVisibility(bool hovered)
    {
        if (Config.HideCoords) CoordsNode->ToggleVisibility(hovered);
        if (Config.HideWeather) WeatherNode->ToggleVisibility(hovered);

        var alpha = (byte)Math.Clamp((hovered ? Config.HoverOpacity : Config.DefaultOpacity) * 255f, 0, 255);
        if (MapNode->Color.A != alpha)
            MapNode->Color.A = alpha;
    }

    private static void UpdateCollision(bool square)
    {
        var collisionNode = CollisionNode;
        if (collisionNode == null)
            return;

        var hasCircularCollisionFlag = (collisionNode->Flags_2 & (1 << 23)) != 0;

        if (square && hasCircularCollisionFlag)
            collisionNode->Flags_2 &= ~(uint)(1 << 23); // remove circular collision flag
        else if (!square && !hasCircularCollisionFlag)
            collisionNode->Flags_2 |= 1 << 23; // add circular collision flag
    }
}
