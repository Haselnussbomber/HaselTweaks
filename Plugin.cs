using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using HaselTweaks.Windows;
using DalamudFramework = Dalamud.Game.Framework;

namespace HaselTweaks;

public class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal static readonly WindowSystem WindowSystem = new("HaselTweaks");
    private readonly PluginWindow PluginWindow;

    internal List<Tweak> Tweaks = new();

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        var module = Process.GetCurrentProcess().MainModule;
        if (module == null)
            throw new Exception("Unable to access process module.");

        var gameVersion = File.ReadAllText(Path.Combine(Directory.GetParent(module.FileName)!.FullName, "ffxivgame.ver"));
        if (string.IsNullOrEmpty(gameVersion))
            throw new Exception("Unable to read game version.");

        pluginInterface.Create<Service>();

        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Tweak)) && !t.IsAbstract))
        {
            try
            {
                Tweaks.Add((Tweak)Activator.CreateInstance(t)!);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed initializing tweak '{t.Name}'.");
            }
        }

        Configuration.Load(Tweaks.Select(t => t.InternalName).ToArray(), gameVersion);

        Interop.Resolver.GetInstance.SetupSearchSpace(Service.SigScanner.SearchBase);
        Interop.Resolver.GetInstance.Resolve();

        PluginWindow = new PluginWindow(this);
        WindowSystem.AddWindow(PluginWindow);

        Service.PluginInterface.UiBuilder.Draw += OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

        Service.Commands.AddHandler("/haseltweaks", new CommandInfo(OnCommand)
        {
            HelpMessage = "Show Window"
        });

        // ensure Framework is set up
        Service.Framework.RunOnFrameworkThread(SetupTweaks);
    }

    private void SetupTweaks()
    {
        foreach (var tweak in Tweaks)
        {
            try
            {
                tweak.SetupInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed setting up tweak '{tweak.InternalName}'.");
            }

            if (Configuration.Instance.EnabledTweaks.Contains(tweak.InternalName))
            {
                try
                {
                    tweak.EnableInternal();
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed enabling tweak '{tweak.InternalName}'.");
                }
            }
        }

        Service.Framework.Update += OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(DalamudFramework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnFrameworkUpdate(framework);
        }
    }

    private void OnDraw()
    {
        try
        {
            WindowSystem.Draw();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Unexpected exception in OnDraw");
        }
    }

    private void OnOpenConfigUi()
    {
        PluginWindow.Toggle();
    }

    private void OnCommand(string command, string args)
    {
        PluginWindow.Toggle();
    }

    void IDisposable.Dispose()
    {
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.PluginInterface.UiBuilder.Draw -= OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

        Service.Commands.RemoveHandler("/haseltweaks");

        foreach (var tweak in Tweaks.ToArray())
        {
            if (tweak.Enabled)
            {
                try
                {
                    tweak.DisableInternal();
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed unloading tweak '{tweak.Name}'.");
                }
            }

            try
            {
                tweak.DisposeInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed disposing tweak '{tweak.Name}'.");
            }

            Tweaks.Remove(tweak);
        }

        WindowSystem.RemoveAllWindows();

        Configuration.Save();
        ((IDisposable?)Configuration.Instance)?.Dispose();
    }
}
