using System.Text.Json.Nodes;

namespace HaselTweaks.Config.Migrations;

// Version 7: custom DTR FPS Format
public class Version7 : IConfigMigration
{
    public int Version => 7;

    public void Migrate(ref JsonObject config)
    {
        var tweakConfigs = (JsonObject?)config["Tweaks"];

        var dtrConfig = (JsonObject?)tweakConfigs?["DTR"];
        if (dtrConfig == null || (string?)dtrConfig["FormatUnitText"] == null)
            return; // nothing to do

        dtrConfig!["FpsFormat"] = "{0}" + (string?)dtrConfig["FormatUnitText"] ?? " fps";
        dtrConfig.Remove("FormatUnitText");
    }
}
