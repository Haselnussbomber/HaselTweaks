using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Game.Command;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Utils;
using static Dalamud.Game.Command.CommandInfo;

namespace HaselTweaks;

public abstract class Tweak<T> : Tweak
{
    public Tweak() : base()
    {
        CachedConfigType = typeof(T);
        Config = (T?)(typeof(TweakConfigs)
            .GetProperties()?
            .FirstOrDefault(pi => pi!.PropertyType == typeof(T), null)?
            .GetValue(Plugin.Config.Tweaks))
            ?? throw new InvalidOperationException($"Configuration for {typeof(T).Name} not found.");
    }

    public Type CachedConfigType { get; init; }
    public T Config { get; init; }

    protected IEnumerable<MethodInfo> CommandHandlers
        => CachedType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(mi => mi.GetCustomAttribute<CommandHandlerAttribute>() != null);

    public override void DrawConfig()
    {
        var configFields = CachedConfigType.GetFields()
            .Select(fieldInfo => (FieldInfo: fieldInfo, Attribute: fieldInfo.GetCustomAttribute<BaseConfigAttribute>()))
            .Where((tuple) => tuple.Attribute != null)
            .Cast<(FieldInfo, BaseConfigAttribute)>();

        if (!configFields.Any())
            return;

        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        foreach (var (field, attr) in configFields)
        {
            using var fieldid = ImRaii.PushId(field.Name);

            var hasDependency = !string.IsNullOrEmpty(attr.DependsOn);
            var isDisabled = hasDependency && (bool?)CachedConfigType.GetField(attr.DependsOn)?.GetValue(Config) == false;
            var indent = hasDependency ? ImGuiUtils.ConfigIndent() : null;
            var disabled = isDisabled ? ImRaii.Disabled() : null;

            attr.Draw(this, Config!, field);

            disabled?.Dispose();
            indent?.Dispose();
        }
    }

    protected override void EnableCommands()
    {
        foreach (var methodInfo in CommandHandlers)
        {
            var attr = methodInfo.GetCustomAttribute<CommandHandlerAttribute>()!;
            var enabled = string.IsNullOrEmpty(attr.ConfigFieldName);

            if (!string.IsNullOrEmpty(attr.ConfigFieldName))
            {
                enabled |= (typeof(T).GetField(attr.ConfigFieldName)?.GetValue(Config) as bool?)
                    ?? throw new InvalidOperationException($"Configuration field {attr.ConfigFieldName} in {typeof(T).Name} not found.");
            }

            if (enabled)
            {
                EnableCommand(attr.Command, attr.HelpMessage, methodInfo);
            }
        }
    }

    protected override void DisableCommands()
    {
        foreach (var methodInfo in CommandHandlers)
        {
            var attr = methodInfo.GetCustomAttribute<CommandHandlerAttribute>()!;
            var enabled = string.IsNullOrEmpty(attr.ConfigFieldName);

            if (!string.IsNullOrEmpty(attr.ConfigFieldName))
            {
                enabled |= (typeof(T).GetField(attr.ConfigFieldName)?.GetValue(Config) as bool?)
                    ?? throw new InvalidOperationException($"Configuration field {attr.ConfigFieldName} in {typeof(T).Name} not found.");
            }

            if (enabled)
            {
                DisableCommand(attr.Command);
            }
        }
    }

    internal override void OnConfigChangeInternal(string fieldName)
    {
        foreach (var methodInfo in CommandHandlers)
        {
            var attr = methodInfo.GetCustomAttribute<CommandHandlerAttribute>()!;
            if (attr.ConfigFieldName != fieldName)
                continue;

            var enabled = string.IsNullOrEmpty(attr.ConfigFieldName);

            if (!string.IsNullOrEmpty(attr.ConfigFieldName))
            {
                enabled |= (typeof(T).GetField(attr.ConfigFieldName)?.GetValue(Config) as bool?)
                    ?? throw new InvalidOperationException($"Configuration field {attr.ConfigFieldName} in {typeof(T).Name} not found.");
            }

            if (enabled)
            {
                EnableCommand(attr.Command, attr.HelpMessage, methodInfo);
            }
            else
            {
                DisableCommand(attr.Command);
            }
        }

        base.OnConfigChange(fieldName);
    }

    private void EnableCommand(string command, string helpMessage, MethodInfo methodInfo)
    {
        var handler = methodInfo.CreateDelegate<HandlerDelegate>(this);

        if (Service.CommandManager.AddHandler(command, new CommandInfo(handler) { HelpMessage = helpMessage }))
        {
            Log($"CommandHandler {command} added");
        }
        else
        {
            Warning($"CommandHandler {command} not added");
        }
    }

    private void DisableCommand(string command)
    {
        if (Service.CommandManager.RemoveHandler(command))
        {
            Log($"CommandHandler {command} removed");
        }
        else
        {
            Warning($"CommandHandler {command} not removed");
        }
    }
}
