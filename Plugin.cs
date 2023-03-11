using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using HaselTweaks.Utils;
using HaselTweaks.Windows;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal static WindowSystem WindowSystem = new("HaselTweaks");
    internal static List<Tweak> Tweaks = new();
    internal static Configuration Config = null!;

    private PluginWindow? PluginWindow;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        // ensure Framework is set up
        Service.Framework.RunOnFrameworkThread(Setup);
    }

    private unsafe void Setup()
    {
        var gameVersion = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GameVersion.Base;
        if (string.IsNullOrEmpty(gameVersion))
            throw new Exception("Unable to read game version.");

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

        Config = Configuration.Load(Tweaks.Select(t => t.InternalName).ToArray(), gameVersion);

        Interop.Resolver.GetInstance.SetupSearchSpace(Service.SigScanner.SearchBase);
        Interop.Resolver.GetInstance.Resolve();

        PluginWindow = new PluginWindow();
        WindowSystem.AddWindow(PluginWindow);

        Service.PluginInterface.UiBuilder.Draw += OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

        Service.Commands.AddHandler("/haseltweaks", new CommandInfo(OnCommand)
        {
            HelpMessage = "Show Window"
        });

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

            if (Config.EnabledTweaks.Contains(tweak.InternalName))
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

    private void OnFrameworkUpdate(Framework framework)
    {
        Service.AddonObserver.Update();

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
        PluginWindow!.Toggle();
    }

    private void OnCommand(string command, string args)
    {
        PluginWindow!.Toggle();
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
        WindowSystem = null!;

        Tweaks = null!;

        Config.Save();
        Config = null!;

        foreach (var kv in ImGuiUtils.IconCache)
        {
            kv.Value?.Dispose();
        }

        ImGuiUtils.IconCache.Clear();

        ((IDisposable)Service.StringUtils).Dispose();
        Service.StringUtils = null!;
    }
}
