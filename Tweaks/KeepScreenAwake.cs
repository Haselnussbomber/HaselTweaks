using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace HaselTweaks.Tweaks;

public unsafe class KeepScreenAwake : BaseTweak
{
    public override string Name => "Keep Screen Awake";

    private Timer timer = null!;

    [Flags]
    public enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

    public override void Setup(HaselTweaks plugin)
    {
        base.Setup(plugin);
        timer = new Timer();
        timer.Elapsed += Timer_Elapsed;
        timer.Interval = 10000; // every 10 seconds
    }

    public override void Enable()
    {
        base.Enable();
        timer.Start();
    }

    public override void Disable()
    {
        base.Disable();
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        timer.Stop();
    }

    public override void Dispose()
    {
        base.Dispose();
        timer.Dispose();
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
    }
}
