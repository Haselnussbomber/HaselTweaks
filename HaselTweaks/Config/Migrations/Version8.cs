using System.Text.Json.Nodes;

namespace HaselTweaks.Config.Migrations;

// Version 8: added FastMouseClickFix as replacement for ReducedMouseClickThrottle
public class Version8 : IConfigMigration
{
    public int Version => 8;

    public void Migrate(ref JsonObject config)
    {
        var enabledTweaks = (JsonArray?)config["EnabledTweaks"];
        if (enabledTweaks == null)
            return;

        var nodeToRemove = enabledTweaks.FirstOrDefault(node => node?.ToString() == "ReducedMouseClickThrottle");
        if (nodeToRemove != null)
        {
            enabledTweaks.Remove(nodeToRemove);
            enabledTweaks.Add("FastMouseClickFix");
        }
    }
}
