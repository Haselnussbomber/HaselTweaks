namespace HaselTweaks.Tweaks;

public class CharacterClassSwitcherConfiguration
{
    public bool DisableTooltips = false;
    public bool AlwaysOpenOnClassesJobsTab = false;
    public ClassesJobsSubTabs ForceClassesJobsSubTab = ClassesJobsSubTabs.None;
}

public partial class CharacterClassSwitcher
{
    public override void DrawConfig()
    {
        _configGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["CharacterWindowJobSwitcher"])]);

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("DisableTooltips", ref _config.DisableTooltips);
        _configGui.DrawBool("AlwaysOpenOnClassesJobsTab", ref _config.AlwaysOpenOnClassesJobsTab, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!_config.AlwaysOpenOnClassesJobsTab))
                _configGui.DrawEnum("ForceClassesJobsSubTab", ref _config.ForceClassesJobsSubTab);
        });
    }
}
