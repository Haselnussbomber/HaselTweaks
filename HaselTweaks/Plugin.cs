using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Windows;

namespace HaselTweaks;

public partial class Plugin : IDalamudPlugin
{
    internal static HashSet<Tweak> Tweaks = [];
    internal static Configuration Config = null!;

    private bool _disposed;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Service.Initialize(pluginInterface);
        Task.Run(HaselCommon.Interop.Resolver.GetInstance.Resolve)
            .ContinueOnFrameworkThreadWith(Setup);
    }

    private void Setup()
    {
        Config = Configuration.Load();

        foreach (var tweakType in typeof(Plugin).Assembly.GetTypes()
            .Where(type => type.Namespace == "HaselTweaks.Tweaks" && type.GetCustomAttribute<TweakAttribute>() != null))
        {
            try
            {
                Service.PluginLog.Verbose($"Initializing {tweakType.Name}");
                Tweaks.Add((Tweak)Activator.CreateInstance(tweakType)!);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, $"[{tweakType.Name}] Error during initialization");
            }
        }

        Service.TranslationManager.Initialize(Config);

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

        Service.AddonObserver.AddonClose += AddonObserver_AddonClose;
        Service.AddonObserver.AddonOpen += AddonObserver_AddonOpen;
        Service.ClientState.Login += ClientState_Login;
        Service.ClientState.Logout += ClientState_Logout;
        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Service.Framework.Update += Framework_Update;
        Service.GameInventory.InventoryChangedRaw += GameInventory_InventoryChangedRaw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
        Service.TranslationManager.OnLanguageChange += TranslationManager_OnLanguageChange;

        Service.CommandManager.AddHandler("/haseltweaks", new CommandInfo(OnCommand)
        {
            HelpMessage = t("HaselTweaks.CommandHandlerHelpMessage")
        });
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

    private void Framework_Update(IFramework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (!tweak.Enabled)
                continue;

            tweak.OnFrameworkUpdateInternal();
        }
    }

    private void GameInventory_InventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnInventoryUpdate();
        }
    }

    private void UiBuilder_OnOpenConfigUi()
    {
        Service.WindowManager.OpenWindow<PluginWindow>();
    }

    private void TranslationManager_OnLanguageChange()
    {
        Config.Save();

        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnLanguageChange();
        }
    }

    private void OnCommand(string command, string args)
    {
        Service.WindowManager.ToggleWindow<PluginWindow>();
    }

    void IDisposable.Dispose()
    {
        if (_disposed)
            return;

        Service.AddonObserver.AddonClose -= AddonObserver_AddonClose;
        Service.AddonObserver.AddonOpen -= AddonObserver_AddonOpen;
        Service.ClientState.Login -= ClientState_Login;
        Service.ClientState.Logout -= ClientState_Logout;
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Service.Framework.Update -= Framework_Update;
        Service.GameInventory.InventoryChangedRaw -= GameInventory_InventoryChangedRaw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
        Service.TranslationManager.OnLanguageChange -= TranslationManager_OnLanguageChange;

        Service.CommandManager.RemoveHandler("/haseltweaks");

        if (Service.HasService<WindowManager>())
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
