using System.Threading;
using System.Threading.Tasks;
using HaselCommon.Commands;
using HaselTweaks.Windows;

namespace HaselTweaks.Services;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class CommandManager : IHostedService
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly WindowManager _windowManager;
    private readonly CommandService _commandService;
    private CommandHandler? _haselTweaksCommandHandler;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _haselTweaksCommandHandler = _commandService.Register(OnHaselTweaksCommand, true);
        _pluginInterface.UiBuilder.OpenConfigUi += TogglePluginWindow;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _haselTweaksCommandHandler?.Dispose();
        _haselTweaksCommandHandler = null;

        _pluginInterface.UiBuilder.OpenConfigUi -= TogglePluginWindow;

        return Task.CompletedTask;
    }

    [CommandHandler("/haseltweaks", "HaselTweaks.CommandHandlerHelpMessage", true, 1)]
    private void OnHaselTweaksCommand(string command, string arguments)
    {
        TogglePluginWindow();
    }

    private void TogglePluginWindow()
    {
        _windowManager.CreateOrToggle<PluginWindow>();
    }
}
