using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace HaselTweaks.Services;

public class WindowManager : WindowSystem, IDisposable
{
    private readonly DalamudPluginInterface _pluginInterface;

    public WindowManager(DalamudPluginInterface pluginInterface) : base(pluginInterface.InternalName)
    {
        _pluginInterface = pluginInterface;

        pluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        _pluginInterface.UiBuilder.Draw -= Draw;

        foreach (var window in Windows)
        {
            (window as IDisposable)?.Dispose();
        }

        RemoveAllWindows();
    }

    public T? GetWindow<T>() where T : Window
    {
        return Windows.OfType<T>().FirstOrDefault();
    }

    public T OpenWindow<T>() where T : Window, new()
    {
        var window = GetWindow<T>();
        if (window is null)
        {
            AddWindow(window = new T());
        }

        window.IsOpen = true;

        return window;
    }

    public void CloseWindow<T>() where T : Window
    {
        var window = GetWindow<T>();
        if (window is null)
            return;

        (window as IDisposable)?.Dispose();
        RemoveWindow(window);
    }

    public void ToggleWindow<T>() where T : Window, new()
    {
        if (GetWindow<T>() is null)
        {
            OpenWindow<T>();
        }
        else
        {
            CloseWindow<T>();
        }
    }
}
