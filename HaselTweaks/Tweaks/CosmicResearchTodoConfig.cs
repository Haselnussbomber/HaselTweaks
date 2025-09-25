namespace HaselTweaks.Tweaks;

public class CosmicResearchTodoConfiguration
{
    public bool ShowCosmicToolScore = true;
    public bool ShowCompletedAnalysis = true;
}

public partial class CosmicResearchTodo
{
    private CosmicResearchTodoConfiguration Config => _pluginConfig.Tweaks.CosmicResearchTodo;

    public override void OnConfigChange(string fieldName)
    {
        RequestUpdate();
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("ShowCosmicToolScore", ref Config.ShowCosmicToolScore);
        _configGui.DrawBool("ShowCompletedAnalysis", ref Config.ShowCompletedAnalysis);
    }
}
