namespace HaselTweaks.Utils;

public abstract class LockableWindow : SimpleWindow
{
    private static readonly ImGuiWindowFlags LockedWindowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

    private readonly TextService _textService = Service.Get<TextService>();
    private readonly PluginConfig _pluginConfig = Service.Get<PluginConfig>();

    private readonly TitleBarButton _lockButton;

    public LockableWindow(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (_pluginConfig.LockedImGuiWindows.Contains(WindowName))
            Flags |= LockedWindowFlags;

        _lockButton = new TitleBarButton()
        {
            Icon = WindowLocked
                ? FontAwesomeIcon.Lock
                : FontAwesomeIcon.LockOpen,
            IconOffset = new(2.5f, 1f),
            ShowTooltip = () =>
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(_textService.Translate(WindowLocked
                    ? "ImGuiWindow.WindowLocked"
                    : "ImGuiWindow.WindowUnlocked"));
                ImGui.EndTooltip();
            },
            Click = (button) =>
            {
                WindowLocked = !WindowLocked;
                _lockButton!.Icon = WindowLocked
                    ? FontAwesomeIcon.Lock
                    : FontAwesomeIcon.LockOpen;
            }
        };

        TitleBarButtons.Add(_lockButton);
    }

    protected bool WindowLocked
    {
        get => Flags.HasFlag(LockedWindowFlags);
        set
        {
            var config = _pluginConfig;
            if (WindowLocked && !value)
            {
                Flags &= ~LockedWindowFlags;
                config.LockedImGuiWindows.Remove(WindowName);
                config.Save();
            }
            else if (!WindowLocked && value)
            {
                Flags |= LockedWindowFlags;
                config.LockedImGuiWindows.Add(WindowName);
                config.Save();
            }
        }
    }
}
