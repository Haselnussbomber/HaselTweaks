using System.IO;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Utils.PortraitHelper;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Ole;

namespace HaselTweaks.Services.PortraitHelper;

[RegisterSingleton, AutoConstruct]
public partial class ClipboardService : IDisposable
{
    private readonly ILogger<ClipboardService> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;

    private Hook<UIClipboard.Delegates.OnClipboardDataChanged>? _onClipboardDataChangedHook;

    public ImportFlags CurrentImportFlags { get; set; } = ImportFlags.All;
    public PortraitPreset? ClipboardPreset { get; set; }

    [AutoPostConstruct]
    private unsafe void Initialize()
    {
        _onClipboardDataChangedHook = _gameInteropProvider.HookFromAddress<UIClipboard.Delegates.OnClipboardDataChanged>(
            UIClipboard.MemberFunctionPointers.OnClipboardDataChanged,
            OnClipboardDataChangedDetour);

        _onClipboardDataChangedHook?.Enable();
    }

    void IDisposable.Dispose()
    {
        _onClipboardDataChangedHook?.Dispose();
    }

    private unsafe void OnClipboardDataChangedDetour(UIClipboard* uiClipboard)
    {
        _onClipboardDataChangedHook!.Original(uiClipboard);

        try
        {
            ClipboardPreset = PortraitPreset.FromExportedString(uiClipboard->Data.SystemClipboardText.ToString());
            if (ClipboardPreset != null)
                _logger.LogDebug("Parsed ClipboardPreset: {ClipboardPreset}", ClipboardPreset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading preset");
        }
    }

    public async Task SetClipboardPortraitPreset(PortraitPreset? preset)
    {
        if (preset == null)
        {
            ClipboardPreset = null;
            return;
        }

        if (!await OpenClipboard().ConfigureAwait(false))
            return;

        try
        {
            PInvoke.EmptyClipboard();

            var presetString = preset.ToExportedString();
            var length = Encoding.UTF8.GetByteCount(presetString);

            using var writer = new ClipboardWriter(CLIPBOARD_FORMAT.CF_TEXT);

            if (!writer.TryAllocMemorySpan(length, out var span))
                return;

            Encoding.UTF8.GetBytes(presetString, span);

            if (writer.End())
                ClipboardPreset = preset;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during PortraitPreset.ToClipboard");
        }
        finally
        {
            PInvoke.CloseClipboard();
        }
    }

    internal async Task SetClipboardImage(BgraImage image)
    {
        if (!await OpenClipboard().ConfigureAwait(false))
            return;

        if (!PInvoke.EmptyClipboard())
            return;

        SetDIB(image);
        SetDIBV5(image);
        SetPNG(image);
        PInvoke.CloseClipboard();
    }

    private async Task<bool> OpenClipboard()
    {
        var start = DateTime.Now;
        HWND hwnd;

        while (!(hwnd = GetWindowHandle()).IsNull)
        {
            if (PInvoke.OpenClipboard(hwnd))
                return true;

            if (DateTime.Now - start > TimeSpan.FromSeconds(2))
            {
                _logger.LogError("Clipboard timeout: Could not open clipboard after 2 seconds.");
                return false;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        return false;
    }

    private unsafe HWND GetWindowHandle()
    {
        var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        if (framework == null)
            return HWND.Null;

        var uiClipboard = framework->GetUIClipboard();
        if (uiClipboard == null)
            return HWND.Null;

        return (HWND)uiClipboard->ThisHwnd;
    }

    private unsafe void SetDIB(BgraImage image)
    {
        using var writer = new ClipboardWriter(CLIPBOARD_FORMAT.CF_DIB);

        var width = (int)image.Width;
        var height = (int)image.Height;

        var length = sizeof(BITMAPINFOHEADER) + width * 4 * height;
        if (!writer.TryAllocMemory(length, out var memory))
            return;

        var header = (BITMAPINFOHEADER*)memory;
        header->biSize = (uint)sizeof(BITMAPINFOHEADER);
        header->biWidth = width;
        header->biHeight = -height;
        header->biPlanes = 1;
        header->biBitCount = 32; // bits per pixel
        header->biCompression = 0; // BI_RGB
        header->biSizeImage = 0;
        header->biXPelsPerMeter = 0;
        header->biYPelsPerMeter = 0;
        header->biClrUsed = 0;
        header->biClrImportant = 0;

        var pixelSpan = new Span<byte>((byte*)memory + header->biSize, width * 4 * height);
        image.CopyPixelDataTo(pixelSpan);
    }

    private unsafe void SetDIBV5(BgraImage image)
    {
        using var writer = new ClipboardWriter(CLIPBOARD_FORMAT.CF_DIBV5);

        var width = (int)image.Width;
        var height = (int)image.Height;

        var length = sizeof(BITMAPV5HEADER) + width * 4 * height;
        if (!writer.TryAllocMemory(length, out var memory))
            return;

        var bitmapInfo = (BITMAPV5HEADER*)memory;
        bitmapInfo->bV5Size = (uint)Marshal.SizeOf(typeof(BITMAPV5HEADER));
        bitmapInfo->bV5Width = width;
        bitmapInfo->bV5Height = -height; // negative height for top-down image
        bitmapInfo->bV5Planes = 1;
        bitmapInfo->bV5BitCount = 32; // 4 bytes per pixel (Rgba32)
        bitmapInfo->bV5Compression = BI_COMPRESSION.BI_RGB;
        bitmapInfo->bV5SizeImage = (uint)(width * 4 * height);
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

        var pixelSpan = new Span<byte>((byte*)memory + bitmapInfo->bV5Size, width * 4 * height);
        image.CopyPixelDataTo(pixelSpan);
    }

    private void SetPNG(BgraImage image)
    {
        var format = PInvoke.RegisterClipboardFormat("PNG");
        if (format == 0)
            return;

        using var ms = new MemoryStream();

        image.SaveAsPng(ms);

        using var writer = new ClipboardWriter(format);

        if (!writer.TryAllocMemorySpan((int)ms.Length, out var span))
            return;

        if (!ms.TryGetBuffer(out var buffer))
            return;

        buffer.AsSpan().CopyTo(span);
    }
}
