using Dalamud.Interface.Utility.Raii;
using HaselTweaks.Enums;

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

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = _configGui.PushContext(this);

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
