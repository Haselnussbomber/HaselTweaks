using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HaselTweaks.Utils;

public abstract class LockableWindow : Window
{
    private static readonly ImGuiWindowFlags LockedWindowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
    private readonly TitleBarButton LockButton;

    public LockableWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
        : base(name, flags, forceMainWindow)
    {
        if (Plugin.Config.LockedImGuiWindows.Contains(WindowName))
            Flags |= LockedWindowFlags;

        LockButton = new TitleBarButton()
        {
            Icon = WindowLocked
                ? FontAwesomeIcon.Lock
                : FontAwesomeIcon.LockOpen,
            IconOffset = new(2.5f, 1f),
            ShowTooltip = () =>
            {
                ImGui.SetTooltip(
                    WindowLocked
                    ? t("ImGuiWindow.WindowLocked")
                    : t("ImGuiWindow.WindowUnlocked"));
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
            if (WindowLocked && !value)
            {
                Flags &= ~LockedWindowFlags;
                Plugin.Config.LockedImGuiWindows.Remove(WindowName);
                Plugin.Config.Save();
            }
            else if (!WindowLocked && value)
            {
                Flags |= LockedWindowFlags;
                Plugin.Config.LockedImGuiWindows.Add(WindowName);
                Plugin.Config.Save();
            }
        }
    }
}
