using System.IO;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Lumina.Data.Files;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace HaselTweaks.Utils.PortraitHelper;

public unsafe class BgraImage : IDisposable
{
    private ComPtr<IWICBitmap> _bitmap;

    public (uint Width, uint Height) Size
    {
        get
        {
            var bitmap = _bitmap.Get();
            if (bitmap == null)
                return (0, 0);

            uint width, height;
            bitmap->GetSize(&width, &height);
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

        D3D11_TEXTURE2D_DESC desc;
        texture->GetDesc(&desc);

        desc.BindFlags = 0;
        desc.CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
        desc.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
        desc.MiscFlags = 0;
        desc.MipLevels = 1;

        using ComPtr<ID3D11Texture2D> stagingTexture = null;
        device->CreateTexture2D(&desc, null, stagingTexture.GetAddressOf()).ThrowOnError();

        if (desc.Format != DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM &&
            desc.Format != DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS &&
            desc.Format != DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB)
        {
            throw new Exception($"Unsupported image format. Expected a B8G8R8A8 variant, got {desc.Format}.");
        }

        using ComPtr<ID3D11DeviceContext> context = null;
        device->GetImmediateContext(context.GetAddressOf());

        context.Get()->CopyResource((ID3D11Resource*)stagingTexture.Get(), (ID3D11Resource*)texture);

        D3D11_MAPPED_SUBRESOURCE mapped;
        context.Get()->Map((ID3D11Resource*)stagingTexture.Get(), 0, D3D11_MAP.D3D11_MAP_READ, 0, &mapped).ThrowOnError();

        try
        {
            using var wicFactory = CreateWicFactory();

            ComPtr<IWICBitmap> bitmap = null;
            var srcFmt = GUID.GUID_WICPixelFormat32bppBGRA;
            wicFactory.Get()->CreateBitmapFromMemory(
                desc.Width,
                desc.Height,
                &srcFmt,
                mapped.RowPitch,
                mapped.RowPitch * desc.Height,
                (byte*)mapped.pData,
                bitmap.GetAddressOf()
            ).ThrowOnError();

            return new BgraImage(bitmap);
        }
        finally
        {
            context.Get()->Unmap((ID3D11Resource*)stagingTexture.Get(), 0);
        }
    }

    public static BgraImage FromFile(string path)
    {
        using var wicFactory = CreateWicFactory();

        using ComPtr<IWICStream> wicStream = null;
        wicFactory.Get()->CreateStream(wicStream.GetAddressOf()).ThrowOnError();

        fixed (char* pathPtr = path)
            wicStream.Get()->InitializeFromFilename(pathPtr, GENERIC_READ).ThrowOnError();

        using ComPtr<IWICBitmapDecoder> decoder = null;
        wicFactory.Get()->CreateDecoderFromStream(
            (IStream*)wicStream.Get(),
            null,
            WICDecodeOptions.WICDecodeMetadataCacheOnDemand,
            decoder.GetAddressOf()
        ).ThrowOnError();

        using ComPtr<IWICBitmapFrameDecode> frame = null;
        decoder.Get()->GetFrame(0, frame.GetAddressOf()).ThrowOnError();

        using ComPtr<IWICFormatConverter> converter = null;
        wicFactory.Get()->CreateFormatConverter(converter.GetAddressOf()).ThrowOnError();

        var dstFmt = GUID.GUID_WICPixelFormat32bppBGRA;
        converter.Get()->Initialize(
            (IWICBitmapSource*)frame.Get(),
            &dstFmt,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom
        ).ThrowOnError();

        uint width, height;
        converter.Get()->GetSize(&width, &height).ThrowOnError();

        ComPtr<IWICBitmap> bitmap = null;
        wicFactory.Get()->CreateBitmapFromSource(
            (IWICBitmapSource*)converter.Get(),
            WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
            bitmap.GetAddressOf()
        ).ThrowOnError();

        return new BgraImage(bitmap);
    }

    public static BgraImage FromTexFile(TexFile texFile)
    {
        using var wicFactory = CreateWicFactory();

        ComPtr<IWICBitmap> bitmap = null;
        var srcFmt = GUID.GUID_WICPixelFormat32bppBGRA;
        wicFactory.Get()->CreateBitmapFromMemory(
            texFile.Header.Width,
            texFile.Header.Height,
            &srcFmt,
            texFile.Header.Width * 4u,
            texFile.Header.Width * 4u * texFile.Header.Height,
            texFile.ImageData.GetPointer(0),
            bitmap.GetAddressOf()
        ).ThrowOnError();

        return new BgraImage(bitmap);
    }

    public void SaveAsPng(string path, string? userComment = null)
    {
        using ComPtr<IStream> fileStream = null;
        fixed (char* pathPtr = path)
        {
            SHCreateStreamOnFileEx(
                pathPtr,
                STGM.STGM_WRITE | STGM.STGM_CREATE | STGM.STGM_SHARE_DENY_WRITE,
                0,
                true,
                null,
                fileStream.GetAddressOf()
            ).ThrowOnError();
        }

        SaveAsPng(in fileStream, userComment);

        fileStream.Get()->Commit((uint)STGC.STGC_DEFAULT).ThrowOnError();
    }

    public void SaveAsPng(MemoryStream stream, string? userComment = null)
    {
        using ComPtr<IStream> memStream = SHCreateMemStream(null, 0);
        if (memStream.Get() == null)
            throw new OutOfMemoryException("SHCreateMemStream failed.");

        SaveAsPng(in memStream, userComment);

        LARGE_INTEGER zero = default;
        ULARGE_INTEGER size = default;
        memStream.Get()->Seek(zero, (uint)STREAM_SEEK.STREAM_SEEK_END, &size).ThrowOnError();
        memStream.Get()->Seek(zero, (uint)STREAM_SEEK.STREAM_SEEK_SET, null).ThrowOnError();

        var totalBytes = size.QuadPart;
        if (totalBytes == 0)
            return;

        var startingPosition = stream.Position;
        stream.SetLength(startingPosition + (long)totalBytes);

        var targetSpan = stream.GetBuffer().AsSpan((int)startingPosition, (int)totalBytes);

        fixed (byte* bufferPtr = targetSpan)
        {
            uint bytesRead;
            memStream.Get()->Read(bufferPtr, (uint)targetSpan.Length, &bytesRead).ThrowOnError();
        }

        stream.Position = startingPosition + (long)totalBytes;
    }

    private void SaveAsPng(in ComPtr<IStream> stream, string? userComment)
    {
        using var wicFactory = CreateWicFactory();

        // Create & initialize converter for BGRA to BGR
        using ComPtr<IWICFormatConverter> converter = null;
        wicFactory.Get()->CreateFormatConverter(converter.GetAddressOf()).ThrowOnError();

        var dstFmt = GUID.GUID_WICPixelFormat24bppBGR;
        converter.Get()->Initialize(
            (IWICBitmapSource*)_bitmap.Get(),
            &dstFmt,
            WICBitmapDitherType.WICBitmapDitherTypeNone,
            null,
            0.0,
            WICBitmapPaletteType.WICBitmapPaletteTypeCustom
        ).ThrowOnError();

        // Create & initialize PNG encoder
        var pngGuid = GUID.GUID_ContainerFormatPng;
        using ComPtr<IWICBitmapEncoder> encoder = null;
        wicFactory.Get()->CreateEncoder(&pngGuid, null, encoder.GetAddressOf()).ThrowOnError();
        encoder.Get()->Initialize(stream.Get(), WICBitmapEncoderCacheOption.WICBitmapEncoderNoCache).ThrowOnError();

        // Create & initialize frame and property bag
        using ComPtr<IWICBitmapFrameEncode> frame = null;
        using ComPtr<IPropertyBag2> propertyBag = null;
        encoder.Get()->CreateNewFrame(frame.GetAddressOf(), propertyBag.GetAddressOf()).ThrowOnError();
        frame.Get()->Initialize(propertyBag).ThrowOnError();

        // Set size
        frame.Get()->SetSize(Width, Height).ThrowOnError();

        // Set pixel format
        var fmtGuid = GUID.GUID_WICPixelFormat24bppBGR;
        frame.Get()->SetPixelFormat(&fmtGuid).ThrowOnError();

        // Get metadata writer
        using ComPtr<IWICMetadataQueryWriter> metaWriter = null;
        frame.Get()->GetMetadataQueryWriter(metaWriter.GetAddressOf()).ThrowOnError();

        // Write user comment
        if (!string.IsNullOrEmpty(userComment))
        {
            var metaPath = "/tEXt/{str=Comment}";
            fixed (char* metaPathPtr = metaPath)
            fixed (char* commentPtr = userComment)
            {
                var propVar = new PROPVARIANT
                {
                    vt = (ushort)VARENUM.VT_LPWSTR,
                    pwszVal = commentPtr
                };
                metaWriter.Get()->SetMetadataByName(metaPathPtr, &propVar).ThrowOnError();
            }
        }

        // Write RGB data from converter to frame
        frame.Get()->WriteSource((IWICBitmapSource*)converter.Get(), null).ThrowOnError();

        // Commit to everything
        frame.Get()->Commit().ThrowOnError();
        encoder.Get()->Commit().ThrowOnError();
    }

    public void Resize(uint width, uint height, WICBitmapInterpolationMode interpolationMode = WICBitmapInterpolationMode.WICBitmapInterpolationModeHighQualityCubic)
    {
        using var wicFactory = CreateWicFactory();

        using ComPtr<IWICBitmapScaler> scaler = null;
        wicFactory.Get()->CreateBitmapScaler(scaler.GetAddressOf()).ThrowOnError();

        scaler.Get()->Initialize(
            (IWICBitmapSource*)_bitmap.Get(),
            width,
            height,
            interpolationMode).ThrowOnError();

        ComPtr<IWICBitmap> newBitmap = null;
        wicFactory.Get()->CreateBitmapFromSource(
            (IWICBitmapSource*)scaler.Get(),
            WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
            newBitmap.GetAddressOf()).ThrowOnError();

        _bitmap.Dispose();
        _bitmap = newBitmap;
    }

    public IDalamudTextureWrap AsDalamudTextureWrap(ITextureProvider textureProvider)
    {
        using ComPtr<IWICBitmapLock> bitmapLock = null;
        var lockFlags = WICBitmapLockFlags.WICBitmapLockRead;

        _bitmap.Get()->Lock(null, (uint)lockFlags, bitmapLock.GetAddressOf()).ThrowOnError();

        byte* data;
        uint bufferSize;
        bitmapLock.Get()->GetDataPointer(&bufferSize, &data).ThrowOnError();

        uint rowPitch;
        bitmapLock.Get()->GetStride(&rowPitch).ThrowOnError();

        if (rowPitch != Width * 4)
            throw new InvalidOperationException("Invalid row pitch");

        return textureProvider.CreateFromRaw(RawImageSpecification.Bgra32((int)Width, (int)Height), new ReadOnlySpan<byte>(data, (int)(rowPitch * Height)));
    }

    public BgraImage Clone()
    {
        ComPtr<IWICBitmap> copy = null;

        using var wicFactory = CreateWicFactory();

        wicFactory.Get()->CreateBitmapFromSource(
            (IWICBitmapSource*)_bitmap.Get(),
            WICBitmapCreateCacheOption.WICBitmapCacheOnLoad,
            copy.GetAddressOf()
        ).ThrowOnError();

        return new BgraImage(copy);
    }

    public void CompositeLayers(params Span<BgraImage> layers)
    {
        using ComPtr<IWICBitmapLock> dstLock = null;
        _bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockWrite, dstLock.GetAddressOf()).ThrowOnError();

        byte* dst;
        uint dstSize;
        dstLock.Get()->GetDataPointer(&dstSize, &dst).ThrowOnError();

        uint dstStride;
        dstLock.Get()->GetStride(&dstStride).ThrowOnError();

        foreach (var layer in layers)
        {
            using ComPtr<IWICBitmapLock> srcLock = null;
            layer._bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockRead, srcLock.GetAddressOf()).ThrowOnError();

            byte* src;
            uint srcSize;
            srcLock.Get()->GetDataPointer(&srcSize, &src).ThrowOnError();

            uint srcStride;
            srcLock.Get()->GetStride(&srcStride).ThrowOnError();

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
        _bitmap.Get()->Lock(null, (uint)WICBitmapLockFlags.WICBitmapLockRead, srcLock.GetAddressOf()).ThrowOnError();

        byte* src;
        uint srcSize;
        srcLock.Get()->GetDataPointer(&srcSize, &src).ThrowOnError();

        Buffer.MemoryCopy(src, pixelSpan.GetPointer(0), pixelSpan.Length, srcSize);
    }

    private static ComPtr<IWICImagingFactory> CreateWicFactory()
    {
        ComPtr<IWICImagingFactory> wicFactory = null;
        var wicClsid = CLSID.CLSID_WICImagingFactory;
        Guid wicIid = __uuidof<IWICImagingFactory>();
        CoCreateInstance(&wicClsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &wicIid, (void**)wicFactory.GetAddressOf()).ThrowOnError();
        return wicFactory;
    }
}
