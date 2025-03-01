using System.Globalization;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselTweaks.Config;

[RegisterSingleton, AutoConstruct]
public partial class ConfigGui
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly TextureService _textureService;

    private IConfigurableTweak? _tweak = null;

    public ImRaii.IEndObject PushContext(IConfigurableTweak tweak)
    {
        _tweak = tweak;

        var disabled = ImRaii.Disabled(tweak.Status == TweakStatus.Outdated);

        return new EndUnconditionally(() =>
        {
            _tweak = null;
            disabled.Dispose();
        }, true);
    }

    public void DrawConfigurationHeader(string labelKey = "HaselTweaks.Config.SectionTitle.Configuration")
    {
        ImGuiUtils.DrawSection(_textService.Translate(labelKey));
    }

    public bool DrawBool(string fieldName, ref bool value, bool noFixSpaceAfter = false, Action? drawAfterLabel = null, Action? drawAfterDescription = null)
    {
        if (_tweak == null)
            return false;

        using var id = ImRaii.PushId(fieldName);

        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var result = false;

        using (var table = ImRaii.Table(fieldName, 2, ImGuiTableFlags.NoSavedSettings))
        {
            if (table)
            {
                ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed, ImGui.GetFrameHeight());
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                result = ImGui.Checkbox("##Input", ref value);

                ImGui.TableNextColumn();

                ImGui.TextUnformatted(_textService.Translate($"{_tweak.InternalName}.Config.{fieldName}.Label"));

                if (ImGui.IsItemClicked())
                {
                    value = !value;
                    result = true;
                }

                drawAfterLabel?.Invoke();

                if (_textService.TryGetTranslation($"{_tweak.InternalName}.Config.{fieldName}.Description", out var description))
                {
                    ImGuiHelpers.SeStringWrapped(ReadOnlySeString.FromText(description), new SeStringDrawParams() { Color = Color.Grey });
                }

                drawAfterDescription?.Invoke();
            }
        }

        if (!noFixSpaceAfter)
            ImGuiUtils.PushCursorY(-itemSpacing.Y / 2);

        if (result)
        {
            _pluginConfig.Save();
            _tweak.OnConfigChange(fieldName);
        }

        return result;
    }

    public bool DrawEnum<T>(string fieldName, ref T value, bool noLabel = false) where T : Enum
    {
        if (_tweak == null)
            return false;

        using var id = ImRaii.PushId(fieldName);

        var enumType = typeof(T);
        var result = false;

        string GetOptionLabel(int value)
            => _textService.Translate($"{_tweak.InternalName}.Config.{fieldName}.Options.{Enum.GetName(enumType, value)}.Label");

        if (!noLabel)
            ImGui.TextUnformatted(_textService.Translate($"{_tweak.InternalName}.Config.{fieldName}.Label"));

        using var indent = ImGuiUtils.ConfigIndent(!noLabel);

        var selectedValue = (int)(object)value;

        using (var combo = ImRaii.Combo("##Input", GetOptionLabel(selectedValue)))
        {
            if (combo)
            {
                foreach (var optionName in Enum.GetNames(enumType))
                {
                    var optionValue = (int)Enum.Parse(enumType, optionName);

                    var isSelected = selectedValue == optionValue;

                    if (ImGui.Selectable(GetOptionLabel(optionValue), isSelected))
                    {
                        result = true;
                        value = (T)(object)optionValue;
                        _pluginConfig.Save();
                        _tweak.OnConfigChange(fieldName);
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
            }
        }

        if (_textService.TryGetTranslation($"{_tweak.InternalName}.Config.{fieldName}.Description", out var description))
            ImGuiHelpers.SafeTextColoredWrapped(Color.Grey, description);

        return result;
    }

    public bool DrawString(string fieldName, ref string value, string defaultValue = "")
    {
        if (_tweak == null)
            return false;

        using var id = ImRaii.PushId(fieldName);

        ImGui.TextUnformatted(_textService.Translate($"{_tweak.InternalName}.Config.{fieldName}.Label"));

        using var indent = ImGuiUtils.ConfigIndent();

        var result = ImGui.InputText("##Input", ref value, 50);
        if (result)
        {
            _pluginConfig.Save();
            _tweak.OnConfigChange(fieldName);
        }

        if (DrawResetButton(defaultValue))
        {
            value = defaultValue;
            _pluginConfig.Save();
            _tweak.OnConfigChange(fieldName);
        }

        if (_textService.TryGetTranslation($"{_tweak.InternalName}.Config.{fieldName}.Description", out var description))
            ImGuiHelpers.SafeTextColoredWrapped(Color.Grey, description);

        return result;
    }

    public bool DrawFloat(string fieldName, ref float value, float defaultValue = 0, float min = 0, float max = 100)
    {
        if (_tweak == null)
            return false;

        using var id = ImRaii.PushId(fieldName);

        ImGui.TextUnformatted(_textService.Translate($"{_tweak.InternalName}.Config.{fieldName}.Label"));

        using var indent = ImGuiUtils.ConfigIndent();

        var result = ImGui.DragFloat("##Input", ref value, 0.01f, min, max, "%.2f");
        if (result)
        {
            _tweak.OnConfigChange(fieldName);
        }

        if (DrawResetButton(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", defaultValue)))
        {
            value = (float)defaultValue;
            _pluginConfig.Save();
            _tweak.OnConfigChange(fieldName);
        }

        if (_textService.TryGetTranslation($"{_tweak.InternalName}.Config.{fieldName}.Description", out var description))
        {
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Color.Grey, description);
        }

        return result;
    }

    public bool DrawResetButton(string defaultValueString)
    {
        if (string.IsNullOrEmpty(defaultValueString))
            return false;

        ImGui.SameLine();
        return ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, _textService.Translate("HaselTweaks.Config.ResetToDefault", defaultValueString));
    }

    public void DrawIncompatibilityWarnings((string InternalName, string[] ConfigNames)[] incompatibilityWarnings)
    {
        var warnings = incompatibilityWarnings
            .Select(entry => (entry, IsLoaded: _pluginInterface.InstalledPlugins.Any(p => p.IsLoaded && p.InternalName == p.InternalName)))
            .ToArray();

        if (!warnings.Any(tuple => tuple.IsLoaded))
            return;

        DrawConfigurationHeader("HaselTweaks.Config.SectionTitle.IncompatibilityWarning");

        _textureService.DrawIcon(60073, 24);
        ImGui.SameLine();
        var cursorPosX = ImGui.GetCursorPosX();

        string getConfigName(string tweakName, string configName)
            => _textService.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{tweakName}.Config.{configName}");

        if (warnings.Length == 1)
        {
            var (entry, isLoaded) = warnings[0];
            var pluginName = _textService.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Name");

            if (isLoaded)
            {
                if (entry.ConfigNames.Length == 0)
                {
                    ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Single.Plugin", pluginName));
                }
                else if (entry.ConfigNames.Length == 1)
                {
                    var configName = getConfigName(entry.InternalName, entry.ConfigNames[0]);
                    ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Single.PluginSetting", configName, pluginName));
                }
                else if (entry.ConfigNames.Length > 1)
                {
                    var configNames = entry.ConfigNames.Select((configName) => _textService.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{configName}"));
                    ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Single.PluginSettings", pluginName) + $"\n- {string.Join("\n- ", configNames)}");
                }
            }
        }
        else if (warnings.Length > 1)
        {
            ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.Preface"));

            foreach (var (entry, isLoaded) in warnings.Where(tuple => tuple.IsLoaded))
            {
                var pluginName = _textService.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Name");

                ImGui.SetCursorPosX(cursorPosX);

                if (entry.ConfigNames.Length == 0)
                {
                    ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.Plugin", pluginName));
                }
                else if (entry.ConfigNames.Length == 1)
                {
                    var configName = _textService.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{entry.ConfigNames[0]}");
                    ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.PluginSetting", configName, pluginName));
                }
                else if (entry.ConfigNames.Length > 1)
                {
                    var configNames = entry.ConfigNames.Select((configName) => _textService.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{configName}"));
                    ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, _textService.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.PluginSettings", pluginName) + $"\n    - {string.Join("\n    - ", configNames)}");
                }
            }
        }
    }

    public void DrawNetworkWarning()
    {
        ImGui.SameLine();
        ImGuiUtils.Icon(FontAwesomeIcon.Bolt, Color.Yellow);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(_textService.Translate("HaselTweaks.Config.NetworkRequestWarning"));
            ImGui.EndTooltip();
        }
    }
}
