using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace HaselTweaks.Services;

[RegisterSingleton, AutoConstruct]
public partial class NativeService : IDisposable
{
    private readonly ILogger<NativeService> _logger;
    private readonly IDalamudPluginInterface _pluginInterface;
    private HMODULE _library = HMODULE.Null;

    public nint InflateAddr { get; private set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        var dllPath = Path.Join(_pluginInterface.AssemblyLocation.Directory!.FullName, "HaselTweaks.Native.dll");

        _library = PInvoke.LoadLibrary(dllPath);

        if (_library.IsNull)
        {
            _logger.LogError("Failed to load {dllPath} ({code})", dllPath, Marshal.GetLastWin32Error());
            return;
        }

        InflateAddr = PInvoke.GetProcAddress(_library, "Inflate");
    }

    public void Dispose()
    {
        if (!_library.IsNull)
        {
            PInvoke.FreeLibrary(_library);
            _library = HMODULE.Null;
        }
    }
}
