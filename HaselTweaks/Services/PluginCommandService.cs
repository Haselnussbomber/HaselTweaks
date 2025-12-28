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
        _commandService.AddCommand("haseltweaks", cmd => cmd
            .WithHelpTextKey("HaselTweaks.CommandHandlerHelpMessage")
            .WithDisplayOrder(1)
            .WithHandler(OnMainCommand)
            .AddSubcommand("debug", cmd => cmd
#if DEBUG
                .SetEnabled(false)
#endif
                .WithHandler(OnDebugCommand)));

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
