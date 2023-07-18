using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Lumina.Data.Files;

namespace HaselTweaks.Utils.TextureCache;

public record Texture : IDisposable
{
    public static readonly string EmptyIconPath = "ui/icon/000000/000000.tex";
    private static readonly TimeSpan KeepAliveTime = TimeSpan.FromSeconds(2);

    private TextureWrap? _textureWrap;
    private DateTime _lastAccess = DateTime.UtcNow;

    public Texture(string path, int version, Vector2? uv0 = null, Vector2? uv1 = null)
    {
        Path = path;
        Version = version;
        Uv0 = uv0;
        Uv1 = uv1;
    }

    public string Path { get; }
    public int Version { get; }
    public Vector2 Size { get; private set; }
    public Vector2? Uv0 { get; private set; }
    public Vector2? Uv1 { get; private set; }

    public bool IsExpired => _lastAccess < DateTime.UtcNow - KeepAliveTime;

    public void Dispose()
    {
#if DEBUG
        PluginLog.Verbose($"[Texture] Disposing Texture: {Path} (Version {Version})");
#endif
        _textureWrap?.Dispose();
    }

    public void Draw(Vector2? drawSize = null)
    {
        var size = drawSize ?? Size;

        _lastAccess = DateTime.UtcNow;

        if (!ImGuiUtils.IsInViewport() || Path == EmptyIconPath)
        {
            ImGui.Dummy(size);
            return;
        }

        _textureWrap ??= LoadTexture();

        if (_textureWrap == null || _textureWrap.ImGuiHandle == nint.Zero)
        {
            ImGui.Dummy(size);
            return;
        }

        ImGui.Image(_textureWrap.ImGuiHandle, size, Uv0 ?? Vector2.Zero, Uv1 ?? Vector2.One);
    }

    public void Draw(float x, float y)
        => Draw(new Vector2(x, y));

    public void Draw(float dimensions)
        => Draw(dimensions, dimensions);

    private TextureWrap? LoadTexture()
    {
#if DEBUG
        PluginLog.Verbose($"[Texture] Loading Texture: {Path} (Version {Version})");
#endif

        var tex = System.IO.Path.IsPathRooted(Path)
            ? Service.Data.GameData.GetFileFromDisk<TexFile>(Path)
            : Service.Data.GameData.GetFile<TexFile>(Path);

        if (tex == null)
            return null;

        var texSize = new Vector2(tex.Header.Width, tex.Header.Height);

        // defaults
        Uv0 ??= Vector2.Zero;
        Uv1 ??= texSize;

        // set size depending on uv dimensions
        Size = Uv1.Value - Uv0.Value;

        // convert uv coordinates range from [[0, 0], [Width, Height]] to [[0, 0], [1, 1]] for ImGui
        Uv0 = Uv0.Value / texSize;
        Uv1 = Uv1.Value / texSize;

        return Service.Data.GetImGuiTexture(tex);
    }
}
