using System.Reflection;
using Dalamud.Interface;
using HaselTweaks.Utils;
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

    protected bool DrawResetButton(string defaultValueString)
    {
        if (string.IsNullOrEmpty(defaultValueString))
            return false;

        ImGui.SameLine();
        return ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", defaultValueString));
    }
}
