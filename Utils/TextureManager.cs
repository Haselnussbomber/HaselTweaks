using System.Collections.Generic;

namespace HaselTweaks.Utils;

public class TextureManager : IDisposable
{
    private readonly Dictionary<(string, int), Texture> _texCache = new();

    public void Dispose()
    {
        foreach (var tex in _texCache.Values)
            tex?.Dispose();

        _texCache.Clear();
    }

    public Texture GetTexture(string path, int version = 2)
    {
        if (!_texCache.TryGetValue((path, version), out var tex))
        {
            _texCache.Add((path, version), tex = new(path, version));
        }

        return tex;
    }

    public Texture GetIcon(uint iconId, int version = 2)
        => GetTexture($"ui/icon/{iconId / 1000:D3}000/{iconId:D6}.tex", version);

    public Texture GetIcon(int iconId, int version = 2)
        => GetIcon((uint)iconId, version);
}
