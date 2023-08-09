using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using HaselTweaks.Windows;
using DalamudFramework = Dalamud.Game.Framework;

namespace HaselTweaks;

public partial class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal static WindowSystem WindowSystem = new("HaselTweaks");
    internal static HashSet<Tweak> Tweaks = null!;
    internal static Configuration Config = null!;

    private PluginWindow? _pluginWindow;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Service.PluginInterface = pluginInterface;
        Service.Initialize();
        Task.Run(Setup);
    }

    private void Setup()
    {
        InitializeResolver();

        Service.Framework.RunOnFrameworkThread(() =>
        {
            InitializeTweaks();

            Config = Configuration.Load(Tweaks.Select(t => t.InternalName));

            foreach (var tweak in Tweaks)
            {
                if (!Config.EnabledTweaks.Contains(tweak.InternalName))
                    continue;

                try
                {
                    tweak.EnableInternal();
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Failed enabling tweak '{tweak.InternalName}'.");
                }
            }

            Service.Framework.Update += OnFrameworkUpdate;
            Service.ClientState.Login += ClientState_Login;
            Service.ClientState.Logout += ClientState_Logout;
            Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            Service.AddonObserver.AddonOpen += AddonObserver_AddonOpen;
            Service.AddonObserver.AddonClose += AddonObserver_AddonClose;

            Service.PluginInterface.UiBuilder.Draw += OnDraw;
            Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

            Service.CommandManager.RemoveHandler("/haseltweaks");
            Service.CommandManager.AddHandler("/haseltweaks", new CommandInfo(OnCommand)
            {
                HelpMessage = "Show Window"
            });

            WindowSystem.AddWindow(_pluginWindow = new PluginWindow());
        });
    }

    private static void InitializeResolver()
    {
        string gameVersion;
        unsafe { gameVersion = Framework.Instance()->GameVersion.Base; }
        if (string.IsNullOrEmpty(gameVersion))
            throw new Exception("Unable to read game version.");

        var currentSigCacheName = $"SigCache_{gameVersion}.json";

        // delete old sig caches
        foreach (var file in Service.PluginInterface.ConfigDirectory.EnumerateFiles()
            .Where(fi => fi.Name.StartsWith("SigCache_") && fi.Name != currentSigCacheName))
        {
            try { file.Delete(); }
            catch { }
        }

        Interop.Resolver.GetInstance.SetupSearchSpace(
            Service.SigScanner.SearchBase,
            new FileInfo(Path.Join(Service.PluginInterface.ConfigDirectory.FullName, currentSigCacheName)));

        Interop.Resolver.GetInstance.Resolve();
    }

    private void OnFrameworkUpdate(DalamudFramework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (!tweak.Enabled)
                continue;

            tweak.OnFrameworkUpdateInternal(framework);
        }
    }

    private void ClientState_Login(object? sender, EventArgs e)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnLoginInternal();
        }
    }

    private void ClientState_Logout(object? sender, EventArgs e)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnLogoutInternal();
        }
    }

    private void ClientState_TerritoryChanged(object? sender, ushort id)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnTerritoryChangedInternal(id);
        }
    }

    private void AddonObserver_AddonOpen(string addonName)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnAddonOpenInternal(addonName);
        }
    }

    private void AddonObserver_AddonClose(string addonName)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnAddonCloseInternal(addonName);
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
        _pluginWindow!.Toggle();
    }

    private void OnCommand(string command, string args)
    {
        _pluginWindow!.Toggle();
    }

    void IDisposable.Dispose()
    {
        Service.AddonObserver.AddonClose -= AddonObserver_AddonClose;
        Service.AddonObserver.AddonOpen -= AddonObserver_AddonOpen;
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.ClientState.Login -= ClientState_Login;
        Service.ClientState.Logout -= ClientState_Logout;
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Service.PluginInterface.UiBuilder.Draw -= OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

        Service.CommandManager.RemoveHandler("/haseltweaks");

        foreach (var tweak in Tweaks)
        {
            try
            {
                tweak.DisposeInternal();
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed disposing tweak '{tweak.Name}'.");
            }
        }

        Tweaks = null!;

        WindowSystem.RemoveAllWindows();
        WindowSystem = null!;

        _pluginWindow?.Dispose();
        _pluginWindow = null;

        Config?.Save();
        Config = null!;

        Service.Dispose();
    }
}
