using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Lumina.Data.Files;

namespace HaselTweaks.Utils;

public class Texture : IDisposable
{
    private TextureWrap? _textureWrap;

    public Texture(string path, int version)
    {
        EffectivePath = RequestedPath = path;
        EffectiveVersion = RequestedVersion = version;
    }

    public string RequestedPath { get; }
    public string EffectivePath { get; private set; }

    public int RequestedVersion { get; }
    public int EffectiveVersion { get; private set; }

    public void Dispose()
    {
#if DEBUG
        PluginLog.Verbose($"[Texture] Disposing Texture: {RequestedPath} (version {RequestedVersion})");
#endif
        _textureWrap?.Dispose();
        _textureWrap = null;
    }

    public void Draw(Vector2? drawSize = null)
    {
        if (!ImGuiUtils.IsInViewport())
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        _textureWrap ??= GetTexture();

        if (_textureWrap == null || _textureWrap.ImGuiHandle == 0)
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        ImGui.Image(_textureWrap.ImGuiHandle, drawSize ?? new(_textureWrap.Width, _textureWrap.Height));
    }

    public void DrawPart(Vector2 partStart, Vector2 partSize, Vector2? drawSize = null)
    {
        if (!ImGuiUtils.IsInViewport())
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        _textureWrap ??= GetTexture();

        if (_textureWrap == null || _textureWrap.ImGuiHandle == 0)
        {
            ImGui.Dummy(drawSize ?? default);
            return;
        }

        var texSize = new Vector2(_textureWrap.Width, _textureWrap.Height);

        partStart *= EffectiveVersion;
        partSize *= EffectiveVersion;

        var partEnd = (partStart + partSize) / texSize;
        partStart /= texSize;

        ImGui.Image(_textureWrap.ImGuiHandle, drawSize ?? partSize, partStart, partEnd);
    }

    private TextureWrap? GetTexture()
    {
        // if high-res version was requested, but is not part of path
        if (RequestedVersion != 1 && !RequestedPath.Contains("_hr1"))
        {
            var pathHr1 = RequestedPath.Insert(RequestedPath.LastIndexOf('.'), "_hr1");

            // check if high-res version exists
            if (Service.Data.FileExists(pathHr1))
                EffectivePath = pathHr1; // yes: upgrade path
            else
                EffectiveVersion = 1; // no: downgrade version
        }

        // check Penumbra redirect
        try
        {
            EffectivePath = Service.PluginInterface.GetIpcSubscriber<string, string>("Penumbra.ResolveInterfacePath").InvokeFunc(EffectivePath).Replace('\\', '/');
            if (!EffectivePath.Contains("_hr1"))
                EffectiveVersion = 1;
        }
        catch { }

#if DEBUG
        if (RequestedPath == EffectivePath && RequestedVersion == EffectiveVersion)
            PluginLog.Verbose($"[Texture] Loading Texture: {RequestedPath} (version {RequestedVersion}) ");
        else
            PluginLog.Verbose($"[Texture] Loading Texture: {RequestedPath} (version {RequestedVersion}) => {EffectivePath} (version {EffectiveVersion})");
#endif

        if (EffectivePath[0] is '/' or '\\' || EffectivePath[1] == ':')
        {
            var texFile = Service.Data.GameData.GetFileFromDisk<TexFile>(EffectivePath);
            return Service.Data.GetImGuiTexture(texFile);
        }
        else
        {
            return Service.Data.GetImGuiTexture(EffectivePath);
        }
    }
}
