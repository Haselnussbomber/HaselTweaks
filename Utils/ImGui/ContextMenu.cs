using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Caches;
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

        public static unsafe ContextMenuEntry CreateTryOn(uint ItemId, uint GlamourItemId = 0, byte StainId = 0)
            => new()
            {
                Hidden = !ItemUtils.CanTryOn(ItemId),
                Label = StringCache.GetAddonText(2426), // "Try On"
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                        AgentTryon.TryOn(0, ItemId, StainId, 0, 0);
                    else
                        AgentTryon.TryOn(0, ItemId, StainId, GlamourItemId, StainId);
                }
            };

        public static unsafe ContextMenuEntry CreateItemFinder(uint ItemId)
            => new()
            {
                Label = StringCache.GetAddonText(4379), // "Search for Item"
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    ItemFinderModule.Instance()->SearchForItem(ItemId);
                }
            };

        public static ContextMenuEntry CreateGarlandTools(uint ItemId)
            => new()
            {
                Label = "Open on GarlandTools",
                ClickCallback = () =>
                {
                    Task.Run(() => Util.OpenLink($"https://www.garlandtools.org/db/#item/{ItemId}"));
                },
                HoverCallback = () =>
                {
                    using var tooltip = ImRaii.Tooltip();

                    var pos = ImGui.GetCursorPos();
                    ImGui.GetWindowDrawList().AddText(
                        UiBuilder.IconFont, 12 * ImGuiHelpers.GlobalScale,
                        ImGui.GetWindowPos() + pos + new Vector2(2),
                        Colors.Grey,
                        FontAwesomeIcon.ExternalLinkAlt.ToIconString()
                    );
                    ImGui.SetCursorPos(pos + new Vector2(20, 0) * ImGuiHelpers.GlobalScale);
                    TextUnformattedColored(Colors.Grey, $"https://www.garlandtools.org/db/#item/{ItemId}");
                }
            };

        public static ContextMenuEntry CreateCopyItemName(uint ItemId)
            => new()
            {
                Label = StringCache.GetAddonText(159), // "Copy Item Name"
                ClickCallback = () =>
                {
                    ImGui.SetClipboardText(StringCache.GetItemName(ItemId));
                }
            };

        public static ContextMenuEntry CreateItemSearch(uint ItemId)
            => new()
            {
                Hidden = !ItemSearchUtils.CanSearchForItem(ItemId),
                Label = Service.ClientState.ClientLanguage switch
                {
                    ClientLanguage.German => "Auf den M\u00e4rkten suchen",
                    ClientLanguage.French => "Rechercher sur les marchés",
                    ClientLanguage.Japanese => "市場で検索する",
                    _ => "Search the markets"
                },
                LoseFocusOnClick = true,
                ClickCallback = () =>
                {
                    ItemSearchUtils.Search(ItemId);
                }
            };
    }
}
