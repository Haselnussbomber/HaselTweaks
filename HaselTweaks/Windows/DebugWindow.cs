namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public partial class DebugWindow : SimpleDebugWindow
{
    [AutoPostConstruct]
    private void Initialize()
    {
        WindowName = "HaselTweaks Debug Window";
    }
}
