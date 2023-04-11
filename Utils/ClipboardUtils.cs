using System.IO;
using System.Threading.Tasks;
using HaselTweaks.Structs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Utils;

public static class ClipboardUtils
{
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats" />
    public enum ClipboardFormat : uint
    {
        /// <summary>
        /// Text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data. Use this format for ANSI text.
        /// </summary>
        CF_TEXT = 1,

        /// <summary>
        /// A memory object containing a BITMAPINFO structure followed by the bitmap bits.
        /// </summary>
        CF_DIB = 8,

        /// <summary>
        /// A memory object containing a BITMAPV5HEADER structure followed by the bitmap color space information and the bitmap bits.
        /// </summary>
        CF_DIBV5 = 17,
    }

    [DllImport("user32.dll")]
    public static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll")]
    public static extern bool IsClipboardFormatAvailable(ClipboardFormat uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool OpenClipboard(nint hWndOwner);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    public static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    public static extern nint GetClipboardData(ClipboardFormat uFormat);

    [DllImport("user32.dll")]
    public static extern nint SetClipboardData(ClipboardFormat uFormat, nint hMem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern ClipboardFormat RegisterClipboardFormat(string lpszFormat);

    public static async Task OpenClipboard()
    {
        while (!OpenClipboard(0))
        {
            await Task.Delay(100);
        }
    }

    public static async Task SetClipboardImage(Image<Rgba32> image)
    {
        await OpenClipboard();

        EmptyClipboard();

        unsafe
        {
            SetDIB(image);
            SetDIBV5(image);
            SetPNG(image);
        }

        CloseClipboard();
    }

    private static unsafe void SetDIB(Image<Rgba32> image)
    {
        var data = Marshal.AllocHGlobal(sizeof(BITMAPINFOHEADER) + image.Width * image.Height * sizeof(Bgra32)); // tagBITMAPINFO

        var header = (BITMAPINFOHEADER*)data;
        header->biSize = sizeof(BITMAPINFOHEADER);
        header->biWidth = image.Width;
        header->biHeight = -image.Height;
        header->biPlanes = 1;
        header->biBitCount = 32; // bits per pixel
        header->biCompression = 0; // BI_RGB
        header->biSizeImage = 0;
        header->biXPelsPerMeter = 0;
        header->biYPelsPerMeter = 0;
        header->biClrUsed = 0;
        header->biClrImportant = 0;

        var pixelSpan = new Span<Bgra32>((void*)(data + header->biSize), image.Width * image.Height);
        image.CloneAs<Bgra32>().CopyPixelDataTo(pixelSpan);

        foreach (ref var pixel in pixelSpan)
            pixel.A = 0; // rgbReserved of RGBQUAD "must be zero"

        SetClipboardData(ClipboardFormat.CF_DIB, data);
    }

    private static unsafe void SetDIBV5(Image<Rgba32> image)
    {
        var data = Marshal.AllocHGlobal(sizeof(BITMAPV5HEADER) + image.Width * image.Height * sizeof(Bgra32));

        var bitmapInfo = (BITMAPV5HEADER*)data;
        bitmapInfo->bV5Size = (uint)Marshal.SizeOf(typeof(BITMAPV5HEADER));
        bitmapInfo->bV5Width = image.Width;
        bitmapInfo->bV5Height = -image.Height; // negative height for top-down image
        bitmapInfo->bV5Planes = 1;
        bitmapInfo->bV5BitCount = 32; // 4 bytes per pixel (Rgba32)
        bitmapInfo->bV5Compression = 0; // BI_RGB
        bitmapInfo->bV5SizeImage = (uint)(image.Width * image.Height * sizeof(Bgra32));
        bitmapInfo->bV5XPelsPerMeter = 0;
        bitmapInfo->bV5YPelsPerMeter = 0;
        bitmapInfo->bV5ClrUsed = 0;
        bitmapInfo->bV5ClrImportant = 0;
        bitmapInfo->bV5RedMask = 0x00FF0000;
        bitmapInfo->bV5GreenMask = 0x0000FF00;
        bitmapInfo->bV5BlueMask = 0x000000FF;
        bitmapInfo->bV5AlphaMask = 0xFF000000;
        bitmapInfo->bV5CSType = 0x73524742; // 'sRGB'
        bitmapInfo->bV5Endpoints = new CIEXYZTRIPLE();
        bitmapInfo->bV5GammaRed = 0;
        bitmapInfo->bV5GammaGreen = 0;
        bitmapInfo->bV5GammaBlue = 0;
        bitmapInfo->bV5Intent = 4; // LCS_COLORIMETRIC
        bitmapInfo->bV5ProfileData = 0;
        bitmapInfo->bV5ProfileSize = 0;
        bitmapInfo->bV5Reserved = 0;

        var pixelSpan = new Span<Bgra32>((void*)(data + bitmapInfo->bV5Size), image.Width * image.Height);
        image.CloneAs<Bgra32>().CopyPixelDataTo(pixelSpan);

        foreach (ref var pixel in pixelSpan)
            pixel.A = 0; // rgbReserved of RGBQUAD "must be zero"

        SetClipboardData(ClipboardFormat.CF_DIBV5, data);
    }

    private static unsafe void SetPNG(Image<Rgba32> image)
    {
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        var ptr = (nint)MemoryUtils.FromByteArray(ms.ToArray());
        SetClipboardData(RegisterClipboardFormat("PNG"), ptr);
    }
}
