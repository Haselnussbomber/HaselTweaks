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

public unsafe partial class HaselTweaks : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    public List<BaseTweak> Tweaks = new();
    public XivCommonBase XivCommon;

    public HaselTweaks(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Resolver.Initialize();
        XivCommon = new XivCommonBase();

        foreach (var t in Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(BaseTweak)) && !t.IsAbstract))
        {
            PluginLog.Debug("Initalizing Tweak: {0}", t.Name);
            try
            {
                var tweak = (BaseTweak)Activator.CreateInstance(t)!;
                Tweaks.Add(tweak);
                tweak.SetupInternal(this);

                if (tweak.CanLoad)
                    tweak.EnableInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed loading tweak '{t.Name}'.");
            }
        }

        Service.Framework.Update += OnFrameworkUpdate;
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

public sealed partial class HaselTweaks : IDisposable
{
    private bool isDisposed;

    ~HaselTweaks()
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
