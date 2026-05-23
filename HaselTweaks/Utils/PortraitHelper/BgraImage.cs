using System.IO;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Lumina.Data.Files;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Imaging;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

namespace HaselTweaks.Utils.PortraitHelper;

internal unsafe class BgraImage : IDisposable
{
    private ComPtr<IWICBitmap> _bitmap;

    public (uint Width, uint Height) Size
    {
        get
        {
            if (_bitmap.IsNull)
                return (0, 0);

            _bitmap.Get()->GetSize(out var width, out var height);

            return (width, height);
        }
    }

    public uint Width => Size.Width;
    public uint Height => Size.Height;

    public BgraImage(ComPtr<IWICBitmap> bitmap)
    {
        _bitmap = bitmap;
    }

    public void Dispose()
    {
        _bitmap.Dispose();
    }

    public static BgraImage FromTexture2D(ID3D11Texture2D* texture)
    {
        var device = (ID3D11Device*)ServiceLocator.GetService<IUiBuilder>()!.DeviceHandle;

        texture->GetDesc(out var desc);

        desc.BindFlags = 0;
        desc.CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
        desc.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
        desc.MiscFlags = 0;
        desc.MipLevels = 1;

        using ComPtr<ID3D11Texture2D> stagingTexture = null;
        device->CreateTexture2D(desc, null, stagingTexture.GetAddressOf());

        if (desc.Format != DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM)
            throw new Exception($"Unsupported image format. Expected DXGI_FORMAT_B8G8R8A8_UNORM, got {desc.Format}.");

        using ComPtr<ID3D11DeviceContext> context = null;
        device->GetImmediateContext(context.GetAddressOf());

        context.Get()->CopyResource(stagingTexture.Cast<ID3D11Resource>(), (ID3D11Resource*)texture);
        context.Get()->Map(stagingTexture.Cast<ID3D11Resource>(), 0, D3D11_MAP.D3D11_MAP_READ, 0, out var mapped);

        try
        {
            using var wicFactory = CreateWicFactory();

            ComPtr<IWICBitmap> bitmap = null;
            
            wicFactory.Get()->CreateBitmapFromMemory(
                desc.Width,
                desc.Height,
                PInvoke.GUID_WICPixelFormat32bppBGRA,
                mapped.RowPitch,
                new ReadOnlySpan<byte>(mapped.pData, (int)(mapped.RowPitch * desc.Height)),
                bitmap.GetAddressOf());

            return new BgraImage(bitmap);
        }
        finally
        {
            context.Get()->Unmap(stagingTexture.Cast<ID3D11Resource>(), 0);
        }
    }

    public static BgraImage FromFile(string path)
    {
        using var wicFactory = CreateWicFactory();

        using ComPtr<IWICStream> wicStream = null;
        wicFactory.Get()->CreateStream(wicStream.GetAddressOf());

        wicStream.Get()->InitializeFromFilename(path, (uint)GENERIC_ACCESS_RIGHTS.GENERIC_READ);

        using ComPtr<IWICBitmapDecoder> decoder = wicFactory.Get()->CreateDecoderFromStream(wicStream.Cast<IStream>(), null, WICDecodeOptions.WICDecodeMetadataCacheOnDemand);

        using ComPtr<IWICBitmapFrameDecode> frame = null;
        decoder.Get()->GetFrame(0, frame.GetAddressOf());

        using ComPtr<IWICFormatConverter> converter = null;
        wicFactory.Get()->CreateFormatConverter(converter.GetAddressOf());

        converter.Get()->Initialize(
            frame.Cast<IWICBitmapSource>(),
            PInvoke.GUID_WICPixelFormat32bppBGRA,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom);

        converter.Get()->GetSize(out var width, out var height);

        ComPtr<IWICBitmap> bitmap = null;
        wicFactory.Get()->CreateBitmapFromSource(
            converter.Cast<IWICBitmapSource>(),
            WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
            bitmap.GetAddressOf());

        return new BgraImage(bitmap);
    }

    public static BgraImage FromTexFile(TexFile texFile)
    {
        using var wicFactory = CreateWicFactory();

        ComPtr<IWICBitmap> bitmap = null;

        wicFactory.Get()->CreateBitmapFromMemory(
            texFile.Header.Width,
            texFile.Header.Height,
            PInvoke.GUID_WICPixelFormat32bppBGRA,
            texFile.Header.Width * 4u,
            new ReadOnlySpan<byte>(texFile.ImageData.GetPointer(0), texFile.Header.Width * 4 * texFile.Header.Height),
            bitmap.GetAddressOf());

        return new BgraImage(bitmap);
    }

    public void SaveAsPng(string path, string? userComment = null)
    {
        using ComPtr<IStream> fileStream = null;
        fixed (char* pathPtr = path)
        {
            PInvoke.SHCreateStreamOnFileEx(
                path,
                (uint)(STGM.STGM_WRITE | STGM.STGM_CREATE | STGM.STGM_SHARE_DENY_WRITE),
                0,
                true,
                null,
                fileStream.GetAddressOf()
            ).ThrowOnFailure();
        }

        SaveAsPng(in fileStream, userComment);

        fileStream.Get()->Commit((uint)STGC.STGC_DEFAULT);
    }

    public void SaveAsPng(MemoryStream stream, string? userComment = null)
    {
        using ComPtr<IStream> memStream = PInvoke.SHCreateMemStream(null, 0);
        if (memStream.IsNull)
            throw new OutOfMemoryException("SHCreateMemStream failed.");

        SaveAsPng(in memStream, userComment);

        memStream.Get()->Seek(0, SeekOrigin.End, out var totalBytes);
        memStream.Get()->Seek(0, SeekOrigin.Begin, null);

        if (totalBytes == 0)
            return;

        var startingPosition = stream.Position;
        stream.SetLength(startingPosition + (long)totalBytes);

        var targetSpan = stream.GetBuffer().AsSpan((int)startingPosition, (int)totalBytes);

        fixed (byte* bufferPtr = targetSpan)
        {
            uint bytesRead;
            memStream.Get()->Read(bufferPtr, (uint)targetSpan.Length, &bytesRead).ThrowOnFailure();
        }

        stream.Position = startingPosition + (long)totalBytes;
    }

    private void SaveAsPng(in ComPtr<IStream> stream, string? userComment)
    {
        using var wicFactory = CreateWicFactory();

        // Create & initialize converter for BGRA to BGR
        using ComPtr<IWICFormatConverter> converter = null;
        wicFactory.Get()->CreateFormatConverter(converter.GetAddressOf());

        converter.Get()->Initialize(
            _bitmap.Cast<IWICBitmapSource>(),
            PInvoke.GUID_WICPixelFormat24bppBGR,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom);

        // Create & initialize PNG encoder
        using ComPtr<IWICBitmapEncoder> encoder = wicFactory.Get()->CreateEncoder(PInvoke.GUID_ContainerFormatPng, Guid.Empty);
        encoder.Get()->Initialize(stream.Get(), WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache);

        // Create & initialize frame and property bag
        using ComPtr<IWICBitmapFrameEncode> frame = null;
        using ComPtr<IPropertyBag2> propertyBag = null;
        encoder.Get()->CreateNewFrame(frame.GetAddressOf(), propertyBag.GetAddressOf());
        frame.Get()->Initialize(propertyBag);

        // Set size
        frame.Get()->SetSize(Width, Height);

        // Set pixel format
        var fmtGuid = PInvoke.GUID_WICPixelFormat24bppBGR;
        frame.Get()->SetPixelFormat(ref fmtGuid);
        if (fmtGuid != PInvoke.GUID_WICPixelFormat24bppBGR)
            throw new Exception("WICPixelFormat24bppBGR not supported");

        // Get metadata writer
        using ComPtr<IWICMetadataQueryWriter> metaWriter = null;
        frame.Get()->GetMetadataQueryWriter(metaWriter.GetAddressOf());

        // Write user comment
        if (!string.IsNullOrEmpty(userComment))
        {
            fixed (char* commentPtr = userComment)
            {
                var propVar = new PROPVARIANT();
                propVar.Anonymous.Anonymous.vt = VARENUM.VT_LPWSTR;
                propVar.Anonymous.Anonymous.Anonymous.pwszVal = commentPtr;
                metaWriter.Get()->SetMetadataByName("/tEXt/{str=Comment}", propVar);
            }
        }

        // Write RGB data from converter to frame
        frame.Get()->WriteSource(converter.Cast<IWICBitmapSource>(), null);

        // Commit to everything
        frame.Get()->Commit();
        encoder.Get()->Commit();
    }

    public void Resize(uint width, uint height, WICBitmapInterpolationMode interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeHighQualityCubic)
    {
        using var wicFactory = CreateWicFactory();

        using ComPtr<IWICBitmapScaler> scaler = null;
        wicFactory.Get()->CreateBitmapScaler(scaler.GetAddressOf());

        scaler.Get()->Initialize(
            _bitmap.Cast<IWICBitmapSource>(),
            width,
            height,
            interpolationMode);

        ComPtr<IWICBitmap> newBitmap = null;
        wicFactory.Get()->CreateBitmapFromSource(
            scaler.Cast<IWICBitmapSource>(),
            WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
            newBitmap.GetAddressOf());

        _bitmap.Dispose();
        _bitmap = newBitmap;
    }

    public IDalamudTextureWrap AsDalamudTextureWrap(ITextureProvider textureProvider)
    {
        using ComPtr<IWICBitmapLock> bitmapLock = null;

        _bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockRead, bitmapLock.GetAddressOf());

        byte* data;
        bitmapLock.Get()->GetDataPointer(out var bufferSize, &data);
        bitmapLock.Get()->GetStride(out var rowPitch);

        if (rowPitch != Width * 4)
            throw new InvalidOperationException("Invalid row pitch");

        return textureProvider.CreateFromRaw(RawImageSpecification.Bgra32((int)Width, (int)Height), new ReadOnlySpan<byte>(data, (int)(rowPitch * Height)));
    }

    public BgraImage Clone()
    {
        ComPtr<IWICBitmap> copy = null;

        using var wicFactory = CreateWicFactory();

        wicFactory.Get()->CreateBitmapFromSource(
            _bitmap.Cast<IWICBitmapSource>(),
            WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
            copy.GetAddressOf());

        return new BgraImage(copy);
    }

    public void CompositeLayers(params Span<BgraImage> layers)
    {
        using ComPtr<IWICBitmapLock> dstLock = null;

        _bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockWrite, dstLock.GetAddressOf());

        byte* dst;
        dstLock.Get()->GetDataPointer(out var dstSize, &dst);
        dstLock.Get()->GetStride(out var dstStride);

        foreach (var layer in layers)
        {
            using ComPtr<IWICBitmapLock> srcLock = null;

            layer._bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockRead, srcLock.GetAddressOf());

            byte* src;
            srcLock.Get()->GetDataPointer(out var srcSize, &src);
            srcLock.Get()->GetStride(out var srcStride);

            for (var y = 0; y < Height; y++)
            {
                var srcRow = src + y * srcStride;
                var dstRow = dst + y * dstStride;

                for (var x = 0; x < Width; x++)
                {
                    var s = srcRow + x * 4;
                    var d = dstRow + x * 4;

                    var sA = s[3] / 255f;
                    var dA = d[3] / 255f;
                    var outA = sA + dA * (1 - sA);

                    if (outA > 0)
                    {
                        d[0] = (byte)((s[0] * sA + d[0] * dA * (1 - sA)) / outA);
                        d[1] = (byte)((s[1] * sA + d[1] * dA * (1 - sA)) / outA);
                        d[2] = (byte)((s[2] * sA + d[2] * dA * (1 - sA)) / outA);
                        d[3] = (byte)(outA * 255f);
                    }
                    else
                    {
                        d[0] = 0;
                        d[1] = 0;
                        d[2] = 0;
                        d[3] = 0;
                    }
                }
            }
        }
    }

    public void CopyPixelDataTo(Span<byte> pixelSpan)
    {
        using ComPtr<IWICBitmapLock> srcLock = null;

        _bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockRead, srcLock.GetAddressOf());

        byte* src;
        srcLock.Get()->GetDataPointer(out var srcSize, &src);

        Buffer.MemoryCopy(src, pixelSpan.GetPointer(0), pixelSpan.Length, srcSize);
    }

    private static ComPtr<IWICImagingFactory> CreateWicFactory()
    {
        PInvoke.CoCreateInstance<IWICImagingFactory>(PInvoke.CLSID_WICImagingFactory, null, CLSCTX.CLSCTX_INPROC_SERVER, out var wicFactory);
        return wicFactory;
    }
}
