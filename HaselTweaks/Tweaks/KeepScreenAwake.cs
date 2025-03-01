using System.Timers;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
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
        if (Status is not TweakStatus.Enabled)
            return;

        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        _timer.Stop();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _timer.Dispose();

        Status = TweakStatus.Disposed;
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
