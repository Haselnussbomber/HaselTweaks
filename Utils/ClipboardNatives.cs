using System.Threading.Tasks;

namespace HaselTweaks.Utils;

public static class ClipboardNatives
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

    public static async Task OpenClipboard()
    {
        while (!OpenClipboard(0))
        {
            await Task.Delay(100);
        }
    }
}
