using System.Collections.Generic;
using ImGuiNET;

namespace HaselTweaks.Utils;

public partial class ImGuiUtils
{
    public class ContextMenu : List<IContextMenuEntry>
    {
        private readonly string key;

        public ContextMenu(string key)
        {
            this.key = key;
        }

        public void Draw()
        {
            using var popup = ImRaiiExtensions.ContextPopupItem(key);
            if (!popup.Success)
                return;

            foreach (var entry in this)
            {
                entry.Draw();
            }
        }
    }

    public interface IContextMenuEntry
    {
        public bool Enabled { get; set; }
        public bool Hidden { get; set; }
        public string Label { get; set; }
        public Action? ClickCallback { get; set; }
        public Action? HoverCallback { get; set; }
        public bool LoseFocusOnClick { get; set; }
        public void Draw();
    }

    public record ContextMenuEntry : IContextMenuEntry
    {
        public bool Enabled { get; set; } = true;
        public bool Hidden { get; set; } = false;
        public string Label { get; set; } = string.Empty;
        public Action? ClickCallback { get; set; } = null;
        public Action? HoverCallback { get; set; } = null;
        public bool LoseFocusOnClick { get; set; } = false;

        public void Draw()
        {
            if (Hidden)
                return;

            if (ImGui.MenuItem(Label, Enabled))
            {
                ClickCallback?.Invoke();

                if (LoseFocusOnClick)
                {
                    ImGui.SetWindowFocus(null);
                }
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                HoverCallback?.Invoke();
            }
        }
    }
}
