using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Tweaks;

public class EnhancedRecipeNoteConfiguration
{
    // Settings
    public bool RememberSearchHistory = true;

    // User Data
    public List<ReadOnlySeString> SearchHistory = [];
}

public unsafe partial class EnhancedRecipeNote
{
    public override void DrawConfig()
    {
        var agent = AgentRecipeNote.Instance();
        var agentActive = agent->IsAgentActive();

        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("RememberSearchHistory", ref _config.RememberSearchHistory);

        using (ImGuiUtils.ConfigIndent())
        using (ImRaii.Disabled(agentActive || _config.SearchHistory.Count == 0))
        {
            ImGui.Spacing();

            if (ImGui.Button(_textService.Translate("EnhancedRecipeNote.Config.ClearSearchHistory") + "##ClearSearchHistory"))
            {
                _config.SearchHistory.Clear();
                _pluginConfig.Save();

                AgentRecipeNote.ClearStdDequeUtf8String(&agent->RecipeSearchHistory);
            }
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            if (_config.SearchHistory.Count == 0)
            {
                ImGui.SetTooltip(_textService.Translate("EnhancedRecipeNote.Config.ClearSearchHistory.Tooltip.Empty"));
            }
            else if (agentActive)
            {
                ImGui.SetTooltip(_textService.Translate("EnhancedRecipeNote.Config.ClearSearchHistory.Tooltip.AgentActive"));
            }
        }
    }
}
