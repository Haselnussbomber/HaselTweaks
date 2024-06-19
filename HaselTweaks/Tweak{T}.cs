using System.Globalization;
using System.Linq;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Interfaces;
using ImGuiNET;

namespace HaselTweaks;

// TODO: refactor and remove this
public abstract class Tweak<T> : Tweak, IConfigurableTweak where T : notnull
{
    public Tweak(
        Configuration pluginConfig,
        TranslationManager translationManager)
        : base()
    {
        PluginConfig = pluginConfig;
        TranslationManager = translationManager;
        CachedConfigType = typeof(T);
        Config = (T?)(typeof(TweakConfigs)
            .GetProperties()?
            .FirstOrDefault(pi => pi!.PropertyType == typeof(T), null)?
            .GetValue(PluginConfig.Tweaks))
            ?? throw new InvalidOperationException($"Configuration for {typeof(T).Name} not found.");
    }

    public Type CachedConfigType { get; init; }
    public T Config { get; init; }
    public Configuration PluginConfig { get; }
    public TranslationManager TranslationManager { get; }

    public virtual void OnConfigOpen() { }
    public virtual void OnConfigChange(string fieldName) { }
    public virtual void OnConfigClose() { }

    public virtual void DrawConfig()
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
            var hasDependency = !string.IsNullOrEmpty(attr.DependsOn);
            var isDisabled = hasDependency && (bool?)CachedConfigType.GetField(attr.DependsOn)?.GetValue(Config) == false;

            using var id = ImRaii.PushId(field.Name);
            using var indent = ImGuiUtils.ConfigIndent(hasDependency);
            using var disabled = ImRaii.Disabled(isDisabled);

            switch (attr)
            {
                case EnumConfigAttribute enumConfigAttribute:
                    DrawEnumConfig(enumConfigAttribute, Config, field);
                    break;

                case BoolConfigAttribute boolConfigAttribute:
                    DrawBoolConfig(boolConfigAttribute, Config, field);
                    break;

                case FloatConfigAttribute floatConfigAttribute:
                    DrawFloatConfig(floatConfigAttribute, Config, field);
                    break;

                case StringConfigAttribute stringConfigAttribute:
                    DrawStringConfig(stringConfigAttribute, Config, field);
                    break;

                default:
                    break;
            }
        }
    }

    protected void DrawEnumConfig(EnumConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var enumType = fieldInfo.FieldType;

        string GetOptionLabel(int value)
            => t($"{InternalName}.Config.{fieldInfo.Name}.Options.{Enum.GetName(enumType, value)}.Label");

        if (!attr.NoLabel)
            ImGui.TextUnformatted(t($"{InternalName}.Config.{fieldInfo.Name}.Label"));

        using var indent = ImGuiUtils.ConfigIndent(!attr.NoLabel);

        var selectedValue = (int)(fieldInfo.GetValue(config) ?? 0);
        using var combo = ImRaii.Combo("##Input", GetOptionLabel(selectedValue));
        if (combo.Success)
        {
            foreach (var name in Enum.GetNames(enumType))
            {
                var value = (int)Enum.Parse(enumType, name);

                if (ImGui.Selectable(GetOptionLabel(value), selectedValue == value))
                {
                    fieldInfo.SetValue(config, value);
                    PluginConfig.Save();
                    OnConfigChange(fieldInfo.Name);
                }

                if (selectedValue == value)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
        }
        combo?.Dispose();

        if (TranslationManager.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }

    protected void DrawBoolConfig(BoolConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var value = (bool)fieldInfo.GetValue(config)!;

        if (ImGui.Checkbox(t($"{InternalName}.Config.{fieldInfo.Name}.Label") + "##Input", ref value))
        {
            fieldInfo.SetValue(config, value);
            PluginConfig.Save();
            OnConfigChange(fieldInfo.Name);
        }

        DrawConfigInfos(fieldInfo);

        if (TranslationManager.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            ImGuiUtils.PushCursorY(-3);
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
            ImGuiUtils.PushCursorY(3);
        }
    }

    protected void DrawFloatConfig(FloatConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var value = (float)fieldInfo.GetValue(config)!;

        ImGui.TextUnformatted(t($"{InternalName}.Config.{fieldInfo.Name}.Label"));

        using var indent = ImGuiUtils.ConfigIndent();

        if (ImGui.DragFloat("##Input", ref value, 0.01f, attr.Min, attr.Max, "%.2f"))
        {
            fieldInfo.SetValue(config, value);
            OnConfigChange(fieldInfo.Name);
        }

        if (DrawResetButton(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", attr.DefaultValue)))
        {
            fieldInfo.SetValue(config, attr.DefaultValue);
            PluginConfig.Save();
            OnConfigChange(fieldInfo.Name);
        }

        if (TranslationManager.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }

    protected void DrawStringConfig(StringConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var value = (string)fieldInfo.GetValue(config)!;

        ImGui.TextUnformatted(t($"{InternalName}.Config.{fieldInfo.Name}.Label"));

        if (ImGui.InputText("##Input", ref value, 50))
        {
            fieldInfo.SetValue(config, value);
            PluginConfig.Save();
            OnConfigChange(fieldInfo.Name);
        }

        if (DrawResetButton(attr.DefaultValue))
        {
            fieldInfo.SetValue(config, attr.DefaultValue);
            PluginConfig.Save();
            OnConfigChange(fieldInfo.Name);
        }

        if (TranslationManager.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }

    protected static void DrawConfigInfos(FieldInfo fieldInfo)
    {
        var attributes = fieldInfo.GetCustomAttributes<ConfigInfoAttribute>();
        if (!attributes.Any())
            return;

        foreach (var attribute in attributes)
        {
            ImGui.SameLine();
            ImGuiUtils.Icon(attribute.Icon, attribute.Color);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(t(attribute.Translationkey));
                ImGui.EndTooltip();
            }
        }
    }

    protected static bool DrawResetButton(string defaultValueString)
    {
        if (string.IsNullOrEmpty(defaultValueString))
            return false;

        ImGui.SameLine();
        return ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", defaultValueString));
    }
}
