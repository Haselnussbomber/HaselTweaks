using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
#if !DEBUG
using System.Text.RegularExpressions;
#endif
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Gui;
using HaselCommon.Services;
using HaselTweaks.Enums;
using HaselTweaks.Extensions;
using HaselTweaks.Interfaces;
using HaselTweaks.Services;
using ImGuiNET;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public partial class PluginWindow : SimpleWindow
{
    private const uint SidebarWidth = 250;
    private readonly TextService _textService;
    private readonly ITextureProvider _textureProvider;
    private readonly TweakManager _tweakManager;
    private readonly IEnumerable<ITweak> _tweaks;
    private ITweak[] _orderedTweaks;
    private ITweak? _selectedTweak;

#if !DEBUG
    [GeneratedRegex("\\.0$")]
    private static partial Regex VersionPatchZeroRegex();
#endif

    [AutoPostConstruct]
    private void Initialize()
    {
        Size = new Vector2(766, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(766, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Always;

        Flags |= ImGuiWindowFlags.NoResize;

        AllowClickthrough = false;
        AllowPinning = false;

        SortTweaksbyName();
    }

    protected override void OnLanguageChanged(string langCode)
    {
        base.OnLanguageChanged(langCode);
        SortTweaksbyName();
    }

    private void SortTweaksbyName()
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
            var fixY = false;

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
                drawList.PathStroke(Color.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(Color.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                fixY = true;
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

            if (fixY)
            {
                ImGuiUtils.PushCursorY(3); // if i only knew why this happens
            }

            if (status is TweakStatus.InitializationFailed or TweakStatus.Outdated)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Color.Red);
            }
            else if (status is not TweakStatus.Enabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Color.Grey);
            }

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

            if (status is TweakStatus.InitializationFailed or TweakStatus.Outdated or not TweakStatus.Enabled)
            {
                ImGui.PopStyleColor();
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
                ImGui.SetCursorPos(contentAvail / 2 - logoSize / 2 + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0));
                ImGui.Image(logo.ImGuiHandle, logoSize);
            }

            // links, bottom left
            ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.GetTextLineHeight()));
            ImGuiUtils.DrawLink("GitHub", _textService.Translate("HaselTweaks.Config.GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/HaselTweaks");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGuiUtils.DrawLink("Ko-fi", _textService.Translate("HaselTweaks.Config.KoFiLink.Tooltip"), "https://ko-fi.com/haselnussbomber");

            // version, bottom right
#if DEBUG
            ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize("dev"));
            ImGuiUtils.DrawLink("dev", _textService.Translate("HaselTweaks.Config.DevGitHubLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev");
#else
            var version = GetType().Assembly.GetName().Version;
            if (version != null)
            {
                var versionString = "v" + VersionPatchZeroRegex().Replace(version.ToString(), "");
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
