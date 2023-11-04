using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselTweaks.Windows;

namespace HaselTweaks;

public partial class Plugin : IDalamudPlugin
{
    internal static HashSet<Tweak> Tweaks = null!;
    internal static Configuration Config = null!;

    private bool _disposed;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Service.Initialize(pluginInterface);
        Task.Run(Setup);
    }

    private void Setup()
    {
        InitializeResolver();

        Service.Framework.RunOnFrameworkThread(() =>
        {
            InitializeTweaks();

            Config = Configuration.Load(Tweaks.Select(t => t.InternalName));

            Service.TranslationManager.Initialize(Config);
            Service.TranslationManager.OnLanguageChange += OnLanguageChange;

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
                    Service.PluginLog.Error(ex, $"Failed enabling tweak '{tweak.InternalName}'.");
                }
            }

            Service.Framework.Update += OnFrameworkUpdate;
            Service.ClientState.Login += ClientState_Login;
            Service.ClientState.Logout += ClientState_Logout;
            Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            Service.AddonObserver.AddonOpen += AddonObserver_AddonOpen;
            Service.AddonObserver.AddonClose += AddonObserver_AddonClose;
            Service.AgentUpdateObserver.OnInventoryUpdate += OnInventoryUpdate;

            Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

            Service.CommandManager.AddHandler("/haseltweaks", new CommandInfo(OnCommand)
            {
                HelpMessage = "Show Window"
            });
        });
    }

    private static void InitializeResolver()
    {
        string gameVersion;
        unsafe { gameVersion = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GameVersion.Base; }
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

    private void OnLanguageChange()
    {
        Config.Save();

        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnLanguageChange();
        }
    }

    private void OnInventoryUpdate()
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnInventoryUpdate();
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (!tweak.Enabled)
                continue;

            tweak.OnFrameworkUpdateInternal();
        }
    }

    private void ClientState_Login()
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnLoginInternal();
        }
    }

    private void ClientState_Logout()
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnLogoutInternal();
        }
    }

    private void ClientState_TerritoryChanged(ushort id)
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

    private void OnOpenConfigUi()
    {
        Service.WindowManager.OpenWindow<PluginWindow>();
    }

    private void OnCommand(string command, string args)
    {
        Service.WindowManager.ToggleWindow<PluginWindow>();
    }

    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        Service.TranslationManager.OnLanguageChange -= OnLanguageChange;
        Service.AgentUpdateObserver.OnInventoryUpdate -= OnInventoryUpdate;
        Service.AddonObserver.AddonClose -= AddonObserver_AddonClose;
        Service.AddonObserver.AddonOpen -= AddonObserver_AddonOpen;
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.ClientState.Login -= ClientState_Login;
        Service.ClientState.Logout -= ClientState_Logout;
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

        Service.CommandManager.RemoveHandler("/haseltweaks");

        Service.WindowManager.Dispose();

        foreach (var tweak in Tweaks)
        {
            try
            {
                tweak.DisposeInternal();
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, $"Failed disposing tweak '{tweak.InternalName}'.");
            }
        }

        Config?.Save();

        Service.Dispose();

        Tweaks = null!;
        Config = null!;

        _disposed = true;
    }
}
