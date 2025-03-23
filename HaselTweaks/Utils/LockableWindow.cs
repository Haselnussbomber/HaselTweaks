using Dalamud.Interface;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Config;
using ImGuiNET;

namespace HaselTweaks.Utils;

public abstract class LockableWindow : SimpleWindow
{
    private static readonly ImGuiWindowFlags LockedWindowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
    private readonly TitleBarButton _lockButton;

    public readonly PluginConfig PluginConfig;

    public LockableWindow(WindowManager windowManager, TextService textService, LanguageProvider languageProvider, PluginConfig pluginConfig)
        : base(windowManager, textService, languageProvider)
    {
        PluginConfig = pluginConfig;

        if (pluginConfig.LockedImGuiWindows.Contains(WindowName))
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
                ImGui.TextUnformatted(textService.Translate(WindowLocked
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
            var config = PluginConfig;
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
