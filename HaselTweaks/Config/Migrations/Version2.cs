using System.Text.Json.Nodes;

namespace HaselTweaks.Config.Migrations;

public class Version2 : IConfigMigration
{
    public int Version => 2;

    public void Migrate(ref JsonObject config)
    {
        var enabledTweaks = (JsonArray?)config["EnabledTweaks"];
        var tweakConfigs = (JsonObject?)config["Tweaks"];

        if (enabledTweaks == null || tweakConfigs == null)
            return;

        void RenameTweak(string oldName, string newName)
        {
            var nodeToRemove = enabledTweaks.FirstOrDefault(node => node?.ToString() == oldName);
            if (nodeToRemove != null)
            {
                enabledTweaks.Remove(nodeToRemove);
                enabledTweaks.Add(newName);
            }

            if (tweakConfigs[oldName] is JsonObject tweakConfig)
            {
                tweakConfigs.Remove(oldName);
                tweakConfigs[newName] = tweakConfig;
            }
        }

        RenameTweak("RevealDungeonRequirements", "RevealDutyRequirements"); // commit 7ce9b37b
        RenameTweak("SeriesExpBar", "EnhancedExpBar"); // commit 11b6231f
        RenameTweak("RequisiteMaterials", "MaterialAllocation"); // commit 730257d9

        if (tweakConfigs?["EnhancedExpBar"]?["ForcePvPSeasonBar"] != null)
        {
            tweakConfigs["EnhancedExpBar"]!["ForcePvPSeriesBar"] = tweakConfigs["EnhancedExpBar"]!["ForcePvPSeasonBar"];
            ((JsonObject?)tweakConfigs["EnhancedExpBar"]!).Remove("ForcePvPSeasonBar");
        }
    }
}
