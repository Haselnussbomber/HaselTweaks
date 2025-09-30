namespace HaselTweaks.Tweaks;

public class CosmicResearchTodoConfiguration
{
    public bool ShowCosmicToolScore = true;
    public bool ShowCompletedAnalysis = true;
}

public partial class CosmicResearchTodo
{
    public override void OnConfigChange(string fieldName)
    {
        RequestUpdate();
    }

    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("ShowCosmicToolScore", ref _config.ShowCosmicToolScore);
        _configGui.DrawBool("ShowCompletedAnalysis", ref _config.ShowCompletedAnalysis);
    }
}
