using Dalamud.Plugin;
using HaselCommon.Services;
using HaselTweaks.Windows;

namespace HaselTweaks.Services;

[RegisterSingleton, AutoConstruct]
public partial class CommandManager : IDisposable
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly WindowManager _windowManager;
    private readonly CommandService _commandService;

    [AutoPostConstruct]
    private void Initialize()
    {
        _commandService.Register("/haseltweaks", "HaselTweaks.CommandHandlerHelpMessage", HandleCommand, autoEnable: true, displayOrder: 1);

        _pluginInterface.UiBuilder.OpenConfigUi += TogglePluginWindow;
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.OpenConfigUi -= TogglePluginWindow;
    }

    private void HandleCommand(string command, string arguments)
    {
        TogglePluginWindow();
    }

    private void TogglePluginWindow()
    {
        _windowManager.CreateOrToggle(Service.Get<PluginWindow>);
    }
}
