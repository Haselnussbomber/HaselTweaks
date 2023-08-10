using System.Reflection;
using Dalamud.Interface;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class StringConfigAttribute : BaseConfigAttribute
{
    public string DefaultValue = string.Empty;

    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var value = (string)fieldInfo.GetValue(config)!;

        ImGui.TextUnformatted(t($"{tweak.InternalName}.Config.{fieldInfo.Name}.Label"));

        using var indent = ImGuiUtils.ConfigIndent();

        if (ImGui.InputText("##Input", ref value, 50))
        {
            fieldInfo.SetValue(config, value);
            OnChangeInternal(tweak);
        }

        if (DrawResetButton(DefaultValue))
        {
            fieldInfo.SetValue(config, DefaultValue);
            OnChangeInternal(tweak);
        }

        if (Service.TranslationManager.TryGetTranslation($"{tweak.InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }
}
