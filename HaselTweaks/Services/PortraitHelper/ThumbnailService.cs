using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Textures.TextureWraps;
using Size = (int Width, int Height);

namespace HaselTweaks.Services.PortraitHelper;

public record ImageResult : IDisposable
{
    public Guid Id { get; init; }
    public BgraImage? Image { get; set; }
    public Exception? Exception { get; set; }

    void IDisposable.Dispose() => Image?.Dispose();
}

public record ThumbnailResult : IDisposable
{
    public Guid Id { get; init; }
    public Size Size { get; set; }
    public IDalamudTextureWrap? Texture { get; set; }
    public Exception? Exception { get; set; }

    void IDisposable.Dispose() => Texture?.Dispose();
}

[RegisterSingleton, AutoConstruct]
public partial class ThumbnailService : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ITextureProvider _textureProvider;

    private readonly ConcurrentDictionary<Guid, ConfiguredTaskAwaitable> _imageTasks = [];
    private readonly ConcurrentDictionary<Guid, ImageResult> _images = [];

    private readonly ConcurrentDictionary<Guid, ConfiguredTaskAwaitable> _thumbnailTasks = [];
    private readonly ConcurrentDictionary<(Guid, Size), ThumbnailResult> _thumbnails = [];

    private CancellationTokenSource? _disposeCTS = new();
    private bool _disposing;

    void IDisposable.Dispose()
    {
        _disposing = true;
        Clear();
    }

    public void Clear()
    {
        _disposeCTS?.Cancel();
        _disposeCTS?.Dispose();
        _disposeCTS = null;
        _images.Dispose();
        _thumbnails.Dispose();
    }

    public string GetPortraitThumbnailPath(Guid id)
    {
        var portraitsPath = Path.Join(_pluginInterface.ConfigDirectory.FullName, "Portraits");

        if (!Directory.Exists(portraitsPath))
            Directory.CreateDirectory(portraitsPath);

        return Path.Join(portraitsPath, $"{id.ToString("D").ToLowerInvariant()}.png");
    }

    public bool TryGetThumbnail(Guid id, int width, int height, out bool exists, out IDalamudTextureWrap? textureWrap, out Exception? exception)
    {
        exists = false;
        textureWrap = null;
        exception = null;

        if (_disposing)
            return false;

        if (_thumbnails.TryGetValue((id, (width, height)), out var thumbnailResult))
        {
            exists = true;
            textureWrap = thumbnailResult.Texture;
            exception = thumbnailResult.Exception;
            return true;
        }

        var path = GetPortraitThumbnailPath(id);
        if (!File.Exists(path))
            return false;

        exists = true;

        // -- Original Image

        var imageResult = _images.GetOrAdd(id, static id => new ImageResult { Id = id });

        if (imageResult.Exception != null)
        {
            exception = imageResult.Exception;
            return false;
        }

        _disposeCTS ??= new();

        if (imageResult.Image == null)
        {
            _ = _imageTasks.GetOrAdd(id, _ => Task.Run(async () =>
            {
                try
                {
                    imageResult.Image = BgraImage.FromFile(path);
                }
                catch (Exception ex)
                {
                    imageResult.Exception = ex;
                }
                finally
                {
                    _imageTasks.TryRemove(id, out var _);
                }
            }, _disposeCTS.Token).ConfigureAwait(false));
        }

        // -- Thumbnail

        if (imageResult.Image == null)
            return false;

        thumbnailResult = _thumbnails.GetOrAdd((id, (width, height)), static (key) => new ThumbnailResult { Id = key.Item1, Size = key.Item2 });

        _ = _thumbnailTasks.GetOrAdd(id, _ => Task.Run(() =>
        {
            try
            {
                using var scaledImage = imageResult.Image.Clone();

                if (_disposing)
                    return;

                scaledImage.Resize((uint)width, (uint)height);

                if (_disposing)
                    return;

                thumbnailResult.Texture = scaledImage.AsDalamudTextureWrap(_textureProvider);
            }
            catch (Exception ex)
            {
                thumbnailResult.Exception = ex;
            }
            finally
            {
                _thumbnailTasks.TryRemove(id, out var _);
            }
        }, _disposeCTS.Token).ConfigureAwait(false));

        return false;
    }
}
