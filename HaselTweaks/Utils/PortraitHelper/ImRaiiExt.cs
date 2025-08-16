using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace HaselTweaks.Utils.PortraitHelper;

public static unsafe class ImRaiiExt
{
    public static IEndObject PopupModal(string name, ImGuiWindowFlags flags)
    {
        return new EndConditionally(ImGui.EndPopup, BeginPopupModal(name, flags));
    }

    private static bool BeginPopupModal(string name, ImGuiWindowFlags flags)
    {
        if (name == null)
            return false;

        return ImGui.BeginPopupModal(name, flags);
    }
}
