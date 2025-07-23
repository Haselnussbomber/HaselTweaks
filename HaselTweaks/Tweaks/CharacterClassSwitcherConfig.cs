namespace HaselTweaks.Tweaks;

public class CharacterClassSwitcherConfiguration
{
    public bool DisableTooltips = false;
    public bool AlwaysOpenOnClassesJobsTab = false;
    public ClassesJobsSubTabs ForceClassesJobsSubTab = ClassesJobsSubTabs.None;
}

public partial class CharacterClassSwitcher
{
    private CharacterClassSwitcherConfiguration Config => _pluginConfig.Tweaks.CharacterClassSwitcher;

    public override void DrawConfig()
    {
        _configGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["CharacterWindowJobSwitcher"])]);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("DisableTooltips", ref Config.DisableTooltips);
        _configGui.DrawBool("AlwaysOpenOnClassesJobsTab", ref Config.AlwaysOpenOnClassesJobsTab, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.AlwaysOpenOnClassesJobsTab))
                _configGui.DrawEnum("ForceClassesJobsSubTab", ref Config.ForceClassesJobsSubTab);
        });
    }
}
