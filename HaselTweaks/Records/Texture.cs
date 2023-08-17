using System.IO;
using System.Numerics;
using HaselTweaks.Utils;
using ImGuiNET;
using ImGuiScene;

namespace HaselTweaks.Records;

public record Texture : IDisposable
{
    public static readonly string EmptyIconPath = "ui/icon/000000/000000.tex";
    private static readonly TimeSpan KeepAliveTime = TimeSpan.FromSeconds(2);

    private TextureWrap? _textureWrap;
    private DateTime _lastAccess = DateTime.UtcNow;
    private DateTime _lastRender = DateTime.MinValue;
    private bool _sizesSet;

    public Texture(string path, int version, Vector2? uv0 = null, Vector2? uv1 = null)
    {
        Path = path;
        Version = version;
        Uv0 = uv0;
        Uv1 = uv1;
    }

    void IDisposable.Dispose()
    {
        Unload();
    }

    public string Path { get; }
    public int Version { get; }
    public Vector2 Size { get; private set; }
    public Vector2? Uv0 { get; private set; }
    public Vector2? Uv1 { get; private set; }

    public bool IsExpired => _lastAccess < DateTime.UtcNow - KeepAliveTime;

    public void Draw(Vector2? drawSize = null)
    {
        var size = drawSize ?? Size;

        _lastAccess = DateTime.UtcNow;

        if (!ImGuiUtils.IsInViewport(size) || Path == EmptyIconPath)
        {
            ImGui.Dummy(size);

            if (_textureWrap != null && _lastRender < DateTime.UtcNow - KeepAliveTime)
                Unload();
            return;
        }

        _textureWrap ??= LoadTexture();
        _lastRender = DateTime.UtcNow;

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
        var tex = System.IO.Path.IsPathRooted(Path)
            ? Service.TextureProvider.GetTextureFromFile(new FileInfo(Path), true)
            : Service.TextureProvider.GetTextureFromGame(Path, true);

        if (tex != null && !_sizesSet)
        {
            var texSize = new Vector2(tex.Width, tex.Height);

            // defaults
            Uv0 ??= Vector2.Zero;
            Uv1 ??= texSize;

            // set size depending on uv dimensions
            Size = Uv1.Value - Uv0.Value;

            // convert uv coordinates range from [[0, 0], [Width, Height]] to [[0, 0], [1, 1]] for ImGui
            Uv0 = Uv0.Value / texSize;
            Uv1 = Uv1.Value / texSize;

            _sizesSet = true;
        }

        return tex;
    }

    public void Unload()
    {
        if (_textureWrap == null)
            return;

        _textureWrap?.Dispose();
        _textureWrap = null;
    }
}
