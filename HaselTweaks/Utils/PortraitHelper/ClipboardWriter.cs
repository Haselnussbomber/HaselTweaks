using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Ole;

namespace HaselTweaks.Utils.PortraitHelper;

public unsafe struct ClipboardWriter : IDisposable
{
    private readonly uint _format;
    private HGLOBAL _hMem = HGLOBAL.Null;

    public ClipboardWriter()
    {
        throw new InvalidOperationException("Format required.");
    }

    public ClipboardWriter(uint format)
    {
        ArgumentOutOfRangeException.ThrowIfZero(format);
        _format = format;
    }

    internal ClipboardWriter(CLIPBOARD_FORMAT format)
    {
        _format = (uint)format;
    }

    public bool TryAllocMemory(int length, out void* address)
    {
        var hMem = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE | GLOBAL_ALLOC_FLAGS.GMEM_ZEROINIT, (nuint)length + 1); // + 1 for null terminator
        if (hMem.IsNull)
        {
            address = null;
            return false;
        }

        var lockedMem = PInvoke.GlobalLock(hMem);
        if (lockedMem == null)
        {
            PInvoke.GlobalFree(hMem);
            address = null;
            return false;
        }

        address = lockedMem;
        _hMem = hMem;
        return true;
    }

    public bool TryAllocMemorySpan(int length, out Span<byte> span)
    {
        if (TryAllocMemory(length, out var address))
        {
            span = new Span<byte>(address, length);
            return true;
        }

        span = null;
        return false;
    }

    /// <summary>
    /// Sets the clipboard data.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> on success, <see langword="false"/> otherwise.
    /// </returns>
    public bool End()
    {
        if (_hMem.IsNull)
            return false;

        PInvoke.GlobalUnlock(_hMem);

        var handle = (HANDLE)_hMem.Value;

        if (PInvoke.SetClipboardData(_format, handle).IsNull)
        {
            PInvoke.GlobalFree(_hMem);
            _hMem = HGLOBAL.Null;
            return false;
        }

        _hMem = HGLOBAL.Null;
        return true;
    }

    public void Dispose()
    {
        End();
    }
}
