using Dalamud.Interface.Utility.Raii;
using HaselTweaks.Config;
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
    private CharacterClassSwitcherConfiguration Config => PluginConfig.Tweaks.CharacterClassSwitcher;

    public void OnConfigOpen() { }
    public void OnConfigClose() { }
    public void OnConfigChange(string fieldName) { }

    public void DrawConfig()
    {
        using var _ = ConfigGui.PushContext(this);

        ConfigGui.DrawIncompatibilityWarnings([("SimpleTweaksPlugin", ["CharacterWindowJobSwitcher"])]);

        ConfigGui.DrawConfigurationHeader();
        ConfigGui.DrawBool("DisableTooltips", ref Config.DisableTooltips);
        ConfigGui.DrawBool("AlwaysOpenOnClassesJobsTab", ref Config.AlwaysOpenOnClassesJobsTab, drawAfterDescription: () =>
        {
            using (ImRaii.Disabled(!Config.AlwaysOpenOnClassesJobsTab))
                ConfigGui.DrawEnum("ForceClassesJobsSubTab", ref Config.ForceClassesJobsSubTab);
        });
    }
}
