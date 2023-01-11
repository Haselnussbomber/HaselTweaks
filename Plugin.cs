using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using HaselTweaks.Windows;

namespace HaselTweaks;

public class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal static readonly WindowSystem WindowSystem = new("HaselTweaks");
    private readonly PluginWindow PluginWindow;

    internal List<Tweak> Tweaks = new();

    public Plugin(DalamudPluginInterface pluginInterface)
    {
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

        string? gameVersion = null;
        unsafe
        {
            gameVersion = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GameVersion.Base;
        }

        Configuration.Load(Tweaks.Select(t => t.InternalName).ToArray(), gameVersion);

        Interop.Resolver.GetInstance.SetupSearchSpace(Service.SigScanner.SearchBase);
        Interop.Resolver.GetInstance.Resolve();

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

        PluginWindow = new PluginWindow(this);
        WindowSystem.AddWindow(PluginWindow);

        Service.Framework.Update += OnFrameworkUpdate;
        Service.PluginInterface.UiBuilder.Draw += OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

        Service.Commands.AddHandler("/haseltweaks", new CommandInfo(OnCommand)
        {
            HelpMessage = "Show Window"
        });
    }

    private void OnFrameworkUpdate(Framework framework)
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

        foreach (var tweak in Tweaks)
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
        }

        WindowSystem.RemoveAllWindows();

        Tweaks.Clear();

        Configuration.Save();
        ((IDisposable)Configuration.Instance).Dispose();
    }
}
