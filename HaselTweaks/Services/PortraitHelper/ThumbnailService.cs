using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HaselTweaks.Services.PortraitHelper;

public record ImageResult : IDisposable
{
    public Guid Id { get; init; }
    public Image<Rgba32>? Image { get; set; }
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

    public bool TryGetThumbnail(Guid id, Size size, out bool exists, out IDalamudTextureWrap? textureWrap, out Exception? exception)
    {
        exists = false;
        textureWrap = null;
        exception = null;

        if (_disposing)
            return false;

        if (_thumbnails.TryGetValue((id, size), out var thumbnailResult))
        {
            exists = true;
            textureWrap = thumbnailResult.Texture;
            exception = thumbnailResult.Exception;
            return textureWrap != null;
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
            _imageTasks.GetOrAdd(id, _ => Task.Run(async () =>
            {
                try
                {
                    imageResult.Image = await Image.LoadAsync<Rgba32>(path, _disposeCTS.Token);
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

        thumbnailResult = _thumbnails.GetOrAdd((id, size), static (key) => new ThumbnailResult { Id = key.Item1, Size = key.Item2 });

        _thumbnailTasks.GetOrAdd(id, _ => Task.Run(() =>
        {
            try
            {
                using var scaledImage = imageResult.Image.Clone();

                if (_disposing)
                    return;

                scaledImage.Mutate(i => i.Resize(size, KnownResamplers.Lanczos3, false));

                if (_disposing)
                    return;

                var data = new byte[4 * scaledImage.Width * scaledImage.Height];
                scaledImage.CopyPixelDataTo(data);

                if (_disposing)
                    return;

                thumbnailResult.Texture = _textureProvider.CreateFromRaw(RawImageSpecification.Rgba32(scaledImage.Width, scaledImage.Height), data);
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
