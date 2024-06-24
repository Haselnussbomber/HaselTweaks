using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;

namespace HaselTweaks.Tweaks;

public sealed class MinimapAdjustmentsConfiguration
{
    [BoolConfig]
    public bool Square = false;

    [FloatConfig(Max = 1, DefaultValue = 0.8f)]
    public float DefaultOpacity = 0.8f;

    [FloatConfig(Max = 1, DefaultValue = 1)]
    public float HoverOpacity = 1f;

    [BoolConfig]
    public bool HideCoords = true;

    [BoolConfig(DependsOn = nameof(HideCoords))]
    public bool CoordsVisibleOnHover = true;

    [BoolConfig]
    public bool HideWeather = true;

    [BoolConfig(DependsOn = nameof(HideWeather))]
    public bool WeatherVisibleOnHover = true;

    [BoolConfig]
    public bool HideSun = false;

    [BoolConfig(DependsOn = nameof(HideSun))]
    public bool SunVisibleOnHover = true;

    [BoolConfig]
    public bool HideCardinalDirections = false;

    [BoolConfig(DependsOn = nameof(HideCardinalDirections))]
    public bool CardinalDirectionsVisibleOnHover = true;
}

public unsafe struct NaviMap
{
    public AtkResNode* GetNode(uint nodeId)
    {
        fixed (NaviMap* ptr = &this)
            return GetNode<AtkResNode>((AtkUnitBase*)ptr, nodeId);
    }

    public AtkResNode* Collision => GetNode(19);
    public AtkResNode* Mask => GetNode(17);
    public AtkResNode* Coords => GetNode(5);
    public AtkResNode* Weather => GetNode(14);
    public AtkResNode* Sun => GetNode(16);
    public AtkResNode* CardinalDirections => GetNode(8);
}

public sealed unsafe class MinimapAdjustments(
    PluginConfig pluginConfig,
    TextService textService,
    IFramework Framework,
    IClientState ClientState)
    : Tweak<MinimapAdjustmentsConfiguration>(pluginConfig, textService)
{
    private bool? HoverState;
    private float TargetAlpha;

    public override void OnEnable()
    {
        HoverState = null;
        TargetAlpha = Config.DefaultOpacity;

        Framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;

        if (!TryGetAddon<NaviMap>("_NaviMap", out var naviMap))
            return;

        // reset alpha
        naviMap->Mask->Color.A = 255;

        // reset visibility
        naviMap->Coords->ToggleVisibility(true);
        naviMap->Weather->ToggleVisibility(true);
        naviMap->Sun->ToggleVisibility(true);
        naviMap->CardinalDirections->ToggleVisibility(true);

        // add back circular collision flag
        naviMap->Collision->DrawFlags |= 1 << 23;
    }

    public override void OnConfigChange(string fieldName)
    {
        if (fieldName is nameof(Config.HideCoords)
                      or nameof(Config.HideWeather)
                      or nameof(Config.HideSun)
                      or nameof(Config.HideCardinalDirections))
        {
            if (!TryGetAddon<NaviMap>("_NaviMap", out var naviMap))
                return;

            naviMap->Coords->ToggleVisibility(!Config.HideCoords);
            naviMap->Weather->ToggleVisibility(!Config.HideWeather);
            naviMap->Sun->ToggleVisibility(!Config.HideSun);
            naviMap->CardinalDirections->ToggleVisibility(!Config.HideCardinalDirections);
        }

        if (fieldName is nameof(Config.DefaultOpacity))
        {
            TargetAlpha = Config.DefaultOpacity;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn)
            return;

        if (!TryGetAddon<NaviMap>("_NaviMap", out var naviMap))
            return;

        UpdateAlpha(naviMap);

        var isHovered = RaptureAtkModule.Instance()->AtkCollisionManager.IntersectingAddon == naviMap;
        if (HoverState == isHovered)
            return;

        HoverState = isHovered;
        TargetAlpha = isHovered ? Config.HoverOpacity : Config.DefaultOpacity;

        UpdateVisibility(naviMap, isHovered);
        UpdateCollision(naviMap, Config.Square);
    }

    private void UpdateAlpha(NaviMap* naviMap)
    {
        var maskNode = naviMap->Mask;
        var targetAlphaByte = TargetAlpha * 255;

        if (maskNode->Color.A == targetAlphaByte)
            return;

        maskNode->Color.A = (byte)MathUtils.DeltaLerp(maskNode->Color.A, targetAlphaByte, 0.16f);
    }

    private void UpdateVisibility(NaviMap* naviMap, bool hovered)
    {
        bool ShouldSetVisibility(bool hide, bool visibleOnHover)
            => hide && (visibleOnHover || (!visibleOnHover && hovered == false));

        if (ShouldSetVisibility(Config.HideCoords, Config.CoordsVisibleOnHover))
            naviMap->Coords->ToggleVisibility(hovered);

        if (ShouldSetVisibility(Config.HideWeather, Config.WeatherVisibleOnHover))
            naviMap->Weather->ToggleVisibility(hovered);

        if (ShouldSetVisibility(Config.HideSun, Config.SunVisibleOnHover))
            naviMap->Sun->ToggleVisibility(hovered);

        if (ShouldSetVisibility(Config.HideCardinalDirections, Config.CardinalDirectionsVisibleOnHover))
            naviMap->CardinalDirections->ToggleVisibility(hovered);
    }

    private static void UpdateCollision(NaviMap* naviMap, bool square)
    {
        var collisionNode = naviMap->Collision;
        var hasCircularCollisionFlag = (collisionNode->DrawFlags & (1 << 23)) != 0;

        if (square && hasCircularCollisionFlag)
            collisionNode->DrawFlags &= ~(uint)(1 << 23); // remove circular collision flag
        else if (!square && !hasCircularCollisionFlag)
            collisionNode->DrawFlags |= 1 << 23; // add circular collision flag
    }
}
