using Dalamud.Interface.Utility.Raii;
using HaselTweaks.Config;

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
    private MinimapAdjustmentsConfiguration Config => PluginConfig.Tweaks.MinimapAdjustments;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
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

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("Square", ref Config.Square);
        ConfigGui.DrawFloat("DefaultOpacity", ref Config.DefaultOpacity, defaultValue: 0.8f, max: 1);
        ConfigGui.DrawFloat("HoverOpacity", ref Config.HoverOpacity, defaultValue: 1, max: 1);
        ConfigGui.DrawBool("HideCoords", ref Config.HideCoords, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideCoords))
                ConfigGui.DrawBool("CoordsVisibleOnHover", ref Config.CoordsVisibleOnHover);
        });
        ConfigGui.DrawBool("HideWeather", ref Config.HideWeather, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideWeather))
                ConfigGui.DrawBool("WeatherVisibleOnHover", ref Config.WeatherVisibleOnHover);
        });
        ConfigGui.DrawBool("HideSun", ref Config.HideSun, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideSun))
                ConfigGui.DrawBool("SunVisibleOnHover", ref Config.SunVisibleOnHover);
        });
        ConfigGui.DrawBool("HideCardinalDirections", ref Config.HideCardinalDirections, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.HideCardinalDirections))
                ConfigGui.DrawBool("CardinalDirectionsVisibleOnHover", ref Config.CardinalDirectionsVisibleOnHover);
        });
    }
}
