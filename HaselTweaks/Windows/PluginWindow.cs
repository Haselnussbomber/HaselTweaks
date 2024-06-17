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
using HaselCommon;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using ImGuiNET;

namespace HaselTweaks.Windows;

public partial class PluginWindow : SimpleWindow, IDisposable
{
    private const uint SidebarWidth = 250;
    private const string LogoManifestResource = "HaselTweaks.Assets.Logo.png";

    private readonly DalamudPluginInterface PluginInterface;
    private readonly TranslationManager TranslationManager;
    private readonly ICommandManager CommandManager;
    private readonly TweakManager TweakManager;
    private readonly TextureManager TextureManager;
    private readonly ITweak[] Tweaks;
    private readonly CommandInfo CommandInfo;
    private readonly IDalamudTextureWrap? LogoTextureWrap;
    private readonly Point _logoSize = new(425, 132);

    private ITweak? SelectedTweak;
    private (IncompatibilityWarningAttribute Entry, bool IsLoaded)[]? IncompatibilityWarnings;

#if !DEBUG
    [GeneratedRegex("\\.0$")]
    private static partial Regex VersionPatchZeroRegex();
#endif

    public PluginWindow(
        WindowManager windowManager,
        DalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        TranslationManager translationManager,
        ICommandManager commandManager,
        TweakManager tweakManager,
        TextureManager textureManager,
        IEnumerable<ITweak> tweaks) : base(windowManager, "HaselTweaks")
    {
        PluginInterface = pluginInterface;
        TranslationManager = translationManager;
        CommandManager = commandManager;
        TweakManager = tweakManager;
        TextureManager = textureManager;
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
        TranslationManager.LanguageChanged += OnLanguageChanged;

        CommandManager.AddHandler("/haseltweaks", CommandInfo = new CommandInfo((_, _) => Toggle())
        {
            HelpMessage = translationManager.Translate("HaselTweaks.CommandHandlerHelpMessage")
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
        TranslationManager.LanguageChanged -= OnLanguageChanged;
        CommandManager.RemoveHandler("/haseltweaks");
        LogoTextureWrap?.Dispose();
        base.Dispose();
    }

    private void OnLanguageChanged(string langCode)
    {
        CommandInfo.HelpMessage = TranslationManager.Translate("HaselTweaks.CommandHandlerHelpMessage");
    }

    public override void OnClose()
    {
        SelectedTweak = null;
        IncompatibilityWarnings = null;

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
                        ImGuiUtils.TextUnformattedColored(color, TranslationManager.Translate($"HaselTweaks.Config.TweakStatus.{Enum.GetName(status)}"));
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

            if (!TranslationManager.TryGetTranslation(tweak.InternalName + ".Tweak.Name", out var name))
                name = tweak.InternalName;

            if (ImGui.Selectable(name + "##Selectable_" + tweak.InternalName, SelectedTweak != null && SelectedTweak.InternalName == tweak.InternalName))
            {
                if (SelectedTweak is IConfigurableTweak configurableTweak)
                    configurableTweak.OnConfigClose();

                IncompatibilityWarnings = null;

                if (SelectedTweak == null || SelectedTweak.InternalName != tweak.InternalName)
                    SelectedTweak = Tweaks.FirstOrDefault(t => t.InternalName == tweak.InternalName);
                else
                    SelectedTweak = null;
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
            ImGuiUtils.DrawLink("GitHub", TranslationManager.Translate("HaselTweaks.Config.GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/HaselTweaks");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGuiUtils.DrawLink("Ko-fi", TranslationManager.Translate("HaselTweaks.Config.KoFiLink.Tooltip"), "https://ko-fi.com/haselnussbomber");

            // version, bottom right
#if DEBUG
            ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize("dev"));
            ImGuiUtils.DrawLink("dev", TranslationManager.Translate("HaselTweaks.Config.DevGitHubLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev");
#else
            var version = GetType().Assembly.GetName().Version;
            if (version != null)
            {
                var versionString = "v" + VersionPatchZeroRegex().Replace(version.ToString(), "");
                ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(versionString));
                ImGuiUtils.DrawLink(versionString, TranslationManager.Translate("HaselTweaks.Config.ReleaseNotesLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
            }
#endif

            return;
        }

        using var id = ImRaii.PushId(SelectedTweak.InternalName);

        ImGuiUtils.TextUnformattedColored(Colors.Gold, TranslationManager.TryGetTranslation(SelectedTweak.InternalName + ".Tweak.Name", out var name) ? name : SelectedTweak.InternalName);

        var statusText = TranslationManager.Translate("HaselTweaks.Config.TweakStatus." + Enum.GetName(SelectedTweak.Status));
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

        if (TranslationManager.TryGetTranslation(SelectedTweak.InternalName + ".Tweak.Description", out var description))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, description);
        }

        DrawIncompatibilityWarnings();

        if (SelectedTweak is IConfigurableTweak configurableTweak)
            configurableTweak.DrawConfig();
    }

    private void DrawIncompatibilityWarnings()
    {
        IncompatibilityWarnings ??= SelectedTweak!.GetType().GetCustomAttributes<IncompatibilityWarningAttribute>()
            .Select(iw => (Entry: iw, IsLoaded: PluginInterface.InstalledPlugins.Any(p => p.InternalName == iw.InternalName && p.IsLoaded)))
            .ToArray();

        if (IncompatibilityWarnings.Any(tuple => tuple.IsLoaded))
        {
            ImGuiUtils.DrawSection(TranslationManager.Translate("HaselTweaks.Config.SectionTitle.IncompatibilityWarning"));
            TextureManager.GetIcon(60073).Draw(24);
            ImGui.SameLine();
            var cursorPosX = ImGui.GetCursorPosX();

            string getConfigName(string tweakName, string configName)
                => TranslationManager.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{tweakName}.Config.{configName}");

            if (IncompatibilityWarnings.Length == 1)
            {
                var (entry, isLoaded) = IncompatibilityWarnings[0];
                var pluginName = TranslationManager.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Name");

                if (isLoaded)
                {
                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Single.Plugin", pluginName));
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        var configName = getConfigName(entry.InternalName, entry.ConfigNames[0]);
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Single.PluginSetting", configName, pluginName));
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        var configNames = entry.ConfigNames.Select((configName) => TranslationManager.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{configName}"));
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Single.PluginSettings", pluginName) + $"\n- {string.Join("\n- ", configNames)}");
                    }
                }
            }
            else if (IncompatibilityWarnings.Length > 1)
            {
                ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.Preface"));

                foreach (var (entry, isLoaded) in IncompatibilityWarnings.Where(tuple => tuple.IsLoaded))
                {
                    var pluginName = TranslationManager.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Name");

                    ImGui.SetCursorPosX(cursorPosX);

                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.Plugin", pluginName));
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        var configName = TranslationManager.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{entry.ConfigNames[0]}");
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.PluginSetting", configName, pluginName));
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        var configNames = entry.ConfigNames.Select((configName) => TranslationManager.Translate($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{configName}"));
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, TranslationManager.Translate("HaselTweaks.Config.IncompatibilityWarning.Multi.PluginSettings", pluginName) + $"\n    - {string.Join("\n    - ", configNames)}");
                    }
                }
            }
        }
    }
}
