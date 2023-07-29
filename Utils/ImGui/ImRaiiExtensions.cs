using ImGuiNET;
using IEndObject = Dalamud.Interface.Raii.ImRaii.IEndObject;

namespace HaselTweaks.Utils;

public static class ImRaiiExtensions
{
    public static IEndObject ContextPopupItem(string id)
        => new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupContextItem(id));

    public static IEndObject ContextPopupItem(string id, ImGuiPopupFlags flags)
        => new EndConditionally(ImGui.EndPopup, ImGui.BeginPopupContextItem(id, flags));

    private struct EndConditionally : IEndObject, IDisposable
    {
        private Action EndAction { get; }

        public bool Success { get; }

        public bool Disposed { get; private set; }

        public EndConditionally(Action endAction, bool success)
        {
            EndAction = endAction;
            Success = success;
            Disposed = false;
        }

        public void Dispose()
        {
            if (!Disposed)
            {
                if (Success)
                {
                    EndAction();
                }

                Disposed = true;
            }
        }
    }
}
