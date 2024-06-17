using System.Timers;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace HaselTweaks.Tweaks;

public sealed class KeepScreenAwake : ITweak
{
    private readonly Timer _timer = new();

    public string InternalName => nameof(KeepScreenAwake);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _timer.Elapsed += Timer_Elapsed;
        _timer.Interval = 10000; // every 10 seconds
    }

    public void OnEnable()
    {
        _timer.Start();
    }

    public void OnDisable()
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        _timer.Stop();
    }

    public void Dispose()
    {
        OnDisable();
        _timer.Dispose();
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
