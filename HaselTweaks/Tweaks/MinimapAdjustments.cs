using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class MinimapAdjustments : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;

    private float _targetAlpha;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _targetAlpha = Config.DefaultOpacity;

        _framework.Update += OnFrameworkUpdate;
    }

    public void OnDisable()
    {
        _framework.Update -= OnFrameworkUpdate;

        if (Status is not TweakStatus.Enabled)
            return;

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

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_clientState.IsLoggedIn)
            return;

        if (!TryGetAddon<NaviMap>("_NaviMap", out var naviMap))
            return;

        UpdateAlpha(naviMap);

        var isHovered = RaptureAtkModule.Instance()->AtkCollisionManager.IntersectingAddon == naviMap;
        _targetAlpha = isHovered ? Config.HoverOpacity : Config.DefaultOpacity;

        UpdateVisibility(naviMap, isHovered);
        UpdateCollision(naviMap, Config.Square);
    }

    private void UpdateAlpha(NaviMap* naviMap)
    {
        var maskNode = naviMap->Mask;
        var targetAlphaByte = _targetAlpha * 255;

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
