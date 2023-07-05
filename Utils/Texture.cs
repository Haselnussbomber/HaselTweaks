using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;

namespace HaselTweaks.Utils;

public class Texture : IDisposable
{
    private readonly TextureManager _textureManager;
    private TextureWrap? _textureWrap;

    public Texture(TextureManager manager, string path, int version)
    {
        _textureManager = manager;
        Path = path;
        Version = version;
    }

    public string Path { get; }
    public int Version { get; }

    public void Dispose()
    {
        _textureWrap?.Dispose();
        _textureWrap = null;
    }

    public void Draw(Vector2? drawSize = null)
    {
        if (!IsInViewport())
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        _textureWrap ??= LoadTexture();

        if (_textureWrap == null || _textureWrap.ImGuiHandle == 0)
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        ImGui.Image(_textureWrap.ImGuiHandle, drawSize ?? new(_textureWrap.Width, _textureWrap.Height));
    }

    public void DrawPart(Vector2 partStart, Vector2 partSize, Vector2? drawSize = null)
    {
        if (!IsInViewport())
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        _textureWrap ??= LoadTexture();

        if (_textureWrap == null || _textureWrap.ImGuiHandle == 0)
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        var texSize = new Vector2(_textureWrap.Width, _textureWrap.Height);

        partStart *= Version;
        partSize *= Version;

        var partEnd = (partStart + partSize) / texSize;
        partStart /= texSize;

        ImGui.Image(_textureWrap.ImGuiHandle, drawSize ?? partSize, partStart, partEnd);
    }

    private TextureWrap? LoadTexture()
    {
        var path = Path;

        try
        {
            if (_textureManager.PenumbraPathResolver != null)
                path = _textureManager.PenumbraPathResolver.InvokeFunc(Path);
        }
        catch { }

#if DEBUG
        PluginLog.Verbose($"[Texture] Loading Texture: {path}");
#endif

        return Service.Data.GetImGuiTexture(path);
    }

    private static bool IsInViewport()
    {
        var distanceY = ImGui.GetCursorPosY() - ImGui.GetScrollY();
        return distanceY >= 0 && distanceY <= ImGui.GetWindowHeight();
    }
}
