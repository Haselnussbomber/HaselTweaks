using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Utils;

namespace HaselTweaks;

public abstract unsafe class Tweak
{
    private Type? _type = null;
    private string? _internalName = null;
    private string? _name = null;
    private string? _description = null;
    private bool? _hasCustomConfig = null;
    private IncompatibilityWarning[]? _incompatibilityWarnings = null;

    public Type CachedType
        => _type ??= GetType();

    public string InternalName
        => _internalName ??= CachedType.Name;

    public string Name
        => _name ??= CachedType.GetCustomAttribute<TweakAttribute>()?.Name ?? "";

    public string Description
        => _description ??= CachedType.GetCustomAttribute<TweakAttribute>()?.Description ?? "";

    public bool HasCustomConfig
        => _hasCustomConfig ??= CachedType.GetCustomAttribute<TweakAttribute>()?.HasCustomConfig ?? false;

    public IncompatibilityWarning[] IncompatibilityWarnings
        => _incompatibilityWarnings ??= CachedType.GetCustomAttributes<IncompatibilityWarning>().ToArray();

    public virtual void DrawCustomConfig(TextureManager textureManager) { }
    public virtual void OnConfigWindowClose() { }

    public virtual bool Outdated { get; protected set; }
    public virtual bool Ready { get; protected set; }
    public virtual bool Enabled { get; protected set; }
    public virtual Exception? LastException { get; protected set; }

    public Tweak()
    {
        try
        {
            SignatureHelper.Initialise(this);
        }
        catch (SignatureException ex)
        {
            Error(ex, "SignatureException, flagging as outdated");
            Outdated = true;
            LastException = ex;
            return;
        }

        try
        {
            SetupVTableHooks(); // before SetupAddressHooks!
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during SetupVTableHooks");
            LastException = ex;
            return;
        }

        try
        {
            SetupAddressHooks();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during SetupAddressHooks");
            LastException = ex;
            return;
        }

        Ready = true;
    }

    protected IEnumerable<PropertyInfo> Hooks => CachedType
        .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(prop =>
            prop.PropertyType.IsGenericType &&
            prop.PropertyType.GetGenericTypeDefinition() == typeof(Hook<>)
        );

    protected void CallHooks(string methodName)
    {
        foreach (var property in Hooks)
        {
            var hook = property.GetValue(this);
            if (hook == null) continue;

            typeof(Hook<>)
                .MakeGenericType(property.PropertyType.GetGenericArguments().First())
                .GetMethod(methodName)?
                .Invoke(hook, null);
        }
    }

    internal void EnableInternal()
    {
        if (!Ready || Outdated) return;

        try
        {
            CallHooks("Enable");
            Enable();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Enable");
            LastException = ex;
        }

        Enabled = true;
    }

    internal void DisableInternal()
    {
        if (!Enabled) return;

        try
        {
            CallHooks("Disable");
            Disable();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Disable");
            LastException = ex;
        }

        Enabled = false;
    }

    internal void DisposeInternal()
    {
        DisableInternal();

        try
        {
            CallHooks("Dispose");
            Dispose();
        }
        catch (Exception ex)
        {
            Error(ex, "Unable to disable");
            LastException = ex;
        }

        Ready = false;
    }

    internal void OnFrameworkUpdateInternal(Framework framework)
    {
        try
        {
            OnFrameworkUpdate(framework);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnFrameworkUpdate");
            LastException = ex;
            return;
        }
    }

    internal void OnLoginInternal()
    {
        try
        {
            OnLogin();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnLogin");
            LastException = ex;
            return;
        }
    }

    internal void OnLogoutInternal()
    {
        try
        {
            OnLogout();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnLogout");
            LastException = ex;
            return;
        }
    }

    internal void OnTerritoryChangedInternal(ushort id)
    {
        try
        {
            OnTerritoryChanged(id);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnTerritoryChanged");
            LastException = ex;
            return;
        }
    }

    internal void OnAddonOpenInternal(string addonName, AtkUnitBase* unitbase)
    {
        try
        {
            OnAddonOpen(addonName, unitbase);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnAddonOpen");
            LastException = ex;
            return;
        }
    }

    internal void OnAddonCloseInternal(string addonName, AtkUnitBase* unitbase)
    {
        try
        {
            OnAddonClose(addonName, unitbase);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnAddonOpen");
            LastException = ex;
            return;
        }
    }

    public virtual void SetupAddressHooks() { }
    public virtual void SetupVTableHooks() { }
    public virtual void Enable() { }
    public virtual void Disable() { }
    public virtual void Dispose() { }
    public virtual void OnFrameworkUpdate(Framework framework) { }
    public virtual void OnLogin() { }
    public virtual void OnLogout() { }
    public virtual void OnTerritoryChanged(ushort id) { }
    public virtual void OnAddonOpen(string addonName, AtkUnitBase* unitbase) { }
    public virtual void OnAddonClose(string addonName, AtkUnitBase* unitbase) { }

    #region Logging methods

    protected void Log(string messageTemplate, params object[] values)
        => Information(messageTemplate, values);

    protected void Log(Exception exception, string messageTemplate, params object[] values)
        => Information(exception, messageTemplate, values);

    protected void Verbose(string messageTemplate, params object[] values)
        => PluginLog.Verbose($"[{Name}] {messageTemplate}", values);

    protected void Verbose(Exception exception, string messageTemplate, params object[] values)
        => PluginLog.Verbose(exception, $"[{Name}] {messageTemplate}", values);

    protected void Debug(string messageTemplate, params object[] values)
        => PluginLog.Debug($"[{Name}] {messageTemplate}", values);

    protected void Debug(Exception exception, string messageTemplate, params object[] values)
        => PluginLog.Debug(exception, $"[{Name}] {messageTemplate}", values);

    protected void Information(string messageTemplate, params object[] values)
        => PluginLog.Information($"[{Name}] {messageTemplate}", values);

    protected void Information(Exception exception, string messageTemplate, params object[] values)
        => PluginLog.Information(exception, $"[{Name}] {messageTemplate}", values);

    protected void Warning(string messageTemplate, params object[] values)
        => PluginLog.Warning($"[{Name}] {messageTemplate}", values);

    protected void Warning(Exception exception, string messageTemplate, params object[] values)
        => PluginLog.Warning(exception, $"[{Name}] {messageTemplate}", values);

    protected void Error(string messageTemplate, params object[] values)
        => PluginLog.Error($"[{Name}] {messageTemplate}", values);

    protected void Error(Exception exception, string messageTemplate, params object[] values)
        => PluginLog.Error(exception, $"[{Name}] {messageTemplate}", values);

    protected void Fatal(string messageTemplate, params object[] values)
        => PluginLog.Fatal($"[{Name}] {messageTemplate}", values);

    protected void Fatal(Exception exception, string messageTemplate, params object[] values)
        => PluginLog.Fatal(exception, $"[{Name}] {messageTemplate}", values);

    #endregion
}
