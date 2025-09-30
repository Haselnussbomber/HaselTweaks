namespace HaselTweaks.Tweaks;

public class EnhancedMonsterNoteConfiguration
{
    public bool RememberTabSelection = true;
    public bool OpenWithCurrentClass = true;
    public bool OpenWithIncompleteFilter = true;
}

public unsafe partial class EnhancedMonsterNote
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("RememberTabSelection", ref _config.RememberTabSelection);
        _configGui.DrawBool("OpenWithCurrentClass", ref _config.OpenWithCurrentClass);
        _configGui.DrawBool("OpenWithIncompleteFilter", ref _config.OpenWithIncompleteFilter);
    }
}
