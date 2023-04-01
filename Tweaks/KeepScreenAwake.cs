using System.Timers;

namespace HaselTweaks.Tweaks;

public partial class KeepScreenAwake : Tweak
{
    public override string Name => "Keep Screen Awake";
    public override string Description => "Prevents the screen from going into standby.";

    private Timer? timer;

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
        timer = new Timer();
        timer.Elapsed += Timer_Elapsed;
        timer.Interval = 10000; // every 10 seconds
    }

    public override void Enable()
    {
        timer?.Start();
    }

    public override void Disable()
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        timer?.Stop();
    }

    public override void Dispose()
    {
        timer?.Dispose();
    }

    private static void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
