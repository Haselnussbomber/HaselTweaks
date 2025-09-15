using System.Threading.Tasks;
using Dalamud.Memory;
using Dalamud.Utility;
using HaselTweaks.Enums.PortraitHelper;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Services.PortraitHelper;
using HaselTweaks.Windows.PortraitHelperWindows.Dialogs;
using Lumina.Data.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HaselTweaks.Windows.PortraitHelperWindows.Overlays;

[RegisterTransient, AutoConstruct]
public partial class PresetBrowserOverlay : Overlay
{
    private const int SidebarWidth = 170;

    private static readonly Vector2 PortraitSize = new(576, 960); // native texture size

    private static readonly Color ButtonActiveColor = Color.White with { A = 0.3f };
    private static readonly Color ButtonHoveredColor = Color.White with { A = 0.2f };

    private readonly MenuBarState _state;
    private readonly IDataManager _dataManager;
    private readonly ITextureProvider _textureProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly BannerService _bannerService;
    private readonly ThumbnailService _thumbnailService;
    private readonly ClipboardService _clipboardService;
    private readonly DeletePresetDialog _deletePresetDialog;
    private readonly EditPresetDialog _editPresetDialog;
    private readonly CreateTagDialog _createTagDialog;
    private readonly RenameTagDialog _renameTagDialog;
    private readonly DeleteTagDialog _deleteTagDialog;

    private int _reorderTagOldIndex = -1;
    private int _reorderTagNewIndex = -1;

    public Guid? SelectedTagId { get; set; }

    [AutoPostConstruct]
    private void Initialize()
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 500),
            MaximumSize = new Vector2(4069),
        };
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

        _createTagDialog.Draw();
        _renameTagDialog.Draw();
        _deleteTagDialog.Draw();
        _deletePresetDialog.Draw();
        _editPresetDialog.Draw();
    }

    private void DrawSidebarTag(SavedPresetTag tag, ref bool removeUnusedTags)
    {
        var config = _pluginConfig.Tweaks.PortraitHelper;
        var count = config.Presets.Count(preset => preset.Tags.Contains(tag.Id));

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
                    ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.MovingTag.Tooltip", tag.Name));

                var bytes = tag.Id.ToByteArray();
                ImGui.SetDragDropPayload("MoveTag"u8, bytes, ImGuiCond.None);
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target)
            {
                unsafe
                {
                    var payload = ImGui.AcceptDragDropPayload("MoveTag"u8);
                    if (!payload.IsNull && payload.Data != null && payload.IsDelivery())
                    {
                        var tagId = MemoryHelper.Read<Guid>((nint)payload.Data).ToString();
                        _reorderTagOldIndex = config.PresetTags.AsEnumerable().IndexOf((tag) => tag.Id.ToString() == tagId);
                        _reorderTagNewIndex = config.PresetTags.IndexOf(tag);
                    }

                    payload = ImGui.AcceptDragDropPayload("MovePresetCard"u8);
                    if (!payload.IsNull && payload.Data != null && payload.IsDelivery())
                    {
                        var presetId = MemoryHelper.Read<Guid>((nint)payload.Data).ToString();
                        var preset = config.Presets.FirstOrDefault((preset) => preset?.Id.ToString() == presetId, null);
                        if (preset != null)
                        {
                            preset.Tags.Add(tag.Id);
                            _pluginConfig.Save();
                        }
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
                    _createTagDialog.Open();
                }

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RenameTag.Label")))
                {
                    _renameTagDialog.Open(tag);
                }

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveTag.Label")))
                {
                    _deleteTagDialog.Open(this, tag);
                }

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveUnusedTags.Label")))
                {
                    removeUnusedTags = true;
                }
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);
        DrawTagIcon();
    }

    private void DrawPresetBrowserSidebar()
    {
        using var framePadding = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);
        using var child = ImRaii.Child("PresetBrowser_SideBar", new Vector2(SidebarWidth - ImGui.GetStyle().ItemSpacing.X, -1));
        if (!child) return;
        framePadding?.Dispose();

        var config = _pluginConfig.Tweaks.PortraitHelper;
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

        foreach (var tag in config.PresetTags)
            DrawSidebarTag(tag, ref removeUnusedTags);

        if (_reorderTagOldIndex > -1 && _reorderTagOldIndex < config.PresetTags.Count && _reorderTagNewIndex > -1 && _reorderTagNewIndex < config.PresetTags.Count)
        {
            var item = config.PresetTags[_reorderTagOldIndex];
            config.PresetTags.RemoveAt(_reorderTagOldIndex);
            config.PresetTags.Insert(_reorderTagNewIndex, item);
            _pluginConfig.Save();
            _reorderTagOldIndex = -1;
            _reorderTagNewIndex = -1;
        }

        if (removeUnusedTags)
            RemoveUnusedTags();
    }

    private void DrawAllTag(ref bool removeUnusedTags)
    {
        var config = _pluginConfig.Tweaks.PortraitHelper;

        var treeNodeFlags =
            ImGuiTreeNodeFlags.SpanAvailWidth |
            ImGuiTreeNodeFlags.FramePadding |
            ImGuiTreeNodeFlags.DefaultOpen |
            ImGuiTreeNodeFlags.Leaf |
            (SelectedTagId == null ? ImGuiTreeNodeFlags.Selected : ImGuiTreeNodeFlags.None);

        using var allTreeNode = ImRaii.TreeNode(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.Sidebar.AllTags.Title", config.Presets.Count.ToString()) + "##PresetBrowser_SideBar_All", treeNodeFlags);
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
                    _createTagDialog.Open();

                if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetBrowserOverlay.ContextMenu.RemoveUnusedTags.Label")))
                    removeUnusedTags = true;
            }
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(4);
        DrawTagIcon();
    }

    private static void DrawTagIcon()
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.Disabled())
            ImGui.Text(FontAwesomeIcon.Tags.ToIconString());
    }

    private void RemoveUnusedTags()
    {
        var config = _pluginConfig.Tweaks.PortraitHelper;

        foreach (var tag in config.PresetTags.ToArray())
        {
            var isUsed = false;

            foreach (var preset in config.Presets)
            {
                if (preset.Tags.Contains(tag.Id))
                {
                    isUsed = true;
                    break;
                }
            }

            if (!isUsed)
                config.PresetTags.Remove(tag);
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

        var presetsPerRow = 3;
        var availableWidth = ImGui.GetContentRegionAvail().X - style.ItemInnerSpacing.X * presetsPerRow;

        var presetWidth = availableWidth / presetsPerRow;
        var scale = presetWidth / PortraitSize.X;

        var clipper = ImGui.ImGuiListClipper();

        var presets = _pluginConfig.Tweaks.PortraitHelper.Presets
            .Where((preset) => (SelectedTagId == null || preset.Tags.Contains(SelectedTagId.Value)) && preset.Preset != null);

        var presetCount = presets.Count();

        clipper.Begin((int)Math.Ceiling(presetCount / (float)presetsPerRow), PortraitSize.Y * scale);
        while (clipper.Step())
        {
            for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                for (int i = 0, index = row * presetsPerRow; i < presetsPerRow && index < presetCount; i++, index++)
                {
                    DrawPresetCard(presets.ElementAt(index), scale, DefaultImGuiTextColor);

                    if (i < presetsPerRow - 1 && index + 1 < presetCount)
                        ImGui.SameLine(0, style.ItemInnerSpacing.X);
                }
            }
        }
        clipper.Destroy();
    }

    private void DrawPresetCard(SavedPreset preset, float scale, uint defaultImGuiTextColor)
    {
        var hasBannerFrameRow = _excelService.TryGetRow<BannerFrame>(preset.Preset!.BannerFrame, out var bannerFrameRow);
        var hasBannerDecorationRow = _excelService.TryGetRow<BannerDecoration>(preset.Preset.BannerDecoration, out var bannerDecorationRow);

        var bannerFrameImage = hasBannerFrameRow ? (uint)bannerFrameRow.Image : 0u;
        var bannerDecorationImage = hasBannerDecorationRow ? (uint)bannerDecorationRow.Image : 0u;

        var isBannerTimelineUnlocked = _bannerService.IsBannerTimelineUnlocked(preset.Preset.BannerTimeline);
        var isBannerBgUnlocked = _bannerService.IsBannerBgUnlocked(preset.Preset.BannerBg);
        var isBannerFrameUnlocked = _bannerService.IsBannerFrameUnlocked(preset.Preset.BannerFrame);
        var isBannerDecorationUnlocked = _bannerService.IsBannerDecorationUnlocked(preset.Preset.BannerDecoration);

        var hasErrors = !isBannerTimelineUnlocked || !isBannerBgUnlocked || !isBannerFrameUnlocked || !isBannerDecorationUnlocked;
        var style = ImGui.GetStyle();

        using var _id = ImRaii.PushId(preset.Id.ToString());

        var cursorPos = ImGui.GetCursorPos();
        var center = cursorPos + PortraitSize * scale / 2f;

        _textureProvider.DrawIcon(190009, PortraitSize * scale);
        ImGui.SetCursorPos(cursorPos);

        var path = _thumbnailService.GetPortraitThumbnailPath(preset.Id);

        var hasThumbnailResult = _thumbnailService.TryGetThumbnail(
            preset.Id,
            new((int)(PortraitSize.X * scale), (int)(PortraitSize.Y * scale)),
            out var exists,
            out var textureWrap,
            out var exception);

        if (hasThumbnailResult)
        {
            if (textureWrap != null)
            {
                ImGui.SetCursorPos(cursorPos);
                ImGui.Image(textureWrap.Handle, PortraitSize * scale);
            }
            else if (exception != null)
            {
                using var font = ImRaii.PushFont(UiBuilder.IconFont);
                using var color = Color.Red.Push(ImGuiCol.Text);
                ImGui.SetCursorPos(center - ImGui.CalcTextSize(FontAwesomeIcon.Times.ToIconString()) / 2f);
                ImGui.Text(FontAwesomeIcon.Times.ToIconString());
            }
        }
        else if (!exists)
        {
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            using var color = Color.Red.Push(ImGuiCol.Text);
            ImGui.SetCursorPos(center - ImGui.CalcTextSize(FontAwesomeIcon.FileImage.ToIconString()) / 2f);
            ImGui.Text(FontAwesomeIcon.FileImage.ToIconString());
        }
        else
        {
            DrawLoadingSpinner(ImGui.GetWindowPos() - new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()) + center);
        }

        if (bannerFrameImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            _textureProvider.DrawIcon(bannerFrameImage, PortraitSize * scale);
        }

        if (bannerDecorationImage != 0)
        {
            ImGui.SetCursorPos(cursorPos);
            _textureProvider.DrawIcon(bannerDecorationImage, PortraitSize * scale);
        }

        if (hasErrors)
        {
            ImGui.SetCursorPos(cursorPos + new Vector2(PortraitSize.X - 190, 10) * scale);
            _textureProvider.Draw("ui/uld/Warning_hr1.tex", 160 * scale);
        }

        ImGui.SetCursorPos(cursorPos);

        {
            using var color = ImRaii.PushColor(ImGuiCol.Button, 0)
                .Push(ImGuiCol.ButtonActive, ButtonActiveColor)
                .Push(ImGuiCol.ButtonHovered, ButtonHoveredColor);
            using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0);
            ImGui.Button($"##{preset.Id}_Button", PortraitSize * scale);
        }

        using (var source = ImRaii.DragDropSource())
        {
            if (source)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetCard.MovingPresetCard.Tooltip", preset.Name));

                ImGui.SetDragDropPayload("MovePresetCard"u8, preset.Id.ToByteArray(), ImGuiCond.None);
            }
        }

        using (var target = ImRaii.DragDropTarget())
        {
            if (target)
            {
                var payload = ImGui.AcceptDragDropPayload("MovePresetCard"u8);
                unsafe
                {
                    if (!payload.IsNull && payload.IsDelivery() && payload.Data != null)
                    {
                        var presetId = MemoryHelper.Read<Guid>((nint)payload.Data).ToString();
                        var config = _pluginConfig.Tweaks.PortraitHelper;
                        var oldIndex = config.Presets.AsEnumerable().IndexOf((preset) => preset.Id.ToString() == presetId);
                        var newIndex = config.Presets.IndexOf(preset);
                        var item = config.Presets[oldIndex];
                        config.Presets.RemoveAt(oldIndex);
                        config.Presets.Insert(newIndex, item);
                        _pluginConfig.Save();
                    }
                }
            }
        }

        if (ImGui.IsItemHovered() && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            using (ImRaii.Tooltip())
            {
                using (ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor))
                    ImGui.Text(preset.Name);

                if (hasErrors)
                {
                    ImGui.Separator();

                    using (Color.Red.Push(ImGuiCol.Text))
                    {
                        ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.Title"));

                        using (ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 2))
                        using (ImRaii.PushIndent(1))
                        {
                            if (!isBannerTimelineUnlocked)
                            {
                                ImGui.Text("•");
                                ImGui.SameLine(0, 5);
                                ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.PoseNotUnlocked", _bannerService.GetBannerTimelineName(preset.Preset!.BannerTimeline)));
                            }

                            if (!isBannerBgUnlocked && _excelService.TryGetRow<BannerBg>(preset.Preset!.BannerBg, out var bannerBgRow))
                            {
                                ImGui.Text("•");
                                ImGui.SameLine(0, 5);
                                ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.BackgroundNotUnlocked", bannerBgRow.Name.ToString()));
                            }

                            if (!isBannerFrameUnlocked && hasBannerFrameRow)
                            {
                                ImGui.Text("•");
                                ImGui.SameLine(0, 5);
                                ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.FrameNotUnlocked", bannerFrameRow.Name.ToString()));
                            }

                            if (!isBannerDecorationUnlocked && hasBannerDecorationRow)
                            {
                                ImGui.Text("•");
                                ImGui.SameLine(0, 5);
                                ImGui.Text(_textService.Translate("PortraitHelperWindows.PresetCard.Tooltip.ElementsNotApplied.DecorationNotUnlocked", bannerDecorationRow.Name.ToString()));
                            }
                        }
                    }
                }
            }

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                _bannerService.ImportPresetToState(preset.Preset);
                _state.CloseOverlay();
            }
        }

        using var popup = ImRaii.ContextPopupItem($"{preset.Id}_Popup");
        if (!popup)
            return;

        using var textColor = ImRaii.PushColor(ImGuiCol.Text, defaultImGuiTextColor);

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.LoadPreset.Label")))
        {
            _bannerService.ImportPresetToState(preset.Preset);
            _state.CloseOverlay();
        }

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.EditPreset.Label")))
            _editPresetDialog.Open(preset);

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.ExportToClipboard.Label")))
            Task.Run(() => _clipboardService.SetClipboardPortraitPreset(preset.Preset));

        if (hasThumbnailResult && ImGui.BeginMenu(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Label")))
        {
            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.Everything.Label")))
                Task.Run(() => CopyImage(preset, path));

            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrame.Label")))
                Task.Run(() => CopyImage(preset, path, CopyImageFlags.NoFrame));

            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutDecoration.Label")))
                Task.Run(() => CopyImage(preset, path, CopyImageFlags.NoDecoration));

            if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.CopyImage.WithoutFrameAndDecoration.Label")))
                Task.Run(() => CopyImage(preset, path, CopyImageFlags.NoFrame | CopyImageFlags.NoDecoration));

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.MenuItem(_textService.Translate("PortraitHelperWindows.PresetCard.ContextMenu.DeletePreset.Label")))
            _deletePresetDialog.Open(preset);
    }

    public static void DrawLoadingSpinner(Vector2 center, float radius = 10f)
    {
        var angle = 0.0f;
        var numSegments = 10;
        var angleStep = (float)(Math.PI * 2.0f / numSegments);
        var time = ImGui.GetTime();
        var drawList = ImGui.GetWindowDrawList();

        for (var i = 0; i < numSegments; i++)
        {
            var pos = center + new Vector2(
                radius * (float)Math.Cos(angle),
                radius * (float)Math.Sin(angle));

            var t = (float)(-angle / (float)Math.PI / 2f + time) % 1f;
            var color = new Vector4(1f, 1f, 1f, 1 - t);

            drawList.AddCircleFilled(pos, 2f, ImGui.ColorConvertFloat4ToU32(color));
            angle += angleStep;
        }
    }

    private async Task CopyImage(SavedPreset preset, string filePath, CopyImageFlags flags = CopyImageFlags.None)
    {
        using var tempImage = await Image.LoadAsync<Rgba32>(filePath);

        if (!flags.HasFlag(CopyImageFlags.NoFrame) && _excelService.TryGetRow<BannerFrame>(preset.Preset!.BannerFrame, out var bannerFrameRow) && bannerFrameRow.Image != 0)
        {
            if (_textureProvider.TryGetIconPath(bannerFrameRow.Image, out var iconPath))
            {
                var texture = _dataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        if (!flags.HasFlag(CopyImageFlags.NoDecoration) && _excelService.TryGetRow<BannerDecoration>(preset.Preset!.BannerDecoration, out var bannerDecorationRow) && bannerDecorationRow.Image != 0)
        {
            if (_textureProvider.TryGetIconPath(bannerDecorationRow.Image, out var iconPath))
            {
                var texture = _dataManager.GetFile<TexFile>(iconPath);
                if (texture != null)
                {
                    using var image = Image.LoadPixelData<Rgba32>(texture.GetRgbaImageData(), texture.Header.Width, texture.Header.Height);
                    image.Mutate(x => x.Resize(tempImage.Width, tempImage.Height));
                    tempImage.Mutate(x => x.DrawImage(image, 1f));
                }
            }
        }

        await _clipboardService.SetClipboardImage(tempImage);
    }
}
