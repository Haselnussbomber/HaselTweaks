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
    public override void OnConfigChange(string fieldName)
    {
        if (Status == TweakStatus.Enabled
            && fieldName is nameof(_config.HideCoords)
                or nameof(_config.HideWeather)
                or nameof(_config.HideSun)
                or nameof(_config.HideCardinalDirections)
            && TryGetAddon<HaselAddonNaviMap>("_NaviMap", out var naviMap))
        {
            naviMap->Coords->ToggleVisibility(!_config.HideCoords);
            naviMap->Weather->ToggleVisibility(!_config.HideWeather);
            naviMap->Sun->ToggleVisibility(!_config.HideSun);
            naviMap->CardinalDirections->ToggleVisibility(!_config.HideCardinalDirections);
        }

        if (fieldName is nameof(_config.DefaultOpacity))
        {
            _targetAlpha = _config.DefaultOpacity;
        }
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("Square", ref _config.Square);
        _configGui.DrawFloat("DefaultOpacity", ref _config.DefaultOpacity, defaultValue: 0.8f, max: 1);
        _configGui.DrawFloat("HoverOpacity", ref _config.HoverOpacity, defaultValue: 1, max: 1);
        _configGui.DrawBool("HideCoords", ref _config.HideCoords, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.HideCoords))
                _configGui.DrawBool("CoordsVisibleOnHover", ref _config.CoordsVisibleOnHover);
        });
        _configGui.DrawBool("HideWeather", ref _config.HideWeather, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.HideWeather))
                _configGui.DrawBool("WeatherVisibleOnHover", ref _config.WeatherVisibleOnHover);
        });
        _configGui.DrawBool("HideSun", ref _config.HideSun, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.HideSun))
                _configGui.DrawBool("SunVisibleOnHover", ref _config.SunVisibleOnHover);
        });
        _configGui.DrawBool("HideCardinalDirections", ref _config.HideCardinalDirections, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.HideCardinalDirections))
                _configGui.DrawBool("CardinalDirectionsVisibleOnHover", ref _config.CardinalDirectionsVisibleOnHover);
        });
    }
}
