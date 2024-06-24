using System.Text.Json.Nodes;

namespace HaselTweaks.Interfaces;

public interface IConfigMigration
{
    /// <summary>
    /// The version to migrate to.
    /// This is the version the configuration will have after migrating.
    /// </summary>
    int Version { get; }

    void Migrate(ref JsonObject config);
}
