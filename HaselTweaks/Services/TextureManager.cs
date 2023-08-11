using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using HaselTweaks.Extensions;
using HaselTweaks.Records;

namespace HaselTweaks.Services;

public class TextureManager : IDisposable
{
    private readonly Dictionary<(string Path, int Version, Vector2? Uv0, Vector2? Uv1), Texture> _cache = new();
    private readonly Dictionary<(uint IconId, bool IsHq), Texture> _iconTexCache = new();
    private readonly Dictionary<(string UldName, uint PartListId, uint PartId), Texture> _uldTexCache = new();

    public TextureManager()
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
        var key = (path, version, uv0, uv1);

        if (!_cache.TryGetValue(key, out var tex))
        {
            lock (_cache)
                _cache.Add(key, tex = new(path, version, uv0, uv1));
        }

        return tex;
    }

    public Texture GetIcon(uint iconId, bool isHq = false)
    {
        var key = (iconId, isHq);

        if (_iconTexCache.TryGetValue(key, out var tex))
            return tex;

        var flags = ITextureProvider.IconFlags.HiRes;

        if (isHq)
            flags |= ITextureProvider.IconFlags.ItemHighQuality;

        var path = Service.TextureProvider.GetIconPath(iconId, flags) ?? Texture.EmptyIconPath;
        var version = path.EndsWith("_hr1.tex") ? 2 : 1;

        lock (_iconTexCache)
            _iconTexCache.Add(key, tex = Get(path, version));

        return tex;
    }

    public Texture GetIcon(int iconId)
        => GetIcon((uint)iconId);

    public Texture GetPart(string uldName, uint partListId, uint partIndex)
    {
        var key = (uldName, partListId, partIndex);

        if (_uldTexCache.TryGetValue(key, out var tex))
            return tex;

        using var uld = Service.PluginInterface.UiBuilder.LoadUld($"ui/uld/{uldName}.uld");

        if (uld == null || !uld.Valid)
            return Get(Texture.EmptyIconPath);

        if (!uld.Uld!.Parts.FindFirst((partList) => partList.Id == partListId, out var partList) || partList.PartCount < partIndex)
            return Get(Texture.EmptyIconPath);

        var part = partList.Parts[partIndex];

        if (!uld.Uld.AssetData.FindFirst((asset) => asset.Id == part.TextureId, out var asset))
            return Get(Texture.EmptyIconPath);

        var assetPath = new string(asset.Path, 0, asset.Path.IndexOf('\0'));

        // check if high-res texture exists
        var path = assetPath;
        path = path.Insert(path.LastIndexOf('.'), "_hr1");
        var exists = Service.DataManager.FileExists(path);
        var version = 2;

        // fallback to normal texture
        if (!exists)
        {
            path = assetPath;
            exists = Service.DataManager.FileExists(path);
            version = 1;
        }

        // fallback to transparent texture
        if (!exists)
            return Get(Texture.EmptyIconPath);

        var uv0 = new Vector2(part.U, part.V) * version;
        var uv1 = new Vector2(part.U + part.W, part.V + part.H) * version;

        lock (_uldTexCache)
            _uldTexCache.Add(key, tex = Get(path, version, uv0, uv1));

        return tex;
    }
}
