using System.Reflection;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class EnumConfigAttribute : BaseConfigAttribute
{
    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var enumType = fieldInfo.FieldType;

        string GetOptionLabel(int value)
            => t($"{tweak.InternalName}.Config.{fieldInfo.Name}.Options.{Enum.GetName(enumType, value)}.Label");

        ImGui.TextUnformatted(t($"{tweak.InternalName}.Config.{fieldInfo.Name}.Label"));

        using var indent = ImGuiUtils.ConfigIndent();

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
                    OnChangeInternal(tweak, fieldInfo);
                }

                if (selectedValue == value)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
        }
        combo?.Dispose();

        if (Service.TranslationManager.TryGetTranslation($"{tweak.InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }
}
