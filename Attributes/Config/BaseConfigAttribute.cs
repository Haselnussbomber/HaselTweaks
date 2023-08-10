using System.Reflection;
using Dalamud.Interface;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks;

public abstract class BaseConfigAttribute : Attribute
{
    public string OnChange = string.Empty;
    public string DependsOn = string.Empty;

    public abstract void Draw(Tweak tweak, object config, FieldInfo fieldInfo);

    protected void OnChangeInternal(Tweak tweak)
    {
        if (string.IsNullOrEmpty(OnChange))
            return;

        tweak.CachedType.GetMethod(OnChange, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(tweak, null);
    }

    protected bool DrawResetButton(string defaultValueString)
    {
        if (string.IsNullOrEmpty(defaultValueString))
            return false;

        ImGui.SameLine();
        return ImGuiUtils.IconButton("##Reset", FontAwesomeIcon.Undo, t("HaselTweaks.Config.ResetToDefault", defaultValueString));
    }
}
