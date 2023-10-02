using System.Linq;
using System.Reflection;
using Dalamud.Interface;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks;

public abstract class BaseConfigAttribute : Attribute
{
    public string DependsOn = string.Empty;

    public abstract void Draw(Tweak tweak, object config, FieldInfo fieldInfo);

    protected void OnChangeInternal(Tweak tweak, FieldInfo fieldInfo)
    {
        Plugin.Config.Save();
        tweak.CachedType.GetMethod(nameof(Tweak.OnConfigChange), BindingFlags.Instance | BindingFlags.Public)?.Invoke(tweak, new[] { fieldInfo.Name });
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
                ImGui.SetTooltip(t(attribute.Translationkey));
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
