using System.Globalization;
using System.Linq;
using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Interfaces;
using ImGuiNET;

namespace HaselTweaks;

// TODO: refactor and remove this
public abstract class Tweak<T> : Tweak, IConfigurableTweak where T : notnull
{
    public Tweak(
        PluginConfig pluginConfig,
        TextService textService)
        : base()
    {
        PluginConfig = pluginConfig;
        TextService = textService;

        CachedConfigType = typeof(T);
        Config = (T?)(typeof(TweakConfigs)
            .GetProperties()?
            .FirstOrDefault(pi => pi!.PropertyType == typeof(T), null)?
            .GetValue(PluginConfig.Tweaks))
            ?? throw new InvalidOperationException($"Configuration for {typeof(T).Name} not found.");
    }

    public Type CachedConfigType { get; init; }
    public T Config { get; init; }
    public PluginConfig PluginConfig { get; }
    public TextService TextService { get; }

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

        TextService.Draw("HaselTweaks.Config.SectionTitle.Configuration");

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
            }
        }
    }

    protected void DrawEnumConfig(EnumConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var enumType = fieldInfo.FieldType;

        string GetOptionLabel(int value)
            => TextService.Translate($"{InternalName}.Config.{fieldInfo.Name}.Options.{Enum.GetName(enumType, value)}.Label");

        if (!attr.NoLabel)
            TextService.Draw($"{InternalName}.Config.{fieldInfo.Name}.Label");

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

        if (TextService.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
    }

    protected void DrawBoolConfig(BoolConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var value = (bool)fieldInfo.GetValue(config)!;

        if (ImGui.Checkbox(TextService.Translate($"{InternalName}.Config.{fieldInfo.Name}.Label") + "##Input", ref value))
        {
            fieldInfo.SetValue(config, value);
            PluginConfig.Save();
            OnConfigChange(fieldInfo.Name);
        }

        DrawConfigInfos(fieldInfo);

        if (TextService.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
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

        TextService.Draw($"{InternalName}.Config.{fieldInfo.Name}.Label");

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

        if (TextService.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }

    protected void DrawStringConfig(StringConfigAttribute attr, object config, FieldInfo fieldInfo)
    {
        var value = (string)fieldInfo.GetValue(config)!;

        TextService.Draw($"{InternalName}.Config.{fieldInfo.Name}.Label");

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

        if (TextService.TryGetTranslation($"{InternalName}.Config.{fieldInfo.Name}.Description", out var description))
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
    }

    protected void DrawConfigInfos(FieldInfo fieldInfo)
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
                TextService.Draw(attribute.Translationkey);
                ImGui.EndTooltip();
            }
        }
    }

    protected bool DrawResetButton(string defaultValueString)
    {
        if (string.IsNullOrEmpty(defaultValueString))
            return false;

        ImGui.SameLine();
        return ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, TextService.Translate("HaselTweaks.Config.ResetToDefault", defaultValueString));
    }
}
