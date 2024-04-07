using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselTweaks.Windows;

namespace HaselTweaks;

public sealed class Plugin : IDalamudPlugin
{
    public readonly HashSet<Tweak> Tweaks = [];
    private readonly CommandInfo CommandInfo;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Service.Initialize(pluginInterface);
        Service.AddService(Configuration.Load());

        Service.AddonObserver.AddonClose += AddonObserver_AddonClose;
        Service.AddonObserver.AddonOpen += AddonObserver_AddonOpen;
        Service.ClientState.Login += ClientState_Login;
        Service.ClientState.Logout += ClientState_Logout;
        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        Service.Framework.Update += Framework_Update;
        Service.GameInventory.InventoryChangedRaw += GameInventory_InventoryChangedRaw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OnOpenConfigUi;
        Service.TranslationManager.LanguageChanged += TranslationManager_LanguageChanged;

        CommandInfo = new CommandInfo(OnCommand) { HelpMessage = t("HaselTweaks.CommandHandlerHelpMessage") };

        Service.CommandManager.AddHandler("/haseltweaks", CommandInfo);

        Service.Framework.RunOnFrameworkThread(InitializeTweaks);
    }

    private void InitializeTweaks()
    {
        var config = Service.GetService<Configuration>();

        foreach (var tweakType in GetType().Assembly.GetTypes()
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

        foreach (var tweak in Tweaks)
        {
            if (!config.EnabledTweaks.Contains(tweak.InternalName))
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
    }

    private void AddonObserver_AddonOpen(string addonName)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnAddonOpenInternal(addonName);
        }
    }

    private void AddonObserver_AddonClose(string addonName)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnAddonCloseInternal(addonName);
        }
    }

    private void ClientState_Login()
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnLoginInternal();
        }
    }

    private void ClientState_Logout()
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnLogoutInternal();
        }
    }

    private void ClientState_TerritoryChanged(ushort id)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnTerritoryChangedInternal(id);
        }
    }

    private void Framework_Update(IFramework framework)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnFrameworkUpdateInternal();
        }
    }

    private void GameInventory_InventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnInventoryUpdateInternal();
        }
    }

    private void UiBuilder_OnOpenConfigUi()
    {
        Service.WindowManager.OpenWindow<PluginWindow>().Plugin = this;
    }

    private void TranslationManager_LanguageChanged(string langCode)
    {
        CommandInfo.HelpMessage = t("HaselTweaks.CommandHandlerHelpMessage");

        foreach (var tweak in Tweaks)
        {
            if (tweak.Enabled)
                tweak.OnLanguageChangeInternal();
        }
    }

    private void OnCommand(string command, string arguments)
    {
        Service.WindowManager.OpenWindow<PluginWindow>().Plugin = this;
    }

    void IDisposable.Dispose()
    {
        Service.AddonObserver.AddonClose -= AddonObserver_AddonClose;
        Service.AddonObserver.AddonOpen -= AddonObserver_AddonOpen;
        Service.ClientState.Login -= ClientState_Login;
        Service.ClientState.Logout -= ClientState_Logout;
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        Service.Framework.Update -= Framework_Update;
        Service.GameInventory.InventoryChangedRaw -= GameInventory_InventoryChangedRaw;
        Service.PluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OnOpenConfigUi;
        Service.TranslationManager.LanguageChanged -= TranslationManager_LanguageChanged;

        Service.CommandManager.RemoveHandler("/haseltweaks");

        if (Service.HasService<WindowManager>())
            Service.WindowManager.Dispose();

        foreach (var tweak in Tweaks)
        {
            try
            {
                Service.PluginLog.Debug($"Disposing {tweak.InternalName}");
                tweak.DisposeInternal();
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex, $"Failed disposing tweak '{tweak.InternalName}'.");
            }
        }

        Service.PluginLog.Debug("Disposing Service");
        Service.Dispose();
    }
}
