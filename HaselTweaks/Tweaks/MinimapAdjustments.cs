using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

public class MinimapAdjustmentsConfiguration
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

[Tweak]
public unsafe class MinimapAdjustments : Tweak<MinimapAdjustmentsConfiguration>
{
    public override void Disable()
    {
        if (!TryGetAddon<AtkUnitBase>("_NaviMap", out var naviMap))
            return;

        // reset visibility
        UpdateVisibility(naviMap, true);

        // add back circular collision flag
        UpdateCollision(naviMap, false);
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName is nameof(Config.HideCoords)
                      or nameof(Config.HideWeather))
        {
            if (!TryGetAddon<AtkUnitBase>("_NaviMap", out var naviMap))
                return;

            GetNode<AtkResNode>(naviMap, 5)->ToggleVisibility(!Config.HideCoords);
            GetNode<AtkResNode>(naviMap, 14)->ToggleVisibility(!Config.HideWeather);
        }
    }

    public override void OnFrameworkUpdate()
    {
        if (!TryGetAddon<AtkUnitBase>("_NaviMap", out var naviMap))
            return;

        UpdateVisibility(naviMap, RaptureAtkModule.Instance()->AtkModule.IntersectingAddon == naviMap);
        UpdateCollision(naviMap, Config.Square);
    }

    private static void UpdateVisibility(AtkUnitBase* naviMap, bool hovered)
    {
        if (Config.HideCoords) GetNode<AtkResNode>(naviMap, 5)->ToggleVisibility(hovered);
        if (Config.HideWeather) GetNode<AtkResNode>(naviMap, 14)->ToggleVisibility(hovered);

        var alpha = (byte)Math.Clamp((hovered ? Config.HoverOpacity : Config.DefaultOpacity) * 255f, 0, 255);
        var mapNode = GetNode<AtkResNode>(naviMap, 17);
        if (mapNode->Color.A != alpha)
            mapNode->Color.A = alpha;
    }

    private static void UpdateCollision(AtkUnitBase* naviMap, bool square)
    {
        var collisionNode = GetNode<AtkResNode>(naviMap, 19);
        if (collisionNode == null)
            return;

        var hasCircularCollisionFlag = (collisionNode->DrawFlags & (1 << 23)) != 0;

        if (square && hasCircularCollisionFlag)
            collisionNode->DrawFlags &= ~(uint)(1 << 23); // remove circular collision flag
        else if (!square && !hasCircularCollisionFlag)
            collisionNode->DrawFlags |= 1 << 23; // add circular collision flag
    }
}
