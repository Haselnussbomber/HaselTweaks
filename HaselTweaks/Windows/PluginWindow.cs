using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Game.Command;
#if !DEBUG
using System.Text.RegularExpressions;
#endif
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselCommon.Textures;
using HaselCommon.Utils;
using HaselCommon.Windowing;
using HaselCommon.Windowing.Interfaces;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using ImGuiNET;

namespace HaselTweaks.Windows;

public partial class PluginWindow : SimpleWindow, IDisposable
{
    private const uint SidebarWidth = 250;
    private const string LogoManifestResource = "HaselTweaks.Assets.Logo.png";

    private readonly DalamudPluginInterface PluginInterface;
    private readonly TextService TextService;
    private readonly ICommandManager CommandManager;
    private readonly TweakManager TweakManager;
    private readonly ITweak[] Tweaks;
    private readonly CommandInfo CommandInfo;
    private readonly IDalamudTextureWrap? LogoTextureWrap;
    private readonly Point _logoSize = new(425, 132);

    private ITweak? SelectedTweak;

#if !DEBUG
    [GeneratedRegex("\\.0$")]
    private static partial Regex VersionPatchZeroRegex();
#endif

    public PluginWindow(
        IWindowManager windowManager,
        DalamudPluginInterface pluginInterface,
        TextService textService,
        IPluginLog pluginLog,
        ICommandManager commandManager,
        TweakManager tweakManager,
        IEnumerable<ITweak> tweaks)
        : base(windowManager, "HaselTweaks")
    {
        PluginInterface = pluginInterface;
        TextService = textService;
        CommandManager = commandManager;
        TweakManager = tweakManager;
        Tweaks = tweaks.ToArray();

        var width = SidebarWidth * 3 + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2;
        Size = new Vector2(width, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(width, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Always;

        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        Flags |= ImGuiWindowFlags.NoSavedSettings;

        AllowClickthrough = false;
        AllowPinning = false;

        PluginInterface.UiBuilder.OpenConfigUi += Toggle;
        TextService.LanguageChanged += OnLanguageChanged;

        CommandManager.AddHandler("/haseltweaks", CommandInfo = new CommandInfo((_, _) => Toggle())
        {
            HelpMessage = textService.Translate("HaselTweaks.CommandHandlerHelpMessage")
        });

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoManifestResource)
                ?? throw new Exception($"ManifestResource \"{LogoManifestResource}\" not found");

            using var ms = new MemoryStream();
            stream.CopyTo(ms);

            LogoTextureWrap = pluginInterface.UiBuilder.LoadImage(ms.ToArray());
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Error loading logo");
        }
    }

    public new void Dispose()
    {
        PluginInterface.UiBuilder.OpenConfigUi -= Toggle;
        TextService.LanguageChanged -= OnLanguageChanged;
        CommandManager.RemoveHandler("/haseltweaks");
        LogoTextureWrap?.Dispose();
        base.Dispose();
    }

    private void OnLanguageChanged(string langCode)
    {
        CommandInfo.HelpMessage = TextService.Translate("HaselTweaks.CommandHandlerHelpMessage");
    }

    public override void OnClose()
    {
        SelectedTweak = null;

        foreach (var tweak in Tweaks.Where(tweak => tweak.Status == TweakStatus.Enabled))
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
        if (!child.Success)
            return;

        using var table = ImRaii.Table("##SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Tweak Name", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tweak in Tweaks.OrderBy(t => t.InternalName))
        {
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
                        TweakStatus.InitializationFailed or TweakStatus.Outdated => Colors.Red,
                        TweakStatus.Enabled => Colors.Green,
                        _ => Colors.Grey3
                    };

                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip.Success)
                    {
                        TextService.Draw(color, $"HaselTweaks.Config.TweakStatus.{Enum.GetName(status)}");
                    }
                }

                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

                var pad = frameHeight / 4f;
                pos += new Vector2(pad);
                size -= new Vector2(pad) * 2;

                drawList.PathLineTo(pos);
                drawList.PathLineTo(pos + size);
                drawList.PathStroke(Colors.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(Colors.Red, ImDrawFlags.None, frameHeight / 5f * 0.5f);

                fixY = true;
            }
            else
            {
                var enabled = status == TweakStatus.Enabled;
                if (ImGui.Checkbox($"##Enabled_{tweak.InternalName}", ref enabled))
                {
                    // TODO: catch errors and display them
                    if (!enabled)
                        TweakManager.UserDisableTweak(tweak);
                    else
                        TweakManager.UserEnableTweak(tweak);
                }
            }

            ImGui.TableNextColumn();

            if (fixY)
            {
                ImGuiUtils.PushCursorY(3); // if i only knew why this happens
            }

            if (status is TweakStatus.InitializationFailed or TweakStatus.Outdated)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Red);
            }
            else if (status is not TweakStatus.Enabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Grey);
            }

            if (!TextService.TryGetTranslation(tweak.InternalName + ".Tweak.Name", out var name))
                name = tweak.InternalName;

            if (ImGui.Selectable(name + "##Selectable_" + tweak.InternalName, SelectedTweak != null && SelectedTweak.InternalName == tweak.InternalName))
            {
                if (SelectedTweak is IConfigurableTweak configurableTweak)
                    configurableTweak.OnConfigClose();

                if (SelectedTweak == null || SelectedTweak.InternalName != tweak.InternalName)
                {
                    SelectedTweak = Tweaks.FirstOrDefault(t => t.InternalName == tweak.InternalName);

                    if (SelectedTweak is IConfigurableTweak configurableTweak2)
                        configurableTweak2.OnConfigOpen();
                }
                else
                {
                    SelectedTweak = null;
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
        if (!child.Success)
            return;

        if (SelectedTweak == null)
        {
            var cursorPos = ImGui.GetCursorPos();
            var contentAvail = ImGui.GetContentRegionAvail();

            if (LogoTextureWrap != null && LogoTextureWrap.ImGuiHandle != 0)
            {
                var maxWidth = SidebarWidth * 2 * 0.85f * ImGuiHelpers.GlobalScale;
                var ratio = maxWidth / _logoSize.X;
                var scaledLogoSize = new Vector2(_logoSize.X, _logoSize.Y) * ratio;

                ImGui.SetCursorPos(contentAvail / 2 - scaledLogoSize / 2 + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0));
                ImGui.Image(LogoTextureWrap.ImGuiHandle, scaledLogoSize);
            }

            // links, bottom left
            ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.GetTextLineHeight()));
            ImGuiUtils.DrawLink("GitHub", TextService.Translate("HaselTweaks.Config.GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/HaselTweaks");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGuiUtils.DrawLink("Ko-fi", TextService.Translate("HaselTweaks.Config.KoFiLink.Tooltip"), "https://ko-fi.com/haselnussbomber");

            // version, bottom right
#if DEBUG
            ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize("dev"));
            ImGuiUtils.DrawLink("dev", TextService.Translate("HaselTweaks.Config.DevGitHubLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev");
#else
            var version = GetType().Assembly.GetName().Version;
            if (version != null)
            {
                var versionString = "v" + VersionPatchZeroRegex().Replace(version.ToString(), "");
                ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(versionString));
                ImGuiUtils.DrawLink(versionString, TextService.Translate("HaselTweaks.Config.ReleaseNotesLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
            }
#endif

            return;
        }

        using var id = ImRaii.PushId(SelectedTweak.InternalName);

        ImGuiUtils.TextUnformattedColored(Colors.Gold, TextService.TryGetTranslation(SelectedTweak.InternalName + ".Tweak.Name", out var name) ? name : SelectedTweak.InternalName);

        var statusText = TextService.Translate("HaselTweaks.Config.TweakStatus." + Enum.GetName(SelectedTweak.Status));
        var statusColor = SelectedTweak.Status switch
        {
            TweakStatus.InitializationFailed or TweakStatus.Outdated => Colors.Red,
            TweakStatus.Enabled => Colors.Green,
            _ => Colors.Grey3
        };

        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(statusText);
        ImGui.SameLine(windowX - textSize.X);

        ImGuiUtils.TextUnformattedColored(statusColor, statusText);

        if (TextService.TryGetTranslation(SelectedTweak.InternalName + ".Tweak.Description", out var description))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, description);
            ImGuiUtils.PushCursorY(ImGui.GetStyle().ItemSpacing.Y);
        }

        if (SelectedTweak is IConfigurableTweak configurableTweak)
            configurableTweak.DrawConfig();
    }
}
