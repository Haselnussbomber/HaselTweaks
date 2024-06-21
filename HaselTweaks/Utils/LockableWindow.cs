using Dalamud.Interface;
using HaselCommon.Services;
using HaselCommon.Windowing;
using HaselCommon.Windowing.Interfaces;
using HaselTweaks.Config;
using ImGuiNET;

namespace HaselTweaks.Utils;

public abstract class LockableWindow : SimpleWindow
{
    private static readonly ImGuiWindowFlags LockedWindowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
    private readonly TitleBarButton LockButton;

    public readonly PluginConfig PluginConfig;

    public LockableWindow(IWindowManager windowManager, PluginConfig pluginConfig, TextService textService, string name)
        : base(windowManager, name)
    {
        PluginConfig = pluginConfig;

        if (pluginConfig.LockedImGuiWindows.Contains(WindowName))
            Flags |= LockedWindowFlags;

        LockButton = new TitleBarButton()
        {
            Icon = WindowLocked
                ? FontAwesomeIcon.Lock
                : FontAwesomeIcon.LockOpen,
            IconOffset = new(2.5f, 1f),
            ShowTooltip = () =>
            {
                ImGui.BeginTooltip();
                textService.Draw(
                    WindowLocked
                    ? "ImGuiWindow.WindowLocked"
                    : "ImGuiWindow.WindowUnlocked");
                ImGui.EndTooltip();
            },
            Click = (button) =>
            {
                WindowLocked = !WindowLocked;
                LockButton!.Icon = WindowLocked
                    ? FontAwesomeIcon.Lock
                    : FontAwesomeIcon.LockOpen;
            }
        };

        TitleBarButtons.Add(LockButton);
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
