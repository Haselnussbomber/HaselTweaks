using System.Globalization;
using System.Reflection;
using Dalamud.Interface;
using HaselCommon;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class FloatConfigAttribute : BaseConfigAttribute
{
    public float DefaultValue = 0;
    public float Min = 0;
    public float Max = 100;

    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var value = (float)fieldInfo.GetValue(config)!;

        ImGui.TextUnformatted(t($"{tweak.InternalName}.Config.{fieldInfo.Name}.Label"));

        using var indent = ImGuiUtils.ConfigIndent();

        if (ImGui.DragFloat("##Input", ref value, 0.01f, Min, Max, "%.2f"))
        {
            fieldInfo.SetValue(config, value);
            OnChangeInternal(tweak, fieldInfo);
        }

        if (DrawResetButton(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", DefaultValue)))
        {
            fieldInfo.SetValue(config, DefaultValue);
            OnChangeInternal(tweak, fieldInfo);
        }

        if (HaselCommonBase.TranslationManager.TryGetTranslation($"{tweak.InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
        }
    }
}
