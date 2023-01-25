using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using HaselTweaks.Utils;
using ImGuiNET;
using static Dalamud.Game.Command.CommandInfo;

namespace HaselTweaks;

public abstract class Tweak
{
    public string InternalName => GetType().Name;
    public abstract string Name { get; }

    public virtual string Description => string.Empty;
    public virtual bool HasDescription => !string.IsNullOrEmpty(Description);
    public virtual void DrawDescription() => ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorGrey2, Description);

    public virtual string IncompatibilityWarning => string.Empty;
    public virtual bool HasIncompatibilityWarning => !string.IsNullOrEmpty(IncompatibilityWarning);
    public virtual void DrawIncompatibilityWarning()
    {
        ImGuiUtils.DrawIcon(60073, 24, 24);
        ImGui.SameLine();
        ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorGrey2, IncompatibilityWarning);
    }

    public virtual bool HasCustomConfig => false;
    public virtual void DrawCustomConfig() { }

    public virtual bool Outdated { get; protected set; }
    public virtual bool Ready { get; protected set; }
    public virtual bool Enabled { get; protected set; }
    public virtual Exception? LastException { get; protected set; }

    protected IEnumerable<PropertyInfo> Hooks => GetType()
        .GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(prop =>
            prop.PropertyType.IsGenericType &&
            prop.PropertyType.GetGenericTypeDefinition() == typeof(Hook<>) &&
            prop.CustomAttributes.Any(ca => ca.AttributeType == typeof(AutoHookAttribute)) &&
            prop.CustomAttributes.Any(ca => ca.AttributeType == typeof(SignatureAttribute))
        );

    internal IEnumerable<MethodInfo> SlashCommands => GetType()
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(method => method.CustomAttributes.Any(ca => ca.AttributeType == typeof(SlashCommandAttribute)));

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

    internal void SetupInternal()
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
            Setup();
        }
        catch (Exception ex)
        {
            Error(ex, "Unexpected error during Setup");
            LastException = ex;
            return;
        }

        Ready = true;
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

        foreach (var methodInfo in SlashCommands)
        {
            var attr = (SlashCommandAttribute?)methodInfo.GetCustomAttribute(typeof(SlashCommandAttribute));
            if (attr == null || Delegate.CreateDelegate(typeof(HandlerDelegate), this, methodInfo, false) == null)
                continue;

            Service.Commands.AddHandler(attr.Command, new CommandInfo((string command, string argument) => // HandlerDelegate
            {
                methodInfo.Invoke(this, new string[] { command, argument });
            })
            {
                HelpMessage = attr.HelpMessage
            });
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

        foreach (var methodInfo in SlashCommands)
        {
            var attr = (SlashCommandAttribute?)methodInfo.GetCustomAttribute(typeof(SlashCommandAttribute));
            if (attr == null || Delegate.CreateDelegate(typeof(HandlerDelegate), this, methodInfo, false) == null)
                continue;

            Service.Commands.RemoveHandler(attr.Command);
        }

        Enabled = false;
    }

    internal void DisposeInternal()
    {
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

    public virtual void Setup() { }
    public virtual void Enable() { }
    public virtual void Disable() { }
    public virtual void Dispose() { }
    public virtual void OnFrameworkUpdate(Framework framework) { }

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
