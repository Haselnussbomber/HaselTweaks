using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselTweaks.Extensions;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Config.Migrations;

// Version 6: removed TextureHash in favor of Id
public class Version6(IDalamudPluginInterface PluginInterface, IPluginLog PluginLog) : IConfigMigration
{
    public int Version => 6;

    public void Migrate(ref JsonObject config)
    {
        var tweakConfigs = (JsonObject?)config["Tweaks"];

        var presets = (JsonArray?)tweakConfigs?["PortraitHelper"]?["Presets"];
        if (presets == null || presets.Count <= 0)
            return; // nothing to do

        PluginLog.Info("[MigrationV6] Portrait thumbnails now use the preset guid as the name. Renaming files...");

        var newPresets = new JsonArray();
        var portraitsPath = Path.Join(PluginInterface.ConfigDirectory.FullName, "Portraits");

        if (!Directory.Exists(portraitsPath))
            Directory.CreateDirectory(portraitsPath);

        for (var i = 0; i < presets.Count; i++)
        {
            var preset = (JsonObject?)presets[i];
            if (preset == null)
                continue;

            var presetCopy = preset.Deserialize<JsonObject>(); // net8: switch to .Clone()
            if (presetCopy == null)
                continue;

            var id = (string?)preset["Id"];
            var textureHash = (string?)preset["TextureHash"];

            if (id == null || textureHash == null)
                continue;

            var guid = Guid.Parse(id);

            var oldPath = Path.Join(portraitsPath, $"{textureHash}.png");

            if (File.Exists(oldPath))
            {
                var newPath = PluginInterface.GetPortraitThumbnailPath(guid);

                PluginLog.Info($"[MigrationV6]   {oldPath} => {newPath}");

                File.Move(oldPath, newPath);

                presetCopy.Remove("TextureHash");

                newPresets.Add(presetCopy);
            }
            else
            {
                var presetCode = (string?)preset["Preset"];
                throw new Exception($"[MigrationV6] Could not find thumbnail {oldPath} for {presetCode ?? string.Empty}. Please re-import.");
            }
        }

        tweakConfigs!["PortraitHelper"]!["Presets"] = newPresets;

        PluginLog.Info("[MigrationV6] Done!");
    }
}
