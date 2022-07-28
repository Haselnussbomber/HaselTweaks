using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using XivCommon;

namespace HaselTweaks;

public class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal static XivCommonBase XivCommon = null!;

    private readonly WindowSystem windowSystem = new("HaselTweaks");
    private readonly PluginWindow pluginWindow;

    internal List<Tweak> Tweaks = new();

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        XivCommon = new();

        this.pluginWindow = new PluginWindow(this);
        this.windowSystem.AddWindow(this.pluginWindow);

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

        Configuration.Load(Tweaks.Select(t => t.InternalName).ToArray());

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

            if (tweak.Ready && Configuration.Instance.EnabledTweaks.Contains(tweak.InternalName))
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

        Service.Framework.Update += this.OnFrameworkUpdate;
        Service.PluginInterface.UiBuilder.Draw += this.OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;

        Service.Commands.AddHandler("/haseltweaks", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Show Window"
        });
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
            {
                tweak.OnFrameworkUpdate(framework);
            }
        }
    }

    private void OnDraw()
    {
        try
        {
            this.windowSystem.Draw();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Unexpected exception in OnDraw");
        }
    }

    private void OnOpenConfigUi()
    {
        this.pluginWindow.Toggle();
    }

    private void OnCommand(string command, string args)
    {
        this.pluginWindow.Toggle();
    }

    void IDisposable.Dispose()
    {
        Service.Framework.Update -= this.OnFrameworkUpdate;
        Service.PluginInterface.UiBuilder.Draw -= this.OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;

        Service.Commands.RemoveHandler("/haseltweaks");

        this.windowSystem.RemoveAllWindows();

        foreach (var tweak in Tweaks)
        {
            try
            {
                tweak.DisableInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed unloading tweak '{tweak.Name}'.");
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

        Tweaks.Clear();

        Configuration.Save();
        ((IDisposable)Configuration.Instance).Dispose();

        XivCommon?.Dispose();
        XivCommon = null!;
    }
}
