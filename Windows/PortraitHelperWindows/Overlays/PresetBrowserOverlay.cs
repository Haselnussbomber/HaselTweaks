using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using HaselTweaks.Utils;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

public unsafe class PresetBrowserOverlay : Overlay
{
    public Guid? SelectedTagId { get; set; }
    public Dictionary<Guid, PresetCard> PresetCards { get; init; } = new();

    public CreateTagDialog CreateTagDialog { get; init; }
    public RenameTagDialog RenameTagDialog { get; init; } = new();
    public DeleteTagDialog DeleteTagDialog { get; init; }
    public DeletePresetDialog DeletePresetDialog { get; init; } = new();
    public EditPresetDialog EditPresetDialog { get; init; } = new();

    private int reorderTagOldIndex = -1;
    private int reorderTagNewIndex = -1;

    public PresetBrowserOverlay(PortraitHelper tweak) : base("[HaselTweaks] Portrait Helper PresetBrowser", tweak)
    {
        CreateTagDialog = new(this);
        DeleteTagDialog = new(this);

        base.IsOpen = true;
    }

    public override void Draw()
    {
        // TODO: fix error on closing
        ImGui.PopStyleVar(); // WindowPadding from PreDraw()

        using (var table = ImRaii.Table("##PresetBrowser_Table", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.NoPadOuterX | ImGuiTableFlags.NoPadInnerX))
        {
            if (table != null && table.Success)
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
        // TODO: optimize?!?
        var count = Config.Presets.Count(preset => preset.Tags.Contains(tag.Id));

        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (tag.Id == SelectedTagId ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var treeNode = ImRaii.TreeNode($"{tag.Name} ({count})##PresetBrowser_SideBar_Tag{tag.Id}", treeNodeFlags);
        if (treeNode == null || !treeNode.Success)
            return;

        if (ImGui.IsItemClicked())
        {
            SelectedTagId = tag.Id;
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source != null && source.Success)
            {
                ImGui.TextUnformatted($"Moving {tag.Name}");

                var idPtr = Marshal.StringToHGlobalAnsi(tag.Id.ToString());
                ImGui.SetDragDropPayload("MoveTag", idPtr, (uint)MemoryUtils.strlen(idPtr));
                Marshal.FreeHGlobal(idPtr);
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target != null && target.Success)
            {
                var payload = ImGui.AcceptDragDropPayload("MoveTag");
                if (payload.NativePtr != null && payload.IsDelivery() && payload.Data != 0)
                {
                    var tagId = Marshal.PtrToStringAnsi(payload.Data, payload.DataSize);
                    reorderTagOldIndex = Config.PresetTags.IndexOf((tag) => tag.Id.ToString() == tagId);
                    reorderTagNewIndex = Config.PresetTags.IndexOf(tag);
                }
            }
        }

        if (ImGui.BeginPopupContextItem($"##PresetBrowser_SideBar_Tag{tag.Id}Popup"))
        {
            if (ImGui.Selectable("Create Tag"))
            {
                CreateTagDialog.Open();
            }

            if (ImGui.Selectable("Rename Tag"))
            {
                RenameTagDialog.Open(tag);
            }

            if (ImGui.Selectable("Remove Tag"))
            {
                DeleteTagDialog.Open(tag);
            }

            if (ImGui.Selectable("Remove unused Tags"))
            {
                removeUnusedTags = true;
            }

            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Tag.ToIconString());
        }
    }

    private void DrawPresetBrowserSidebar()
    {
        var style = ImGui.GetStyle();

        var removeUnusedTags = false;

        // TODO: previews of portraits
        // TODO: right click instead of buttons

        ImGuiUtils.DrawSection("Tags", false);

        using var child = ImRaii.Child("##PresetBrowser_SideBar", ImGui.GetContentRegionAvail() - style.ItemInnerSpacing);
        if (child == null || !child.Success)
            return;

        DrawAllTags(ref removeUnusedTags);

        foreach (var tag in Config.PresetTags)
        {
            DrawSidebarTag(tag, ref removeUnusedTags);
        }

        if (reorderTagOldIndex > -1 && reorderTagOldIndex < Config.PresetTags.Count && reorderTagNewIndex > -1 && reorderTagNewIndex < Config.PresetTags.Count)
        {
            var item = Config.PresetTags[reorderTagOldIndex];
            Config.PresetTags.RemoveAt(reorderTagOldIndex);
            Config.PresetTags.Insert(reorderTagNewIndex, item);
            Plugin.Config.Save();
            reorderTagOldIndex = -1;
            reorderTagNewIndex = -1;
        }

        if (removeUnusedTags)
            RemoveUnusedTags();
    }

    private void DrawAllTags(ref bool removeUnusedTags)
    {
        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (SelectedTagId == null ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var allTreeNode = ImRaii.TreeNode($"All ({Config.Presets.Count})##PresetBrowser_SideBar_All", treeNodeFlags);
        if (allTreeNode == null || !allTreeNode.Success)
            return;

        if (ImGui.IsItemClicked())
            SelectedTagId = null;

        if (ImGui.BeginPopupContextItem("##PresetBrowser_SideBar_AllPopup"))
        {
            if (ImGui.Selectable("Create Tag"))
                CreateTagDialog.Open();

            if (ImGui.Selectable("Remove unused Tags"))
                removeUnusedTags = true;

            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Tag.ToIconString());
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

        ImGuiUtils.DrawSection("Presets", false);

        using var child = ImRaii.Child("##PresetBrowser_Content", ImGui.GetContentRegionAvail() - style.ItemInnerSpacing);
        if (!child.Success)
            return;

        ImGui.Indent(style.ItemInnerSpacing.X * 2);

        // TODO: clip scroll thingy
        // TODO: filters (bg, frame, decoration, pose...)

        var presets = Config.Presets
            .Where((preset) => SelectedTagId == null || preset.Tags.Contains(SelectedTagId.Value))
            .ToArray();

        for (var i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];

            if (!PresetCards.TryGetValue(preset.Id, out var card))
            {
                PresetCards.Add(preset.Id, new(this, preset.Id));
            }

            card?.Draw();

            if (i % 3 < 2)
                ImGui.SameLine(0, style.ItemInnerSpacing.X);
        }

        ImGui.Unindent();
    }
}
