using System.Reflection;
using System.Threading.Tasks;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public partial class PluginWindow : SimpleWindow
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private ITweak[] _tweaks;
    private ITweak[] _orderedTweaks;
    private ITweak? _selectedTweak;
    private Vector2 _workSize;
    private Vector2 _windowSize;
    private bool _updateWindowSize;

    [AutoPostConstruct]
    private void Initialize(IEnumerable<IHostedService> services)
    {
        _tweaks = [.. services.OfType<ITweak>()];

        SizeCondition = ImGuiCond.Always;
        Flags |= ImGuiWindowFlags.NoResize;

        AllowClickthrough = false;
        AllowPinning = false;

        SortTweaksByName();
    }

    public override void OnLanguageChanged(string langCode)
    {
        base.OnLanguageChanged(langCode);
        SortTweaksByName();
        UpdateSize(true);
    }

    public override void OnScaleChanged(float scale)
    {
        UpdateSize(true, scale);
    }

    public override void OnClose()
    {
        _selectedTweak = null;

        foreach (var tweak in _orderedTweaks.Where(tweak => tweak.Status == TweakStatus.Enabled))
        {
            if (tweak is IConfigurableTweak configurableTweak)
                configurableTweak.OnConfigClose();
        }

        base.OnClose();
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (_updateWindowSize)
        {
            ImGui.SetNextWindowSize(_windowSize, ImGuiCond.Always);
            ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetWorkCenter(), ImGuiCond.Always, new Vector2(0.5f));
            _updateWindowSize = false;
        }
    }

    public override void Draw()
    {
        UpdateSize();
        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private unsafe void DrawSidebar()
    {
        var style = ImGui.GetStyle();

        var longestTweakNameWidth = _tweaks.Max(tweak
            => ImGui.CalcTextSize(
                _textService.TryGetTranslation(tweak.InternalName + ".Tweak.Name", out var name)
                    ? name
                    : tweak.InternalName).X);

        var sidebarWidth =
            style.ItemSpacing.X * 2 // margin left/right
            + style.ItemInnerSpacing.X * 2 // padding left/right
            + ImGui.GetFrameHeight() // checkbox
            + style.ItemSpacing.X // space between checkbox and name
            + longestTweakNameWidth // tweak name
            + style.ScrollbarSize; // scrollbar

        using var child = ImRaii.Child("##Sidebar", new Vector2(sidebarWidth, -1), true);
        if (!child) return;

        var selectedTweakName = _selectedTweak?.InternalName;
        var drawList = ImGui.GetWindowDrawList();
        var frameHeight = ImGui.GetFrameHeight();
        var selectableSize = new Vector2(sidebarWidth - frameHeight - style.ItemSpacing.X, frameHeight + 1);
        var g = ImGui.GetCurrentContext();

        foreach (var tweak in _orderedTweaks)
        {
            var tweakName = tweak.InternalName;
            var selected = _selectedTweak == tweak;

            var id = ImGui.GetID($"##SidebarTweak{tweakName}");
            var cursorScreenPos = ImGui.GetCursorScreenPos();
            var startPos = ImGui.GetCursorPos();
            var bb = new ImRect(cursorScreenPos + new Vector2(frameHeight, 0), cursorScreenPos + new Vector2(frameHeight, 0) + selectableSize);

            ImGuiP.ItemAdd(bb, id);

            var hovered = false;
            var held = false;
            var pressed = ImGuiP.ButtonBehavior(bb, id, ref hovered, ref held);

            if (pressed)
            {
                if (!g.NavDisableMouseHover && g.NavWindow == g.CurrentWindow && g.NavLayer == g.CurrentWindow.DC.NavLayerCurrent)
                {
                    ImGuiP.SetNavID(id, g.CurrentWindow.DC.NavLayerCurrent, g.CurrentWindow.DC.NavFocusScopeIdCurrent, new ImRect(startPos, startPos + selectableSize));
                    g.NavDisableHighlight = true;
                }

                ImGuiP.MarkItemEdited(id);
            }

            if (selected && !hovered && !held)
            {
                drawList.AddRectFilled(
                    cursorScreenPos,
                    cursorScreenPos + selectableSize,
                    Color.From(ImGuiCol.FrameBg).ToUInt(),
                    3f);
            }

            if (held)
            {
                drawList.AddRectFilled(
                    cursorScreenPos,
                    cursorScreenPos + selectableSize,
                    Color.From(ImGuiCol.FrameBgActive).ToUInt(),
                    3f);
            }
            else if (hovered)
            {
                drawList.AddRectFilled(
                    cursorScreenPos,
                    cursorScreenPos + selectableSize,
                    Color.From(ImGuiCol.FrameBgHovered).ToUInt(),
                    3f);
            }

            if (pressed)
            {
                if (_selectedTweak is IConfigurableTweak configurableTweak)
                    configurableTweak.OnConfigClose();

                if (_selectedTweak == null || selectedTweakName != tweakName)
                {
                    _selectedTweak = _orderedTweaks.FirstOrDefault(t => t.InternalName == tweakName);

                    if (_selectedTweak is IConfigurableTweak configurableTweak2)
                        configurableTweak2.OnConfigOpen();
                }
                else
                {
                    _selectedTweak = null;
                }
            }

            ImGui.SetCursorPos(startPos);

            var status = tweak.Status;
            var enabled = status == TweakStatus.Enabled;

            if (status is TweakStatus.Error or TweakStatus.Outdated)
            {
                var pos = cursorScreenPos;
                var size = new Vector2(frameHeight);

                ImGui.Dummy(size);

                if (ImGui.IsItemHovered())
                {
                    var color = status switch
                    {
                        TweakStatus.Error or TweakStatus.Outdated => Color.Red,
                        TweakStatus.Enabled => Color.Green,
                        _ => Color.Grey3
                    };

                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip)
                    {
                        using (color.Push(ImGuiCol.Text))
                            ImGui.Text(_textService.Translate($"HaselTweaks.Config.TweakStatus.{Enum.GetName(status)}"));
                    }
                }

                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

                var pad = frameHeight / 4f;
                pos += new Vector2(pad);
                size -= new Vector2(pad) * 2;

                drawList.PathLineTo(pos);
                drawList.PathLineTo(pos + size);
                drawList.PathStroke(Color.Red.ToUInt(), ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(Color.Red.ToUInt(), ImDrawFlags.None, frameHeight / 5f * 0.5f);
            }
            else
            {
                using var c = ImRaii.PushColor(ImGuiCol.FrameBg, Color.Transparent, !selected && (hovered || held))
                    .Push(ImGuiCol.FrameBg, new Color(1, 1, 1, 0.05f).ToUInt(), !enabled && !selected && !hovered && !held);

                if (ImGui.Checkbox($"##Enabled_{tweakName}", ref enabled))
                {
                    // TODO: catch errors and display them
                    if (!enabled)
                    {
                        tweak.OnDisable();
                        tweak.Status = TweakStatus.Disabled;

                        if (_pluginConfig.EnabledTweaks.Remove(tweakName))
                            _pluginConfig.Save();
                    }
                    else
                    {
                        tweak.OnEnable();
                        tweak.Status = TweakStatus.Enabled;

                        if (_pluginConfig.EnabledTweaks.Add(tweakName))
                            _pluginConfig.Save();
                    }
                }
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();

            using var _ = status switch
            {
                TweakStatus.Error or TweakStatus.Outdated => Color.Red.Push(ImGuiCol.Text),
                not TweakStatus.Enabled => Color.Grey.Push(ImGuiCol.Text),
                _ => null
            };

            if (!_textService.TryGetTranslation(tweakName + ".Tweak.Name", out var name))
                name = tweakName;

            ImGui.Text(name);
        }
    }

    private void DrawConfig()
    {
        using var child = ImRaii.Child("##Config", new Vector2(-1), true);
        if (!child)
            return;

        if (_selectedTweak == null)
            DrawHomeScreen();
        else
            DrawTweakConfig(_selectedTweak);
    }

    private void DrawHomeScreen()
    {
        var cursorPos = ImGui.GetCursorPos();
        var contentAvail = ImGui.GetContentRegionAvail();

        if (_textureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), "HaselTweaks.Assets.Logo.png").TryGetWrap(out var logo, out var _))
        {
            var logoSize = ImGuiHelpers.ScaledVector2(256, 128);
            ImGui.SetCursorPos(contentAvail / 2 - logoSize / 2 + ImGui.GetStyle().ItemSpacing.XOnly());
            ImGui.Image(logo.Handle, logoSize);
        }

        // links, bottom left
        ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.GetTextLineHeight()));
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
                Task.Run(licensesWindow.Toggle);
            }
        }

        // version, bottom right
#if DEBUG
        ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize("dev"));
        ImGuiUtils.DrawLink("dev", _textService.Translate("HaselTweaks.Config.DevGitHubLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev");
#else
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            var versionString = "v" + version.ToString(3);
            ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(versionString));
            ImGuiUtils.DrawLink(versionString, _textService.Translate("HaselTweaks.Config.ReleaseNotesLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
        }
#endif
    }

    private void DrawTweakConfig(ITweak tweak)
    {
        var internalName = tweak.InternalName;

        using var id = ImRaii.PushId(internalName);

        ImGui.TextColored(Color.Gold, _textService.TryGetTranslation(internalName + ".Tweak.Name", out var name) ? name : internalName);

        var statusText = _textService.Translate("HaselTweaks.Config.TweakStatus." + Enum.GetName(tweak.Status));
        var statusColor = tweak.Status switch
        {
            TweakStatus.Error or TweakStatus.Outdated => Color.Red,
            TweakStatus.Enabled => Color.Green,
            _ => Color.Grey3
        };

        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(statusText);
        ImGui.SameLine(windowX - textSize.X);

        ImGui.TextColored(statusColor, statusText);

        if (_textService.TryGetTranslation(internalName + ".Tweak.Description", out var description))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
            ImGui.TextColoredWrapped(Color.Grey2, description);
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
        }

        if (tweak is IConfigurableTweak configurableTweak)
        {
            using var _ = _configGui.PushContext(configurableTweak);
            configurableTweak.DrawConfig();
        }
    }

    private void SortTweaksByName()
    {
        _orderedTweaks = [.. _tweaks.OrderBy(tweak => _textService.TryGetTranslation(tweak.InternalName + ".Tweak.Name", out var name) ? name : tweak.InternalName)];
    }

    private void UpdateSize(bool forceFullUpdate = false, float? scale = null)
    {
        var workSize = ImGui.GetMainViewport().WorkSize;
        if (!forceFullUpdate && _workSize == workSize)
            return;

        var size = workSize * new Vector2(0.33f, 0.45f);
        size.X = MathF.Max(size.X, 600 * 1.3636f);
        size.Y = MathF.Max(size.Y, 600);
        _windowSize = size * (scale ?? ImGuiHelpers.GlobalScale);

        if (forceFullUpdate || _workSize != Vector2.Zero)
            _updateWindowSize = true;

        _workSize = workSize;
    }
}
