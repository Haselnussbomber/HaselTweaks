using System.Reflection;
using System.Threading.Tasks;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public partial class PluginWindow : SimpleWindow
{
    private const uint SidebarWidth = 304;
    private const bool ShowDebugBorders = false;

    private readonly ILogger<PluginWindow> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;

    private ITweak[] _tweaks;
    private TweakEntry[] _shownTweaks;
    private ITweak? _selectedTweak;
    private string _searchInput = string.Empty;

    private readonly record struct TweakEntry(ITweak Tweak, string Label);

    [AutoPostConstruct]
    private void Initialize(IEnumerable<IHostedService> services)
    {
        _tweaks = [.. services.OfType<ITweak>()];

        Size = new Vector2(900, 630);
        SizeCondition = ImGuiCond.Always;
        SizeConstraints = new()
        {
            MinimumSize = Size.Value,
            MaximumSize = new Vector2(4096, 2160)
        };

        Flags |= ImGuiWindowFlags.NoResize;

        AllowClickthrough = false;
    }

    public override void OnLanguageChanged(string langCode)
    {
        base.OnLanguageChanged(langCode);

        if (IsOpen)
            UpdateShownTweaks();
    }

    private void UpdateShownTweaks()
    {
        var tweaks = _tweaks
            .Select(tweak => new TweakEntry(tweak, _textService.TryGetTranslation(tweak.InternalName + ".Tweak.Name", out var name) ? name : tweak.InternalName))
            .OrderBy(tuple => tuple.Label);

        _shownTweaks = string.IsNullOrWhiteSpace(_searchInput)
            ? [.. tweaks]
            : [.. tweaks.FuzzyMatch(_searchInput, t => t.Label).Select(r => r.Value)];

        if (IsOpen)
            Size = (Size ?? new Vector2(0, 630)) with { X = tweaks.Max(entry => ImGui.CalcTextSize(entry.Label).X) };
    }

    public override void OnOpen()
    {
        base.OnOpen();
        UpdateShownTweaks();
    }

    public override void OnClose()
    {
        _selectedTweak = null;

        foreach (var entry in _shownTweaks.Where(entry => entry.Tweak.Status == TweakStatus.Enabled))
        {
            if (entry.Tweak is IConfigurableTweak configurableTweak)
                configurableTweak.OnConfigClose();
        }

        WindowStyle.Dispose();
        base.OnClose();
    }

    public override void PreDraw()
    {
        base.PreDraw();
        WindowStyle.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw()
    {
        WindowStyle.Dispose();
        base.PostDraw();
    }

    public override void Draw()
    {
        var scale = ImStyle.Scale;
        var windowPadding = new Vector2(12, 12) * scale;
        var framePadding = new Vector2(8, 6) * scale;
        var startPos = ImCursor.Position;

        WindowStyle.Dispose();

        DrawSidebarBackground();
        DrawSidebarSearch(scale, windowPadding, framePadding);
        DrawSidebarList(scale, windowPadding, framePadding);

        ImCursor.Position = startPos + new Vector2(SidebarWidth * scale, 0);

        DrawConfig();
    }

#region Sidebar

    private static void DrawSidebarBackground()
    {
        // Sidebar Background
        ImGui.GetWindowDrawList().AddRectFilled(
            ImCursor.ScreenPosition,
            ImCursor.ScreenPosition + new Vector2(SidebarWidth * ImStyle.Scale, ImStyle.ContentRegionAvail.Y),
            (Color.White with { A = 0.05f }).ToUInt(),
            ImStyle.WindowRounding,
            ImDrawFlags.RoundCornersBottomLeft);
    }

    private void DrawSidebarSearch(float scale, Vector2 windowPadding, Vector2 framePadding)
    {
        using var childStyle = ImRaii
            .PushStyle(ImGuiStyleVar.WindowPadding, windowPadding);

        using var child = ImRaii.Child(
            "##SearchChild"u8,
            new Vector2(SidebarWidth * scale, ImGui.GetFontSize() + (windowPadding.Y + framePadding.Y) * 2),
            ShowDebugBorders,
            ImGuiWindowFlags.AlwaysUseWindowPadding);

        if (!child)
            return;

        childStyle.Dispose();

        using var style = ImRaii
            .PushStyle(ImGuiStyleVar.FramePadding, framePadding)
            .Push(ImGuiStyleVar.FrameRounding, 3);

        ImGui.SetNextItemWidth(-1);

        if (ImGui.InputTextWithHint(
            "##Search"u8,
            _textService.Translate("HaselTweaks.Config.Search.Hint"),
            ref _searchInput,
            512,
            ImGuiInputTextFlags.AutoSelectAll))
        {
            UpdateShownTweaks();

            _selectedTweak = string.IsNullOrWhiteSpace(_searchInput)
                ? null
                : _shownTweaks.FirstOrDefault().Tweak;
        }
    }

    private void DrawSidebarList(float scale, Vector2 windowPadding, Vector2 framePadding)
    {
        ImCursor.X += windowPadding.X;
        ImCursor.Y -= framePadding.Y;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        using var child = ImRaii.Child(
            "##SidebarList"u8,
            new Vector2(SidebarWidth * scale - windowPadding.X * 2, ImStyle.ContentRegionAvail.Y - framePadding.Y),
            ShowDebugBorders,
            ImGuiWindowFlags.AlwaysUseWindowPadding);
        if (!child)
            return;

        style.Dispose();

        foreach (var tweak in _shownTweaks)
        {
            DrawSidebarListItem(tweak);
        }
    }

    private void DrawSidebarListItem(TweakEntry entry)
    {
        var tweak = entry.Tweak;
        var status = tweak.Status;

        using var entryId = ImRaii.PushId(tweak.InternalName);

        if (status is TweakStatus.Error or TweakStatus.Outdated)
        {
            CrossedCheckbox();

            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                if (tooltip)
                {
                    var statusColor = status.GetColor();
                    ImGui.TextColored(statusColor, _textService.Translate(status.GetTranslateKey()));
                }
            }
        }
        else
        {
            var enabled = status == TweakStatus.Enabled;

            if (ImGui.Checkbox("##ToggleCheckbox"u8, ref enabled))
            {
                try
                {
                    if (!enabled)
                    {
                        tweak.OnDisable();
                        tweak.Status = TweakStatus.Disabled;

                        if (_pluginConfig.EnabledTweaks.Remove(tweak.InternalName))
                            _pluginConfig.Save();
                    }
                    else
                    {
                        tweak.OnEnable();
                        tweak.Status = TweakStatus.Enabled;

                        if (_pluginConfig.EnabledTweaks.Add(tweak.InternalName))
                            _pluginConfig.Save();
                    }
                }
                catch (Exception ex)
                {
                    tweak.Status = TweakStatus.Error;
                    _logger.LogError(ex, "Exception when toggling Tweak");
                }
            }
        }

        ImGui.SameLine(0, ImStyle.ItemInnerSpacing.X);

        if (ListSelectable(entry.Label, _selectedTweak == tweak))
        {
            if (_selectedTweak is IConfigurableTweak configurableTweak)
                configurableTweak.OnConfigClose();

            if (_selectedTweak == null || _selectedTweak?.InternalName != tweak.InternalName)
            {
                _selectedTweak = _shownTweaks.FirstOrDefault(entry => entry.Tweak.InternalName == tweak.InternalName).Tweak;

                if (_selectedTweak is IConfigurableTweak configurableTweak2)
                    configurableTweak2.OnConfigOpen();
            }
            else
            {
                _selectedTweak = null;
            }
        }
    }

    private static void CrossedCheckbox()
    {
        var drawList = ImGui.GetWindowDrawList();
        var frameHeight = ImStyle.FrameHeight;

        var pos = ImCursor.ScreenPosition;
        var size = new Vector2(frameHeight);

        ImGuiP.ItemSize(size, ImStyle.FramePadding.Y);

        if (!ImGuiP.ItemAdd(new ImRect(pos, pos + size), ImGuiP.GetCurrentWindow().GetID("##CrossedCheckbox"u8)))
            return;

        drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

        var pad = frameHeight / 4f;
        pos += new Vector2(pad);
        size -= new Vector2(pad) * 2;

        drawList.PathLineTo(pos);
        drawList.PathLineTo(pos + size);
        drawList.PathStroke(Color.ErrorForeground.ToUInt(), ImDrawFlags.None, frameHeight / 5f * 0.5f);

        drawList.PathLineTo(pos + new Vector2(0, size.Y));
        drawList.PathLineTo(pos + new Vector2(size.X, 0));
        drawList.PathStroke(Color.ErrorForeground.ToUInt(), ImDrawFlags.None, frameHeight / 5f * 0.5f);
    }

    private static bool ListSelectable(string label, bool selected)
    {
        var g = ImGui.GetCurrentContext();
        var window = ImGuiP.GetCurrentWindow();

        // basically ImGui::Selectable with some inner padding, rounded corners and some unnecessary code removed
        // https://github.com/ocornut/imgui/blob/v1.88/imgui_widgets.cpp#L6254

        var id = window.GetID("##Selectable"u8);
        var startScreenPos = ImCursor.ScreenPosition;
        var innerStartScreenPos = startScreenPos + ImStyle.ItemInnerSpacing.XOnly();
        var endPos = startScreenPos + new Vector2(ImStyle.ContentRegionAvail.X - 12, ImGui.GetFontSize()) + ImStyle.ItemInnerSpacing * 2;
        var bb = new ImRect(innerStartScreenPos, endPos);

        ImGuiP.ItemSize(bb, 0);

        if (!ImGuiP.ItemAdd(bb, id))
            return false;

        var hovered = false;
        var held = false;
        var button_flags = ImGuiButtonFlags.None;
        var pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held, button_flags);

        if (g.NavJustMovedToId != 0 && g.NavJustMovedToFocusScopeId == window.DC.NavFocusScopeIdCurrent && g.NavJustMovedToId == id)
            selected = pressed = true;

        if (pressed)
        {
            if (!g.NavDisableMouseHover && g.NavWindow == window && g.NavLayer == window.DC.NavLayerCurrent)
            {
                ImGuiP.SetNavID(id, window.DC.NavLayerCurrent, window.DC.NavFocusScopeIdCurrent, ImGuiP.WindowRectAbsToRel(window, bb));
                g.NavDisableHighlight = true;
            }
        }

        if (pressed)
            ImGuiP.MarkItemEdited(id);

        if (hovered || selected)
        {
            var col = ImGui.GetColorU32((held && hovered) ? ImGuiCol.HeaderActive : hovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header);
            ImGuiP.RenderFrame(bb.Min, bb.Max, col, false, 3.0f);
        }

        ImGuiP.RenderNavHighlight(bb, id, ImGuiNavHighlightFlags.TypeThin | ImGuiNavHighlightFlags.NoRounding);

        ImGuiP.RenderTextClipped(
            innerStartScreenPos + ImStyle.ItemInnerSpacing + ImStyle.ItemInnerSpacing.XOnly() / 2f,
            endPos, label, Vector2.Zero, ImStyle.SelectableTextAlign, bb);

        return pressed;
    }

    #endregion

#region Content

    private void DrawConfig()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(16, 12));
        using var child = ImRaii.Child("##Config"u8, new Vector2(-1), false, ImGuiWindowFlags.AlwaysUseWindowPadding);
        if (!child)
            return;

        style.Dispose();

        if (_selectedTweak == null)
        {
            DrawHomeScreen();
            return;
        }

        var internalName = _selectedTweak.InternalName;

        using var id = ImRaii.PushId(internalName);

        ImGui.TextColored(Color.Gold, _textService.TryGetTranslation(internalName + ".Tweak.Name", out var name) ? name : internalName);

        var statusText = _textService.Translate(_selectedTweak.Status.GetTranslateKey());
        var statusColor = _selectedTweak.Status.GetColor();

        var windowX = ImStyle.ContentRegionMax.X;
        var textSize = ImGui.CalcTextSize(statusText);
        ImGui.SameLine(windowX - textSize.X);

        ImGui.TextColored(statusColor, statusText);

        if (_textService.TryGetTranslation(internalName + ".Tweak.Description", out var description))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImCursor.Y += ImStyle.ItemSpacing.Y;
            ImGui.TextWrapped(description);
            ImCursor.Y += ImStyle.ItemSpacing.Y;
        }

        if (_selectedTweak is IConfigurableTweak configurableTweak)
        {
            using var _ = _configGui.PushContext(configurableTweak);
            configurableTweak.DrawConfig();
        }
    }

    private void DrawHomeScreen()
    {
        var cursorPos = ImGui.GetCursorPos();
        var contentAvail = ImStyle.ContentRegionAvail;

        if (_textureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), "HaselTweaks.Assets.Logo.png").TryGetWrap(out var logo, out var _))
        {
            var logoSize = ImGuiHelpers.ScaledVector2(256, 128);
            ImCursor.Position = contentAvail / 2 - logoSize / 2 + ImStyle.ItemSpacing.XOnly();
            ImGui.Image(logo.Handle, logoSize);
        }

        // links, bottom left
        ImCursor.Position = cursorPos + new Vector2(0, contentAvail.Y - ImGui.GetTextLineHeight());
        ImGuiUtils.DrawLink("GitHub", _textService.Translate("HaselTweaks.Config.GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/HaselTweaks");
        ImGui.SameLine();
        ImGui.Text("•");
        ImGui.SameLine();
        ImGuiUtils.DrawLink("Ko-fi", _textService.Translate("HaselTweaks.Config.KoFiLink.Tooltip"), "https://ko-fi.com/haselnussbomber");
        ImGui.SameLine();
        ImGui.Text("•");
        ImGui.SameLine();
        ImGui.Text(_textService.Translate("HaselTweaks.Config.Licenses"));
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _serviceProvider.GetService<LicensesWindow>() is { } licensesWindow)
            {
                _ = Task.Run(licensesWindow.Toggle);
            }
        }

        // version, bottom right
#if DEBUG
        ImCursor.Position = cursorPos + contentAvail - ImGui.CalcTextSize("dev");
        ImGuiUtils.DrawLink("dev", _textService.Translate("HaselTweaks.Config.DevGitHubLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev");
#else
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                var versionString = "v" + version.ToString(3);
                ImCursor.Position = cursorPos + contentAvail - ImGui.CalcTextSize(versionString);
                ImGuiUtils.DrawLink(versionString, _textService.Translate("HaselTweaks.Config.ReleaseNotesLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
            }
#endif
    }

    #endregion
}
