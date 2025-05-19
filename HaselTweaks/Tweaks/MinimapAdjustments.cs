using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

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

        if (!TryGetAddon<HaselAddonNaviMap>("_NaviMap", out var naviMap))
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

        if (!TryGetAddon<HaselAddonNaviMap>("_NaviMap", out var naviMap))
            return;

        UpdateAlpha(naviMap);

        var isHovered = RaptureAtkModule.Instance()->AtkCollisionManager.IntersectingAddon == naviMap;
        _targetAlpha = isHovered ? Config.HoverOpacity : Config.DefaultOpacity;

        UpdateVisibility(naviMap, isHovered);
        UpdateCollision(naviMap, Config.Square);
    }

    private void UpdateAlpha(HaselAddonNaviMap* naviMap)
    {
        var maskNode = naviMap->Mask;
        var targetAlphaByte = _targetAlpha * 255;

        if (maskNode->Color.A == targetAlphaByte)
            return;

        maskNode->Color.A = (byte)MathUtils.DeltaLerp(maskNode->Color.A, targetAlphaByte, 0.16f);
    }

    private void UpdateVisibility(HaselAddonNaviMap* naviMap, bool hovered)
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

    private static void UpdateCollision(HaselAddonNaviMap* naviMap, bool square)
    {
        var collisionNode = naviMap->Collision;
        var hasCircularCollisionFlag = (collisionNode->DrawFlags & (1 << 23)) != 0;

        if (square && hasCircularCollisionFlag)
            collisionNode->DrawFlags &= ~(uint)(1 << 23); // remove circular collision flag
        else if (!square && !hasCircularCollisionFlag)
            collisionNode->DrawFlags |= 1 << 23; // add circular collision flag
    }
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct HaselAddonNaviMap
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    public AtkCollisionNode* Collision => AtkUnitBase.GetNodeById(19)->GetAsAtkCollisionNode();
    public AtkImageNode* Mask => AtkUnitBase.GetNodeById(17)->GetAsAtkImageNode();
    public AtkResNode* Coords => AtkUnitBase.GetNodeById(5);
    public AtkResNode* Weather => AtkUnitBase.GetNodeById(14);
    public AtkImageNode* Sun => AtkUnitBase.GetNodeById(16)->GetAsAtkImageNode();
    public AtkResNode* CardinalDirections => AtkUnitBase.GetNodeById(8);
}
