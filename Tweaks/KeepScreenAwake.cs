using System.Timers;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Keep Screen Awake",
    Description: "Prevents the screen from going into standby."
)]
public partial class KeepScreenAwake : Tweak
{
    private Timer? _timer;

    [Flags]
    public enum EXECUTION_STATE : uint
    {
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    public override void Setup()
    {
        _timer = new Timer();
        _timer.Elapsed += Timer_Elapsed;
        _timer.Interval = 10000; // every 10 seconds
    }

    public override void Enable()
    {
        _timer?.Start();
    }

    public override void Disable()
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        _timer?.Stop();
    }

    public override void Dispose()
    {
        _timer?.Dispose();
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
