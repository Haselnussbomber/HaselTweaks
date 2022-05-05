using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs;
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

        Ui = new(this);

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

        Config = Configuration.Load(this);
        Config.Plugin = this;

        foreach (var tweak in Tweaks)
        {
            try
            {
                tweak.SetupInternal(this);

                if (tweak.Ready && tweak.CanLoad && Config.EnabledTweaks.Contains(tweak.InternalName))
                    tweak.EnableInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed enabling tweak '{tweak.InternalName}'.");
            }
        }

        Config.Save();

        Service.Framework.Update += OnFrameworkUpdate;
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
}

public sealed partial class Plugin : IDisposable
{
    private bool isDisposed;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposed)
            return;

        if (disposing)
        {
            Service.Framework.Update -= OnFrameworkUpdate;

            foreach (var tweak in Tweaks)
            {
                try
                {
                    if (tweak.Enabled)
                        tweak.DisableInternal();

                    tweak.DisposeInternal();
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed unloading tweak '{tweak.Name}'.");
                }
            }

            Tweaks.Clear();

            Ui.Dispose();
            XivCommon?.Dispose();
        }

        isDisposed = true;
    }
}
