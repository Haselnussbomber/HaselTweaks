using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

public class EnhancedExpBarConfiguration
{
    public bool ForcePvPSeriesBar = true;
    public bool ForceSanctuaryBar = true;
    public bool ForceCompanionBar = true;
    public bool SanctuaryBarHideJob = false;
    public MaxLevelOverrideType MaxLevelOverride = MaxLevelOverrideType.Default;
    public bool DisableColorChanges = false;
}

public enum MaxLevelOverrideType
{
    Default,
    PvPSeriesBar,
    CompanionBar,
    // No SanctuaryBar, because data is only available on the island
}

public unsafe partial class EnhancedExpBar
{
    private EnhancedExpBarConfiguration Config => PluginConfig.Tweaks.EnhancedExpBar;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }

    public void OnConfigChange(string fieldName)
    {
        if (TryGetAddon<AddonExp>("_Exp", out var addon))
        {
            addon->ClassJob--;
            addon->RequiredExp--;
            addon->AtkUnitBase.OnRequestedUpdate(
                AtkStage.Instance()->GetNumberArrayData(),
                AtkStage.Instance()->GetStringArrayData());
        }

        RunUpdate();
    }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["ShowExperiencePercentage"])]);

        ConfigGui.DrawConfigurationHeader();

        ConfigGui.DrawBool("ForcePvPSeriesBar", ref Config.ForcePvPSeriesBar);
        ConfigGui.DrawBool("ForceSanctuaryBar", ref Config.ForceSanctuaryBar);
        ConfigGui.DrawBool("ForceCompanionBar", ref Config.ForceCompanionBar);
        ConfigGui.DrawBool("SanctuaryBarHideJob", ref Config.SanctuaryBarHideJob);
        ConfigGui.DrawEnum("MaxLevelOverride", ref Config.MaxLevelOverride);
        ConfigGui.DrawBool("DisableColorChanges", ref Config.DisableColorChanges);
    }
}
