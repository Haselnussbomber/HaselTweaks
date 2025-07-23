namespace HaselTweaks.Tweaks;

public class EnhancedMonsterNoteConfiguration
{
    public bool RememberTabSelection = true;
    public bool OpenWithCurrentClass = true;
    public bool OpenWithIncompleteFilter = true;
}

public unsafe partial class EnhancedMonsterNote
{
    private EnhancedMonsterNoteConfiguration Config => _pluginConfig.Tweaks.EnhancedMonsterNote;

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("RememberTabSelection", ref Config.RememberTabSelection);
        _configGui.DrawBool("OpenWithCurrentClass", ref Config.OpenWithCurrentClass);
        _configGui.DrawBool("OpenWithIncompleteFilter", ref Config.OpenWithIncompleteFilter);
    }
}
