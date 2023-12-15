using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Memory;
using HaselCommon.Extensions;
using HaselCommon.Utils;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class PresetBrowserOverlay : Overlay, IDisposable
{
    public Guid? SelectedTagId { get; set; }
    public Dictionary<Guid, PresetCard> PresetCards { get; init; } = new();

    public CreateTagDialog CreateTagDialog { get; init; } = new();
    public RenameTagDialog RenameTagDialog { get; init; } = new();
    public DeleteTagDialog DeleteTagDialog { get; init; }
    public DeletePresetDialog DeletePresetDialog { get; init; }
    public EditPresetDialog EditPresetDialog { get; init; } = new();

    private int _reorderTagOldIndex = -1;
    private int _reorderTagNewIndex = -1;

    public PresetBrowserOverlay() : base(t("PortraitHelperWindows.PresetBrowserOverlay.Title"))
    {
        DeleteTagDialog = new(this);
        DeletePresetDialog = new(this);

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 500),
            MaximumSize = new Vector2(4069),
        };
    }

    public new void Dispose()
    {
        base.Dispose();

        foreach (var card in PresetCards.Values)
            card.Dispose();

        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        base.Draw();

        using (var table = ImRaii.Table("##PresetBrowser_Table", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            if (table.Success)
            {
                ImGui.TableSetupColumn("Tags", ImGuiTableColumnFlags.WidthFixed, 180);
                ImGui.TableSetupColumn("Presets", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextColumn();
                DrawPresetBrowserSidebar();

                ImGui.TableNextColumn();
                DrawPresetBrowserContent();
            }
        }

        CreateTagDialog.Draw();
        RenameTagDialog.Draw();
        DeleteTagDialog.Draw();
        DeletePresetDialog.Draw();
        EditPresetDialog.Draw();
    }

    private void DrawSidebarTag(SavedPresetTag tag, ref bool removeUnusedTags)
    {
        var count = Config.Presets.Count(preset => preset.Tags.Contains(tag.Id));

        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (tag.Id == SelectedTagId ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var treeNode = ImRaii.TreeNode($"{tag.Name} ({count})##PresetBrowser_SideBar_Tag{tag.Id}", treeNodeFlags);
        if (!treeNode.Success)
            return;

        if (ImGui.IsItemClicked())
        {
            SelectedTagId = tag.Id;
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source.Success)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, DefaultImGuiTextColor))
                    ImGui.TextUnformatted(t("PortraitHelperWindows.PresetBrowserOverlay.MovingTag.Tooltip", tag.Name));

                var bytes = tag.Id.ToByteArray();
                fixed (byte* ptr = bytes)
                {
                    ImGui.SetDragDropPayload("MoveTag", (nint)ptr, (uint)bytes.Length);
                }
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target.Success)
            {
                var payload = ImGui.AcceptDragDropPayload("MoveTag");
                if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                {
                    var tagId = MemoryHelper.Read<Guid>(payload.Data).ToString();
                    _reorderTagOldIndex = Config.PresetTags.AsEnumerable().IndexOf((tag) => tag.Id.ToString() == tagId);
                    _reorderTagNewIndex = Config.PresetTags.IndexOf(tag);
                }

                payload = ImGui.AcceptDragDropPayload("MovePresetCard");
                if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                {
                    var presetId = MemoryHelper.Read<Guid>(payload.Data).ToString();
                    var preset = Config.Presets.FirstOrDefault((preset) => preset?.Id.ToString() == presetId, null);
                    if (preset != null)
                    {
                        preset.Tags.Add(tag.Id);
                        Plugin.Config.Save();
                    }
                }
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, DefaultImGuiTextColor))
        {
            using var popup = ImRaii.ContextPopupItem($"##PresetBrowser_SideBar_Tag{tag.Id}Popup");
            if (popup.Success)
            {
                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.CreateTag.Label")))
                {
                    CreateTagDialog.Open();
                }

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RenameTag.Label")))
                {
                    RenameTagDialog.Open(tag);
                }

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveTag.Label")))
                {
                    DeleteTagDialog.Open(tag);
                }

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveUnusedTags.Label")))
                {
                    removeUnusedTags = true;
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGuiUtils.TextUnformattedDisabled(FontAwesomeIcon.Tag.ToIconString());
        }
    }

    private void DrawPresetBrowserSidebar()
    {
        var style = ImGui.GetStyle();

        var removeUnusedTags = false;

        ImGuiUtils.DrawSection(
            t("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.Tags.Title"),
            PushDown: false,
            RespectUiTheme: !IsWindow);

        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("##PresetBrowser_SideBar", ImGui.GetContentRegionAvail() - style.ItemInnerSpacing);
        if (!child.Success)
            return;
        framePadding?.Dispose();

        DrawAllTag(ref removeUnusedTags);

        foreach (var tag in Config.PresetTags)
        {
            DrawSidebarTag(tag, ref removeUnusedTags);
        }

        if (_reorderTagOldIndex > -1 && _reorderTagOldIndex < Config.PresetTags.Count && _reorderTagNewIndex > -1 && _reorderTagNewIndex < Config.PresetTags.Count)
        {
            var item = Config.PresetTags[_reorderTagOldIndex];
            Config.PresetTags.RemoveAt(_reorderTagOldIndex);
            Config.PresetTags.Insert(_reorderTagNewIndex, item);
            Plugin.Config.Save();
            _reorderTagOldIndex = -1;
            _reorderTagNewIndex = -1;
        }

        if (removeUnusedTags)
            RemoveUnusedTags();
    }

    private void DrawAllTag(ref bool removeUnusedTags)
    {
        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (SelectedTagId == null ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var allTreeNode = ImRaii.TreeNode(t("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.AllTags.Title", Config.Presets.Count.ToString()) + $"##PresetBrowser_SideBar_All", treeNodeFlags);
        if (!allTreeNode.Success)
            return;

        if (ImGui.IsItemClicked())
            SelectedTagId = null;

        using (ImRaii.PushColor(ImGuiCol.Text, DefaultImGuiTextColor))
        {
            using var popup = ImRaii.ContextPopupItem("##PresetBrowser_SideBar_AllPopup");
            if (popup.Success)
            {
                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.CreateTag.Label")))
                    CreateTagDialog.Open();

                if (ImGui.MenuItem(t("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveUnusedTags.Label")))
                    removeUnusedTags = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGuiUtils.TextUnformattedDisabled(FontAwesomeIcon.Tags.ToIconString());
        }
    }

    private static void RemoveUnusedTags()
    {
        foreach (var tag in Config.PresetTags.ToArray())
        {
            var isUsed = false;

            foreach (var preset in Config.Presets)
            {
                if (preset.Tags.Contains(tag.Id))
                {
                    isUsed = true;
                    break;
                }
            }

            if (!isUsed)
                Config.PresetTags.Remove(tag);
        }

        Plugin.Config.Save();
    }

    private void DrawPresetBrowserContent()
    {
        var style = ImGui.GetStyle();

        ImGuiUtils.PushCursorX(style.ItemSpacing.X);
        ImGuiUtils.DrawSection(
            t("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.Presets.Title"),
            PushDown: false,
            RespectUiTheme: !IsWindow);

        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("##PresetBrowser_Content", ImGui.GetContentRegionAvail() - new Vector2(0, 2)); // HACK: fixes scrolling where it shouldn't scroll
        if (!child.Success)
            return;
        framePadding?.Dispose();

        ImGuiUtils.PushCursorY(style.ItemSpacing.Y);
        ImGui.Indent(style.ItemSpacing.X);

        var presetCards = Config.Presets
            .Where((preset) => (SelectedTagId == null || preset.Tags.Contains(SelectedTagId.Value)) && preset.Preset != null)
            .Select((preset) =>
            {
                if (!PresetCards.TryGetValue(preset.Id, out var card))
                {
                    PresetCards.Add(preset.Id, new(this, preset));
                }

                return card;
            })
            .ToArray();

        var presetsPerRow = 3;
        var availableWidth = ImGui.GetContentRegionAvail().X - style.ItemInnerSpacing.X * presetsPerRow;

        var presetWidth = availableWidth / presetsPerRow;
        var scale = presetWidth / PresetCard.PortraitSize.X;

        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }

        clipper.Begin((int)Math.Ceiling(presetCards.Length / (float)presetsPerRow), PresetCard.PortraitSize.Y * scale);
        while (clipper.Step())
        {
            for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                using (ImRaii.Group())
                {
                    for (int i = 0, index = row * presetsPerRow; i < presetsPerRow && index < presetCards.Length; i++, index++)
                    {
                        presetCards[index]?.Draw(scale, DefaultImGuiTextColor);

                        if (i < presetsPerRow - 1 && index + 1 < presetCards.Length)
                            ImGui.SameLine(0, style.ItemInnerSpacing.X);
                    }
                }
            }
        }
        clipper.Destroy();

        ImGui.Unindent();
    }
}
