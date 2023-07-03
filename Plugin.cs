using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Caches;
using HaselTweaks.Windows;
using DalamudFramework = Dalamud.Game.Framework;

namespace HaselTweaks;

public sealed unsafe partial class Plugin : IDalamudPlugin
{
    public string Name => "HaselTweaks";

    internal static WindowSystem WindowSystem = new("HaselTweaks");
    internal static List<Tweak> Tweaks = new();
    internal static Configuration Config = null!;

    private PluginWindow? _pluginWindow;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Task.Run(Setup);
    }

    private unsafe void Setup()
    {
        SignatureHelper.Initialise(this);
        AddonSetupHook?.Enable();
        AddonFinalizeHook?.Enable();

        var gameVersion = Framework.Instance()->GameVersion.Base;
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

        _pluginWindow = new PluginWindow();
        WindowSystem.AddWindow(_pluginWindow);

        Service.PluginInterface.UiBuilder.Draw += OnDraw;
        Service.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

        Service.Commands.RemoveHandler("/haseltweaks");
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
        Service.ClientState.Login += ClientState_Login;
        Service.ClientState.Logout += ClientState_Logout;
        Service.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
    }

    private void OnFrameworkUpdate(DalamudFramework framework)
    {
        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
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
        Service.Framework.Update -= OnFrameworkUpdate;
        Service.ClientState.Login -= ClientState_Login;
        Service.ClientState.Logout -= ClientState_Logout;
        Service.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
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

        _pluginWindow?.Dispose();
        _pluginWindow = null;

        Tweaks = null!;

        AddonSetupHook?.Dispose();
        AddonFinalizeHook?.Dispose();

        Config.Save();
        Config = null!;

        StringCache.Dispose();
    }

    [SigHook("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 14")]
    public void AddonSetup(AtkUnitBase* unitBase)
    {
        AddonSetupHook.OriginalDisposeSafe(unitBase);

        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnAddonOpenInternal(GetAddonName(unitBase), unitBase);
        }
    }

    [SigHook("E8 ?? ?? ?? ?? 48 8B 7C 24 ?? 41 8B C6")]
    public void AddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** unitBasePtr)
    {
        var unitBase = *unitBasePtr;

        foreach (var tweak in Tweaks.Where(tweak => tweak.Enabled))
        {
            tweak.OnAddonCloseInternal(GetAddonName(unitBase), unitBase);
        }

        AddonFinalizeHook.OriginalDisposeSafe(unitManager, unitBasePtr);
    }
}
