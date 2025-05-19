namespace HaselTweaks.Tweaks;

public class MinimapAdjustmentsConfiguration
{
    public bool Square = false;
    public float DefaultOpacity = 0.8f;
    public float HoverOpacity = 1f;
    public bool HideCoords = true;
    public bool CoordsVisibleOnHover = true;
    public bool HideWeather = true;
    public bool WeatherVisibleOnHover = true;
    public bool HideSun = false;
    public bool SunVisibleOnHover = true;
    public bool HideCardinalDirections = false;
    public bool CardinalDirectionsVisibleOnHover = true;
}

public unsafe partial class MinimapAdjustments
{
    private MinimapAdjustmentsConfiguration Config => _pluginConfig.Tweaks.MinimapAdjustments;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (Status == TweakStatus.Enabled
            && fieldName is nameof(Config.HideCoords)
                or nameof(Config.HideWeather)
                or nameof(Config.HideSun)
                or nameof(Config.HideCardinalDirections)
            && TryGetAddon<HaselAddonNaviMap>("_NaviMap", out var naviMap))
        {
            naviMap->Coords->ToggleVisibility(!Config.HideCoords);
            naviMap->Weather->ToggleVisibility(!Config.HideWeather);
            naviMap->Sun->ToggleVisibility(!Config.HideSun);
            naviMap->CardinalDirections->ToggleVisibility(!Config.HideCardinalDirections);
        }

        if (fieldName is nameof(Config.DefaultOpacity))
        {
            _targetAlpha = Config.DefaultOpacity;
        }
    }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("Square", ref Config.Square);
        _configGui.DrawFloat("DefaultOpacity", ref Config.DefaultOpacity, defaultValue: 0.8f, max: 1);
        _configGui.DrawFloat("HoverOpacity", ref Config.HoverOpacity, defaultValue: 1, max: 1);
        _configGui.DrawBool("HideCoords", ref Config.HideCoords, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideCoords))
                _configGui.DrawBool("CoordsVisibleOnHover", ref Config.CoordsVisibleOnHover);
        });
        _configGui.DrawBool("HideWeather", ref Config.HideWeather, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideWeather))
                _configGui.DrawBool("WeatherVisibleOnHover", ref Config.WeatherVisibleOnHover);
        });
        _configGui.DrawBool("HideSun", ref Config.HideSun, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideSun))
                _configGui.DrawBool("SunVisibleOnHover", ref Config.SunVisibleOnHover);
        });
        _configGui.DrawBool("HideCardinalDirections", ref Config.HideCardinalDirections, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideCardinalDirections))
                _configGui.DrawBool("CardinalDirectionsVisibleOnHover", ref Config.CardinalDirectionsVisibleOnHover);
        });
    }
}
