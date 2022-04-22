using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XivCommon;

namespace HaselTweaks;

public unsafe partial class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal DalamudPluginInterface PluginInterface;
    internal XivCommonBase XivCommon;
    internal Configuration Config;
    internal PluginUi Ui;

    internal List<Tweak> Tweaks = new();

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;

        pluginInterface.Create<Service>();
        Resolver.Initialize();
        XivCommon = new();

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Ui = new(this);

        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Tweak)) && !t.IsAbstract))
        {
            PluginLog.Debug("Initalizing Tweak: {0}", t.Name);
            try
            {
                var tweak = (Tweak)Activator.CreateInstance(t)!;
                Tweaks.Add(tweak);
                tweak.SetupInternal(this);

                if (tweak.CanLoad && (tweak.ForceLoad || Config.EnabledTweaks.Contains(tweak.InternalName)))
                    tweak.EnableInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed loading tweak '{t.Name}'.");
            }
        }

        SaveConfig();

        Service.Framework.Update += OnFrameworkUpdate;
    }

    internal void SaveConfig()
    {
        PluginInterface.SavePluginConfig(Config);
    }

    private void OnFrameworkUpdate(Framework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Ready && tweak.Enabled)
            {
                tweak.OnFrameworkUpdate(framework);
            }
        }
    }
}

public sealed partial class Plugin : IDisposable
{
    private bool isDisposed;

    ~Plugin()
    {
        this.Dispose(false);
    }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        this.Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (this.isDisposed)
            return;

        if (disposing)
        {
            Service.Framework.Update -= OnFrameworkUpdate;

            foreach (var tweak in Tweaks)
            {
                try
                {
                    if (tweak.CanLoad)
                        tweak.DisableInternal();

                    tweak.DisposeInternal();
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed unloading tweak '{tweak.Name}'.");
                }
            }

            Tweaks.Clear();

            XivCommon?.Dispose();
        }

        this.isDisposed = true;
    }
}
