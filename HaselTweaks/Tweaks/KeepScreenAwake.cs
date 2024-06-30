using System.Timers;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace HaselTweaks.Tweaks;

public sealed class KeepScreenAwake : ITweak
{
    public string InternalName => nameof(KeepScreenAwake);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private readonly Timer Timer = new();

    public void OnInitialize()
    {
        Timer.Elapsed += Timer_Elapsed;
        Timer.Interval = 10000; // every 10 seconds
    }

    public void OnEnable()
    {
        Timer.Start();
    }

    public void OnDisable()
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        Timer.Stop();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        Timer.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
