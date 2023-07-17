using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Game;
using HaselTweaks.Extensions;

namespace HaselTweaks.Utils.TextureCache;

public class TextureCache : IDisposable
{
    private readonly Dictionary<(string Path, int Version), Texture> _cache = new();
    private readonly Dictionary<uint, Texture> _iconTexCache = new();
    private readonly Dictionary<(string UldName, uint PartListId, uint PartId), Texture> _uldTexCache = new();

    public TextureCache()
    {
        Service.Framework.Update += Framework_Update;
    }

    public void Dispose()
    {
        Service.Framework.Update -= Framework_Update;
        _iconTexCache.Clear();
        _uldTexCache.Clear();
        _cache.Dispose();
    }

    private void Framework_Update(Framework framework)
    {
        lock (_cache)
        {
            _iconTexCache.RemoveAll((key, value) => value.IsExpired);
            _uldTexCache.RemoveAll((key, value) => value.IsExpired);
            _cache.RemoveAll((key, value) => value.IsExpired, true);
        }
    }

    public Texture Get(string path, int version = 1, Vector2? uv0 = null, Vector2? uv1 = null)
    {
        try
        {
            var _path = Service.PluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveInterfacePath").InvokeFunc(path);
            if (Path.IsPathRooted(_path) ? File.Exists(_path) : Service.Data.FileExists(_path))
            {
                path = _path;

                if (path.EndsWith("_hr1.tex"))
                {
                    version = 2;
                }
            }
        }
        catch { }

        var key = (path, version);

        if (!_cache.TryGetValue(key, out var tex))
        {
            lock (_cache)
                _cache.Add(key, tex = new(path, version, uv0, uv1));
        }

        return tex;
    }

    public Texture GetIcon(uint iconId)
    {
        if (_iconTexCache.TryGetValue(iconId, out var tex))
            return tex;

        var path = $"ui/icon/{iconId / 1000:D3}000/{iconId:D6}_hr1.tex";
        var exists = Service.Data.FileExists(path);
        var version = 2;

        if (!exists)
        {
            path = $"ui/icon/{iconId / 1000:D3}000/{iconId:D6}.tex";
            exists = Service.Data.FileExists(path);
            version = 1;
        }

        if (!exists)
        {
            // fallback: transparent icon
            path = Texture.EmptyIconPath;
        }

        lock (_iconTexCache)
            _iconTexCache.Add(iconId, tex = Get(path, version));

        return tex;
    }

    public Texture GetIcon(int iconId)
        => GetIcon((uint)iconId);

    public Texture GetPart(string uldName, uint partListId, uint partId)
    {
        var key = (uldName, partListId, partId);

        if (_uldTexCache.TryGetValue(key, out var tex))
            return tex;

        var uld = Service.PluginInterface.UiBuilder.LoadUld($"ui/uld/{uldName}.uld");

        if (uld == null || !uld.Valid)
            return Get(Texture.EmptyIconPath);

        if (!uld.Uld!.Parts.FindFirst((partList) => partList.Id == partListId, out var partList) || partList.PartCount < partId)
            return Get(Texture.EmptyIconPath);

        var part = partList.Parts[partId];

        if (!uld.Uld.AssetData.FindFirst((asset) => asset.Id == part.TextureId, out var asset))
            return Get(Texture.EmptyIconPath);

        var assetPath = new string(asset.Path, 0, asset.Path.IndexOf('\0'));

        // check if high-res texture exists
        var path = assetPath;
        path = path.Insert(path.LastIndexOf('.'), "_hr1");
        var exists = Service.Data.FileExists(path);
        var version = 2;

        // fallback to normal texture
        if (!exists)
        {
            path = assetPath;
            exists = Service.Data.FileExists(path);
            version = 1;
        }

        // fallback to transparent texture
        if (!exists)
        {
            return Get(Texture.EmptyIconPath);
        }

        var uv0 = new Vector2(part.U, part.V) * version;
        var uv1 = new Vector2(part.U + part.W, part.V + part.H) * version;

        lock (_uldTexCache)
            _uldTexCache.Add(key, tex = Get(path, version, uv0, uv1));

        return tex;
    }
}
