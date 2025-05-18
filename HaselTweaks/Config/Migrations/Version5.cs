using System.Text.Json.Nodes;

namespace HaselTweaks.Config.Migrations;

// Version 5: switched to System.Text.Json
public class Version5 : IConfigMigration
{
    public int Version => 5;

    public void Migrate(ref JsonObject config)
    {
        var tweakConfigs = (JsonObject?)config["Tweaks"];

        // fix for "The JSON value could not be converted to System.UInt64. Path: $.Tweaks.EnhancedLoginLogout.PetMirageSettings.$type"
        ((JsonObject?)tweakConfigs?["EnhancedLoginLogout"]?["PetMirageSettings"])?.Remove("$type");

        // fix for "The JSON value could not be converted to System.UInt64. Path: $.Tweaks.EnhancedLoginLogout.SelectedEmotes.$type"
        ((JsonObject?)tweakConfigs?["EnhancedLoginLogout"]?["SelectedEmotes"])?.Remove("$type");
    }
}
