using System.Threading;
using System.Threading.Tasks;
using HaselCommon.Services.Commands;
using HaselTweaks.Windows;

namespace HaselTweaks.Services;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class PluginCommandService : IHostedService
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly CommandService _commandService;
    private readonly WindowManager _windowManager;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commandService.AddCommand("haseltweaks", cmd =>
        {
            cmd.WithHelpTextKey("HaselTweaks.CommandHandlerHelpMessage");
            cmd.WithDisplayOrder(1);
            cmd.WithHandler(OnMainCommand);

#if DEBUG
            cmd.AddSubcommand("debug", cmd =>
            {
                cmd.WithHandler(OnDebugCommand);
            });
#endif
        });

        _pluginInterface.UiBuilder.OpenConfigUi += TogglePluginWindow;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pluginInterface.UiBuilder.OpenConfigUi -= TogglePluginWindow;

        return Task.CompletedTask;
    }

    private void OnMainCommand(CommandContext ctx)
    {
        TogglePluginWindow();
    }

    private void OnDebugCommand(CommandContext context)
    {
        _windowManager.CreateOrToggle<DebugWindow>();
    }

    private void TogglePluginWindow()
    {
        _windowManager.CreateOrToggle<PluginWindow>();
    }
}
