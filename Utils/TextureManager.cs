using System.Collections.Generic;
using Dalamud.Logging;

namespace HaselTweaks.Utils;

public class TextureManager : IDisposable
{
    private readonly Dictionary<(string, int), Texture> _cache = new();

    public void Dispose()
    {
        foreach (var tex in _cache.Values)
        {
#if DEBUG
            PluginLog.Verbose($"[TextureManager] Disposing Texture: {tex.Path}");
#endif
            tex?.Dispose();
        }

        _cache.Clear();
    }

    public Texture GetTexture(string path, int version = 2)
    {
        if (version != 1 && !path.Contains("_hr1"))
        {
            var pathHr1 = path.Insert(path.LastIndexOf('.'), "_hr1");
            if (Service.Data.FileExists(pathHr1))
            {
                path = pathHr1;
            }
            else
            {
                version = 1;
            }
        }

        if (!_cache.TryGetValue((path, version), out var tex))
        {
#if DEBUG
            PluginLog.Verbose($"[TextureManager] Creating Texture: {path}");
#endif
            _cache.Add((path, version), tex = new(path, version));
        }

        return tex;
    }

    public Texture GetIcon(int iconId, int version = 2)
        => GetTexture($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}.tex", version);
}
