using System.Timers;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class KeepScreenAwake : Tweak
{
    private Timer? _timer;

    public override void OnEnable()
    {
        _timer = new();
        _timer.Elapsed += Timer_Elapsed;
        _timer.Interval = 10000; // every 10 seconds
        _timer.Start();
    }

    public override void OnDisable()
    {
        if (Status is not TweakStatus.Enabled)
            return;

        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

        _timer?.Dispose();
        _timer = null;
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
