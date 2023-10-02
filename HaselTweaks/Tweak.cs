using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using HaselTweaks.Enums;

namespace HaselTweaks;

public abstract unsafe class Tweak
{
    private Type? _type;
    private string? _internalName;
    private TweakFlags? _flags;
    private IncompatibilityWarningAttribute[]? _incompatibilityWarnings;
    private bool _disposed;

    public Type CachedType
        => _type ??= GetType();

    public string InternalName
        => _internalName ??= CachedType.Name;

    public string Name
        => Service.TranslationManager.TryGetTranslation($"{InternalName}.Tweak.Name", out var text) ? text : InternalName;

    public string Description
        => Service.TranslationManager.TryGetTranslation($"{InternalName}.Tweak.Description", out var text) ? text : string.Empty;

    public TweakFlags Flags
        => _flags ??= CachedType.GetCustomAttribute<TweakAttribute>()?.Flags ?? TweakFlags.None;

    public IncompatibilityWarningAttribute[] IncompatibilityWarnings
        => _incompatibilityWarnings ??= CachedType.GetCustomAttributes<IncompatibilityWarningAttribute>().ToArray();

    public virtual void DrawCustomConfig() { }
    public virtual void OnConfigWindowClose() { }

    public virtual bool Outdated { get; protected set; }
    public virtual bool Ready { get; protected set; }
    public virtual bool Enabled { get; protected set; }
    public virtual Exception? LastException { get; protected set; }

    public Tweak()
    {
        try
        {
            Service.GameInteropProvider.InitializeFromAttributes(this);
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
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Enable (Hooks)");
            LastException = ex;
            return;
        }

        try
        {
            Enable();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Enable");
            LastException = ex;
            return;
        }

        LastException = null;
        Enabled = true;
    }

    internal void DisableInternal()
    {
        if (!Enabled) return;

        try
        {
            CallHooks("Disable");
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Disable (Hooks)");
            LastException = ex;
        }

        try
        {
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
        if (_disposed)
            return;

        DisableInternal();

        try
        {
            CallHooks("Dispose");
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Dispose (Hooks)");
            LastException = ex;
        }

        try
        {
            Dispose();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Dispose");
            LastException = ex;
        }

        Ready = false;
        _disposed = true;
    }

    internal void OnFrameworkUpdateInternal()
    {
        try
        {
            OnFrameworkUpdate();
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

    internal void OnAddonOpenInternal(string addonName)
    {
        try
        {
            OnAddonOpen(addonName);
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during OnAddonOpen");
            LastException = ex;
            return;
        }
    }

    internal void OnAddonCloseInternal(string addonName)
    {
        try
        {
            OnAddonClose(addonName);
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
    public virtual void OnConfigChange(string fieldName) { }
    public virtual void OnLanguageChange() { }
    public virtual void OnFrameworkUpdate() { }
    public virtual void OnLogin() { }
    public virtual void OnLogout() { }
    public virtual void OnTerritoryChanged(ushort id) { }
    public virtual void OnAddonOpen(string addonName) { }
    public virtual void OnAddonClose(string addonName) { }

    #region Logging methods

    protected void Log(string messageTemplate, params object[] values)
        => Information(messageTemplate, values);

    protected void Log(Exception exception, string messageTemplate, params object[] values)
        => Information(exception, messageTemplate, values);

    protected void Verbose(string messageTemplate, params object[] values)
        => Service.PluginLog.Verbose($"[{InternalName}] {messageTemplate}", values);

    protected void Verbose(Exception exception, string messageTemplate, params object[] values)
        => Service.PluginLog.Verbose(exception, $"[{InternalName}] {messageTemplate}", values);

    protected void Debug(string messageTemplate, params object[] values)
        => Service.PluginLog.Debug($"[{InternalName}] {messageTemplate}", values);

    protected void Debug(Exception exception, string messageTemplate, params object[] values)
        => Service.PluginLog.Debug(exception, $"[{InternalName}] {messageTemplate}", values);

    protected void Information(string messageTemplate, params object[] values)
        => Service.PluginLog.Information($"[{InternalName}] {messageTemplate}", values);

    protected void Information(Exception exception, string messageTemplate, params object[] values)
        => Service.PluginLog.Information(exception, $"[{InternalName}] {messageTemplate}", values);

    protected void Warning(string messageTemplate, params object[] values)
        => Service.PluginLog.Warning($"[{InternalName}] {messageTemplate}", values);

    protected void Warning(Exception exception, string messageTemplate, params object[] values)
        => Service.PluginLog.Warning(exception, $"[{InternalName}] {messageTemplate}", values);

    protected void Error(string messageTemplate, params object[] values)
        => Service.PluginLog.Error($"[{InternalName}] {messageTemplate}", values);

    protected void Error(Exception exception, string messageTemplate, params object[] values)
        => Service.PluginLog.Error(exception, $"[{InternalName}] {messageTemplate}", values);

    protected void Fatal(string messageTemplate, params object[] values)
        => Service.PluginLog.Fatal($"[{InternalName}] {messageTemplate}", values);

    protected void Fatal(Exception exception, string messageTemplate, params object[] values)
        => Service.PluginLog.Fatal(exception, $"[{InternalName}] {messageTemplate}", values);

    #endregion
}
