using System.Reflection;
using System.Threading.Tasks;
using HaselCommon.Windows;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public partial class PluginWindow : SimpleWindow
{
    private const uint SidebarWidth = 260;
    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;
    private readonly TweakManager _tweakManager;
    private readonly IEnumerable<ITweak> _tweaks;
    private ITweak[] _orderedTweaks;
    private ITweak? _selectedTweak;

    [AutoPostConstruct]
    private void Initialize()
    {
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
    }

    private void SortTweaksByName()
    {
        _orderedTweaks = [.. _tweaks.OrderBy(tweak => _textService.TryGetTranslation(tweak.GetInternalName() + ".Tweak.Name", out var name) ? name : tweak.GetInternalName())];
    }

    public override void OnOpen()
    {
        Size = new Vector2(SidebarWidth * 3 + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2, 600);
        SizeConstraints = new()
        {
            MinimumSize = (Vector2)Size,
            MaximumSize = new Vector2(4096, 2160)
        };
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

    public override void Draw()
    {
        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private void DrawSidebar()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var child = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth * scale, -1), true);
        if (!child)
            return;

        using var table = ImRaii.Table("##SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table)
            return;

        ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Tweak Name", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tweak in _orderedTweaks)
        {
            var tweakName = tweak.GetInternalName();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var status = tweak.Status;

            if (status is TweakStatus.InitializationFailed or TweakStatus.Outdated)
            {
                var startPos = ImGui.GetCursorPos();
                var drawList = ImGui.GetWindowDrawList();
                var pos = ImGui.GetWindowPos() + startPos - new Vector2(0, ImGui.GetScrollY());
                var frameHeight = ImGui.GetFrameHeight();

                var size = new Vector2(frameHeight);
                ImGui.SetCursorPos(startPos);
                ImGui.Dummy(size);

                if (ImGui.IsItemHovered())
                {
                    var color = status switch
                    {
                        TweakStatus.InitializationFailed or TweakStatus.Outdated => Color.Red,
                        TweakStatus.Enabled => Color.Green,
                        _ => Color.Grey3
                    };

                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip)
                    {
                        using (color.Push(ImGuiCol.Text))
                            ImGui.TextUnformatted(_textService.Translate($"HaselTweaks.Config.TweakStatus.{Enum.GetName(status)}"));
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
                var enabled = status == TweakStatus.Enabled;
                if (ImGui.Checkbox($"##Enabled_{tweakName}", ref enabled))
                {
                    // TODO: catch errors and display them
                    if (!enabled)
                        _tweakManager.UserDisableTweak(tweak);
                    else
                        _tweakManager.UserEnableTweak(tweak);
                }
            }

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();

            using var _ = status switch
            {
                TweakStatus.InitializationFailed or TweakStatus.Outdated => Color.Red.Push(ImGuiCol.Text),
                not TweakStatus.Enabled => Color.Grey.Push(ImGuiCol.Text),
                _ => null
            };

            if (!_textService.TryGetTranslation(tweakName + ".Tweak.Name", out var name))
                name = tweakName;

            var selectedTweakName = _selectedTweak?.GetInternalName();

            if (ImGui.Selectable(name + "##Selectable_" + tweakName, _selectedTweak != null && selectedTweakName == tweakName))
            {
                if (_selectedTweak is IConfigurableTweak configurableTweak)
                    configurableTweak.OnConfigClose();

                if (_selectedTweak == null || selectedTweakName != tweakName)
                {
                    _selectedTweak = _orderedTweaks.FirstOrDefault(t => t.GetInternalName() == tweakName);

                    if (_selectedTweak is IConfigurableTweak configurableTweak2)
                        configurableTweak2.OnConfigOpen();
                }
                else
                {
                    _selectedTweak = null;
                }
            }
        }
    }

    private void DrawConfig()
    {
        using var child = ImRaii.Child("##Config", new Vector2(-1), true);
        if (!child)
            return;

        if (_selectedTweak == null)
        {
            var cursorPos = ImGui.GetCursorPos();
            var contentAvail = ImGui.GetContentRegionAvail();

            if (_textureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), "HaselTweaks.Assets.Logo.png").TryGetWrap(out var logo, out var _))
            {
                var logoSize = ImGuiHelpers.ScaledVector2(256, 128);
                ImGui.SetCursorPos(contentAvail / 2 - logoSize / 2 + ImGui.GetStyle().ItemSpacing.XOnly());
                ImGui.Image(logo.ImGuiHandle, logoSize);
            }

            // links, bottom left
            ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.GetTextLineHeight()));
            ImGuiUtils.DrawLink("GitHub", _textService.Translate("HaselTweaks.Config.GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/HaselTweaks");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGuiUtils.DrawLink("Ko-fi", _textService.Translate("HaselTweaks.Config.KoFiLink.Tooltip"), "https://ko-fi.com/haselnussbomber");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGui.TextUnformatted(_textService.Translate("HaselTweaks.Config.Licenses"));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && Service.TryGet<LicensesWindow>(out var licensesWindow))
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

            return;
        }

        var selectedTweakName = _selectedTweak.GetInternalName();

        using var id = ImRaii.PushId(selectedTweakName);

        ImGuiUtils.TextUnformattedColored(Color.Gold, _textService.TryGetTranslation(selectedTweakName + ".Tweak.Name", out var name) ? name : selectedTweakName);

        var statusText = _textService.Translate("HaselTweaks.Config.TweakStatus." + Enum.GetName(_selectedTweak.Status));
        var statusColor = _selectedTweak.Status switch
        {
            TweakStatus.InitializationFailed or TweakStatus.Outdated => Color.Red,
            TweakStatus.Enabled => Color.Green,
            _ => Color.Grey3
        };

        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(statusText);
        ImGui.SameLine(windowX - textSize.X);

        ImGuiUtils.TextUnformattedColored(statusColor, statusText);

        if (_textService.TryGetTranslation(selectedTweakName + ".Tweak.Description", out var description))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
            ImGuiHelpers.SafeTextColoredWrapped(Color.Grey2, description);
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
        }

        if (_selectedTweak is IConfigurableTweak configurableTweak)
            configurableTweak.DrawConfig();
    }
}
