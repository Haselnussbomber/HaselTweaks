using System.Timers;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class KeepScreenAwake : Tweak
{
    public override void OnEnable()
    {
        var timer = new Timer();

        timer.Elapsed += Timer_Elapsed;
        timer.Interval = 10000; // every 10 seconds
        timer.Start();

        _disposables = timer;
    }

    public override void OnDisable()
    {
        if (Status is not TweakStatus.Enabled)
            return;

        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

        DisposeAndNull(ref _disposables);
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
