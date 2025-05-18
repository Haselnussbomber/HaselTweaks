using Dalamud.Memory;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterScoped, AutoConstruct]
public unsafe partial class PresetBrowserOverlay : Overlay
{
    private const int SidebarWidth = 170;

    private readonly ILogger<PresetBrowserOverlay> _logger;
    private readonly TextService _textService;
    private readonly PluginConfig _pluginConfig;

    private int _reorderTagOldIndex = -1;
    private int _reorderTagNewIndex = -1;

    public Guid? SelectedTagId { get; set; }
    public Dictionary<Guid, PresetCard> PresetCards { get; init; } = [];

    public MenuBar MenuBar { get; internal set; } = null!;
    public CreateTagDialog CreateTagDialog { get; init; }
    public RenameTagDialog RenameTagDialog { get; init; }
    public DeleteTagDialog DeleteTagDialog { get; init; }
    public DeletePresetDialog DeletePresetDialog { get; init; }
    public EditPresetDialog EditPresetDialog { get; init; }

    [AutoPostConstruct]
    private void Initialize()
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 500),
            MaximumSize = new Vector2(4069),
        };
    }

    public override void OnClose()
    {
        PresetCards.Dispose();
        base.OnClose();
    }

    public void Open(MenuBar menuBar)
    {
        MenuBar = menuBar;
        Open();
    }

    public override void Draw()
    {
        base.Draw();

        DrawPresetBrowserSidebar();

        var paddingX = ImGui.GetStyle().ItemSpacing.X;
        ImGui.SameLine(0, paddingX * 2);

        var separatorStartPos = ImGui.GetWindowPos() + new Vector2(SidebarWidth + paddingX, 0);
        ImGui.GetWindowDrawList().AddLine(
            separatorStartPos,
            separatorStartPos + new Vector2(0, ImGui.GetWindowSize().Y),
            ImGui.GetColorU32(ImGuiCol.Separator));

        DrawPresetBrowserContent();

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
        if (!treeNode)
            return;

        if (ImGui.IsItemClicked())
        {
            SelectedTagId = tag.Id;
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, DefaultImGuiTextColor))
                    ImGui.TextUnformatted(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.MovingTag.Tooltip", tag.Name));

                var bytes = tag.Id.ToByteArray();
                fixed (byte* ptr = bytes)
                {
                    ImGui.SetDragDropPayload("MoveTag", (nint)ptr, (uint)bytes.Length);
                }
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target)
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
                        _pluginConfig.Save();
                    }
                }
            }
        }

        using (ImRaii.PushColor(ImGuiCol.Text, DefaultImGuiTextColor))
        {
            using var popup = ImRaii.ContextPopupItem($"##PresetBrowser_SideBar_Tag{tag.Id}Popup");
            if (popup)
            {
                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.CreateTag.Label")))
                {
                    CreateTagDialog.Open();
                }

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RenameTag.Label")))
                {
                    RenameTagDialog.Open(tag);
                }

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveTag.Label")))
                {
                    DeleteTagDialog.Open(this, tag);
                }

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveUnusedTags.Label")))
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
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("PresetBrowser_SideBar", new Vector2(SidebarWidth - ImGui.GetStyle().ItemSpacing.X, -1));
        if (!child) return;
        framePadding?.Dispose();

        var removeUnusedTags = false;

        ImGuiUtils.DrawSection(
            _textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.Tags.Title"),
            pushDown: false,
            respectUiTheme: !IsWindow);

        using var framePaddingChild = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var tagsChild = ImRaii.Child("PresetBrowser_Content_Tags");
        if (!tagsChild) return;
        framePaddingChild?.Dispose();

        DrawAllTag(ref removeUnusedTags);

        foreach (var tag in Config.PresetTags)
            DrawSidebarTag(tag, ref removeUnusedTags);

        if (_reorderTagOldIndex > -1 && _reorderTagOldIndex < Config.PresetTags.Count && _reorderTagNewIndex > -1 && _reorderTagNewIndex < Config.PresetTags.Count)
        {
            var item = Config.PresetTags[_reorderTagOldIndex];
            Config.PresetTags.RemoveAt(_reorderTagOldIndex);
            Config.PresetTags.Insert(_reorderTagNewIndex, item);
            _pluginConfig.Save();
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

        using var allTreeNode = ImRaii.TreeNode(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.AllTags.Title", Config.Presets.Count.ToString()) + $"##PresetBrowser_SideBar_All", treeNodeFlags);
        if (!allTreeNode)
            return;

        if (ImGui.IsItemClicked())
            SelectedTagId = null;

        using (ImRaii.PushColor(ImGuiCol.Text, DefaultImGuiTextColor))
        {
            using var popup = ImRaii.ContextPopupItem("##PresetBrowser_SideBar_AllPopup");
            if (popup)
            {
                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.CreateTag.Label")))
                    CreateTagDialog.Open();

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveUnusedTags.Label")))
                    removeUnusedTags = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);

        using (ImRaii.PushFont(UiBuilder.IconFont))
            ImGuiUtils.TextUnformattedDisabled(FontAwesomeIcon.Tags.ToIconString());
    }

    private void RemoveUnusedTags()
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

        _pluginConfig.Save();
    }

    private void DrawPresetBrowserContent()
    {
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("PresetBrowser_Content");
        if (!child) return;
        framePadding?.Dispose();

        ImGuiUtils.DrawSection(
            _textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.Presets.Title"),
            pushDown: false,
            respectUiTheme: !IsWindow);

        var style = ImGui.GetStyle();
        ImGuiUtils.PushCursorY(style.ItemSpacing.Y);

        using var framePaddingChild = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var presetCardsChild = ImRaii.Child("PresetBrowser_Content_PresetCards");
        if (!presetCardsChild) return;
        framePaddingChild?.Dispose();

        using var indentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, style.ItemSpacing.X);
        using var indent = ImRaii.PushIndent();

        var presetCards = Config.Presets
            .Where((preset) => (SelectedTagId == null || preset.Tags.Contains(SelectedTagId.Value)) && preset.Preset != null)
            .Select((preset) =>
            {
                if (!PresetCards.TryGetValue(preset.Id, out var card))
                {
                    var presetCard = new PresetCard(preset);
                    PresetCards.Add(preset.Id, presetCard);
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
                        presetCards[index]?.Draw(this, scale, DefaultImGuiTextColor);

                        if (i < presetsPerRow - 1 && index + 1 < presetCards.Length)
                            ImGui.SameLine(0, style.ItemInnerSpacing.X);
                    }
                }
            }
        }
        clipper.Destroy();
    }
}
