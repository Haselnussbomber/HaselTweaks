using System.Reflection;
using Dalamud.Interface.Utility;
using HaselCommon;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks;

[AttributeUsage(AttributeTargets.Field)]
public class BoolConfigAttribute : BaseConfigAttribute
{
    public override void Draw(Tweak tweak, object config, FieldInfo fieldInfo)
    {
        var value = (bool)fieldInfo.GetValue(config)!;

        if (ImGui.Checkbox(t($"{tweak.InternalName}.Config.{fieldInfo.Name}.Label") + "##Input", ref value))
        {
            fieldInfo.SetValue(config, value);
            OnChangeInternal(tweak, fieldInfo);
        }

        DrawConfigInfos(fieldInfo);

        if (HaselCommonBase.TranslationManager.TryGetTranslation($"{tweak.InternalName}.Config.{fieldInfo.Name}.Description", out var description))
        {
            ImGuiUtils.PushCursorY(-3);
            using var descriptionIndent = ImGuiUtils.ConfigIndent();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey, description);
            ImGuiUtils.PushCursorY(3);
        }
    }
}
