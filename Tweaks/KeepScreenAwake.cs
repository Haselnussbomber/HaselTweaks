using System.Timers;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Keep Screen Awake",
    Description: "Prevents the screen from going into standby."
)]
public partial class KeepScreenAwake : Tweak
{
    private readonly Timer _timer = new();

    public KeepScreenAwake()
    {
        _timer.Elapsed += Timer_Elapsed;
        _timer.Interval = 10000; // every 10 seconds
    }

    public override void Enable()
    {
        _timer.Start();
    }

    public override void Disable()
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        _timer.Stop();
    }

    public override void Dispose()
    {
        _timer.Dispose();
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
