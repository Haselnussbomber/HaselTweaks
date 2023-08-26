using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using HaselTweaks.Enums;
using HaselTweaks.Extensions;
using HaselTweaks.Services;
using HaselTweaks.Utils;
using ImGuiNET;
using ImGuiScene;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Svg;
using ImColor = HaselTweaks.Structs.ImColor;

namespace HaselTweaks.Windows;

public partial class PluginWindow : Window, IDisposable
{
    private const uint SidebarWidth = 250;
    private const string LogoManifestResource = "HaselTweaks.Assets.Logo.svg";

    private string _selectedTweak = string.Empty;
    private bool _isLogoLoading;
    private TextureWrap? _logoTextureWrap;
    private readonly Point _logoSize = new(580, 180);
    private Point _renderedLogoSize = new(0, 0);

    [GeneratedRegex("\\.0$")]
    private static partial Regex VersionPatchZeroRegex();

    public PluginWindow() : base("HaselTweaks")
    {
        var style = ImGui.GetStyle();
        var width = SidebarWidth * 3 + style.ItemSpacing.X + style.FramePadding.X * 2;

        Namespace = "HaselTweaksConfig";

        Size = new Vector2(width, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(width, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Always;

        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        Flags |= ImGuiWindowFlags.NoSavedSettings;

        UpdateLogo();
    }

    public void Dispose()
    {
        _logoTextureWrap?.Dispose();
        _logoTextureWrap = null;
        GC.SuppressFinalize(this);
    }

    private Tweak? SelectedTweak => Plugin.Tweaks.FirstOrDefault(t => t.InternalName == _selectedTweak);

    public override void OnClose()
    {
        _selectedTweak = string.Empty;
        Flags &= ~ImGuiWindowFlags.MenuBar;

        foreach (var tweak in Plugin.Tweaks)
        {
            if (tweak.Enabled && tweak.Flags.HasFlag(TweakFlags.HasCustomConfig))
            {
                tweak.OnConfigWindowClose();
            }
        }

        Service.WindowManager.CloseWindow<PluginWindow>();
    }

    public override void Update()
    {
        UpdateLogo();
    }

    private void UpdateLogo()
    {
        if (_isLogoLoading)
            return;

        _renderedLogoSize.X = (int)(_logoSize.X * (_logoSize.X / (SidebarWidth * 2) * 0.75f) * ImGui.GetIO().FontGlobalScale);
        _renderedLogoSize.Y = (int)(_logoSize.Y * (_renderedLogoSize.X / (float)_logoSize.X));

        if (_renderedLogoSize.X <= 0 || _renderedLogoSize.Y <= 0)
            return;

        if (_logoTextureWrap != null && _logoTextureWrap.Width == _renderedLogoSize.X && _logoTextureWrap.Height == _renderedLogoSize.Y)
            return;

        _isLogoLoading = true;

        Task.Run(() =>
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoManifestResource);
                if (stream == null)
                {
                    PluginLog.Error("ManifestResource {0} not found", LogoManifestResource);
                    return;
                }

                var svgDocument = SvgDocument.Open<SvgDocument>(stream);
                using var bitmap = svgDocument.Draw(_renderedLogoSize.X, _renderedLogoSize.Y);
                using var memoryStream = new MemoryStream();

                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                using var image = Image.Load<Rgba32>(memoryStream);
                var data = new byte[4 * image.Width * image.Height];
                image.CopyPixelDataTo(data);

                _logoTextureWrap?.Dispose();
                _logoTextureWrap = Service.PluginInterface.UiBuilder.LoadImageRaw(data, image.Width, image.Height, 4);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error while loading logo");
            }
            finally
            {
                _isLogoLoading = false;
            }
        });
    }

    public override void Draw()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Language"))
            {
                static string GetLabel(string type, string code)
                {
                    return TranslationManager.AllowedLanguages.ContainsKey(code)
                        ? $"Override: {type} ({code})"
                        : $"Override: {type} ({code} is not supported, using fallback {TranslationManager.DefaultLanguage})";
                }
                if (ImGui.MenuItem(GetLabel("Dalamud", Service.PluginInterface.UiLanguage), "", Service.TranslationManager.Override == PluginLanguageOverride.Dalamud))
                {
                    Service.TranslationManager.Override = PluginLanguageOverride.Dalamud;
                }

                if (ImGui.MenuItem(GetLabel("Client", Service.ClientState.ClientLanguage.ToCode()), "", Service.TranslationManager.Override == PluginLanguageOverride.Client))
                {
                    Service.TranslationManager.Override = PluginLanguageOverride.Client;
                }

                ImGui.Separator();

                foreach (var (code, name) in TranslationManager.AllowedLanguages)
                {
                    if (ImGui.MenuItem(name, "", Service.TranslationManager.Override == PluginLanguageOverride.None && Service.TranslationManager.Language == code))
                    {
                        Service.TranslationManager.SetLanguage(PluginLanguageOverride.None, code);
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private void DrawSidebar()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        using var child = ImRaii.Child("##Sidebar", new Vector2(SidebarWidth * scale, -1), true);
        if (!child.Success)
            return;

        using var table = ImRaii.Table("##SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Tweak Name", ImGuiTableColumnFlags.WidthStretch);

        foreach (var tweak in Plugin.Tweaks.OrderBy(t => t.Name))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            var enabled = tweak.Enabled;
            var fixY = false;

            if (!tweak.Ready || tweak.Outdated)
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
                    var (status, color) = GetTweakStatus(tweak);
                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip.Success)
                    {
                        ImGuiUtils.TextUnformattedColored(color, status);
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
                if (ImGui.Checkbox($"##Enabled_{tweak.InternalName}", ref enabled))
                {
                    if (!enabled)
                    {
                        tweak.DisableInternal();

                        if (Plugin.Config.EnabledTweaks.Contains(tweak.InternalName))
                        {
                            Plugin.Config.EnabledTweaks.Remove(tweak.InternalName);
                            Plugin.Config.Save();
                        }
                    }
                    else
                    {
                        tweak.EnableInternal();

                        if (!Plugin.Config.EnabledTweaks.Contains(tweak.InternalName))
                        {
                            Plugin.Config.EnabledTweaks.Add(tweak.InternalName);
                            Plugin.Config.Save();
                        }
                    }
                }
            }

            ImGui.TableNextColumn();

            if (fixY)
            {
                ImGuiUtils.PushCursorY(3); // if i only knew why this happens
            }

            if (!tweak.Ready || tweak.Outdated)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Red);
            }
            else if (!enabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, (uint)Colors.Grey);
            }

            if (ImGui.Selectable($"{tweak.Name}##Selectable_{tweak.InternalName}", _selectedTweak == tweak.InternalName))
            {
                SelectedTweak?.OnConfigWindowClose();

                _selectedTweak = _selectedTweak != tweak.InternalName
                    ? tweak.InternalName
                    : string.Empty;
            }

            if (!tweak.Ready || tweak.Outdated || !enabled)
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

        var tweak = SelectedTweak;
        if (tweak == null)
        {
            var cursorPos = ImGui.GetCursorPos();
            var contentAvail = ImGui.GetContentRegionAvail();

            ImGuiUtils.PushCursorX(contentAvail.X - ImGuiUtils.GetIconButtonSize(FontAwesomeIcon.Cog).X);
            if (ImGuiUtils.IconButton("##PluginConfigButton", FontAwesomeIcon.Cog, "Toggle Plugin Configuration", active: Flags.HasFlag(ImGuiWindowFlags.MenuBar)))
            {
                Flags ^= ImGuiWindowFlags.MenuBar;
            }

            if (!_isLogoLoading && _logoTextureWrap != null && _logoTextureWrap.ImGuiHandle != 0)
            {
                ImGui.SetCursorPos(contentAvail / 2 - _renderedLogoSize / 2);
                ImGui.Image(_logoTextureWrap.ImGuiHandle, _renderedLogoSize);
            }

            // links, bottom left
            ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.CalcTextSize(" ").Y));
            ImGuiUtils.DrawLink("GitHub", t("HaselTweaks.Config.GitHubLink.Tooltip"), "https://github.com/Haselnussbomber/HaselTweaks");
            ImGui.SameLine();
            ImGui.TextUnformatted("•");
            ImGui.SameLine();
            ImGuiUtils.DrawLink("Ko-fi", t("HaselTweaks.Config.KoFiLink.Tooltip"), "https://ko-fi.com/haselnussbomber");

            // version, bottom right
#if DEBUG
            ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize("dev"));
            ImGuiUtils.DrawLink("dev", t("HaselTweaks.Config.DevGitHubLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/compare/main...dev");
#else
            var version = GetType().Assembly.GetName().Version;
            if (version != null)
            {
                var versionString = "v" + VersionPatchZeroRegex().Replace(version.ToString(), "");
                ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(versionString));
                ImGuiUtils.DrawLink(versionString, t("HaselTweaks.Config.ReleaseNotesLink.Tooltip"), $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
            }
#endif

            return;
        }

        using var id = ImRaii.PushId(tweak.InternalName);

        ImGuiUtils.TextUnformattedColored(Colors.Gold, tweak.Name);

        var (status, color) = GetTweakStatus(tweak);

        var posX = ImGui.GetCursorPosX();
        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(status);

        ImGui.SameLine(windowX - textSize.X);

        ImGuiUtils.TextUnformattedColored(color, status);

        if (!string.IsNullOrEmpty(tweak.Description))
        {
            ImGuiUtils.DrawPaddedSeparator();
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, tweak.Description);
        }

        if (tweak.IncompatibilityWarnings.Any(entry => entry.IsLoaded))
        {
            ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.IncompatibilityWarning"));
            Service.TextureManager.GetIcon(60073).Draw(24);
            ImGui.SameLine();
            var cursorPosX = ImGui.GetCursorPosX();

            static string getConfigName(string tweakName, string configName)
                => t($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{tweakName}.Config.{configName}");

            if (tweak.IncompatibilityWarnings.Length == 1)
            {
                var entry = tweak.IncompatibilityWarnings[0];
                var pluginName = t($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Name");

                if (entry.IsLoaded)
                {
                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Single.Plugin", pluginName));
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        var configName = getConfigName(entry.InternalName, entry.ConfigNames[0]);
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Single.PluginSetting", configName, pluginName));
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        var configNames = entry.ConfigNames.Select((configName) => t($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{configName}"));
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Single.PluginSettings", pluginName) + $"\n- {string.Join("\n- ", configNames)}");
                    }
                }
            }
            else if (tweak.IncompatibilityWarnings.Length > 1)
            {
                ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Multi.Preface"));

                foreach (var entry in tweak.IncompatibilityWarnings.Where(entry => entry.IsLoaded))
                {
                    var pluginName = t($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Name");

                    if (entry.ConfigNames.Length == 0)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Multi.Plugin", pluginName));
                    }
                    else if (entry.ConfigNames.Length == 1)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        var configName = t($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{entry.ConfigNames[0]}");
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Multi.PluginSetting", configName, pluginName));
                    }
                    else if (entry.ConfigNames.Length > 1)
                    {
                        ImGui.SetCursorPosX(cursorPosX);
                        var configNames = entry.ConfigNames.Select((configName) => t($"HaselTweaks.Config.IncompatibilityWarning.Plugin.{entry.InternalName}.Config.{configName}"));
                        ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, t("HaselTweaks.Config.IncompatibilityWarning.Multi.PluginSettings", pluginName) + $"\n    - {string.Join("\n    - ", configNames)}");
                    }
                }
            }
        }

#if DEBUG
        if (tweak.LastException != null)
        {
            ImGuiUtils.DrawSection("[DEBUG] Exception");
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Red, tweak.LastException.Message.Replace("HaselTweaks.Tweaks.", ""));
            ImGuiHelpers.SafeTextColoredWrapped(Colors.Grey2, tweak.LastException.StackTrace ?? "");
        }
#endif

        if (tweak.Flags.HasFlag(TweakFlags.HasCustomConfig))
        {
            if (!tweak.Flags.HasFlag(TweakFlags.NoCustomConfigHeader))
                ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

            tweak.DrawCustomConfig();
            return;
        }

        var config = Plugin.Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(Plugin.Config.Tweaks);
        if (config == null)
            return;

        var configType = config.GetType();
        var configFields = configType.GetFields();
        if (!configFields.Any())
            return;

        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        foreach (var field in configFields)
        {
            var attr = field.GetCustomAttribute<BaseConfigAttribute>();
            if (attr == null)
                continue;

            using var fieldid = ImRaii.PushId(field.Name);

            var hasDependency = !string.IsNullOrEmpty(attr.DependsOn);
            var isDisabled = hasDependency && (bool?)configType.GetField(attr.DependsOn)?.GetValue(config) == false;
            var indent = hasDependency ? ImGuiUtils.ConfigIndent() : null;
            var disabled = isDisabled ? ImRaii.Disabled() : null;

            attr.Draw(tweak, config, field);

            disabled?.Dispose();
            indent?.Dispose();
        }
    }

    private static (string, ImColor) GetTweakStatus(Tweak tweak)
    {
        var status = t("HaselTweaks.Config.TweakStatus.Unknown");
        var color = Colors.Grey3;

        if (tweak.Outdated)
        {
            status = t("HaselTweaks.Config.TweakStatus.Outdated");
            color = Colors.Red;
        }
        else if (!tweak.Ready)
        {
            status = t("HaselTweaks.Config.TweakStatus.InitializationFailed");
            color = Colors.Red;
        }
        else if (tweak.Enabled)
        {
            status = t("HaselTweaks.Config.TweakStatus.Enabled");
            color = Colors.Green;
        }
        else if (!tweak.Enabled)
        {
            status = t("HaselTweaks.Config.TweakStatus.Disabled");
        }

        return (status, color);
    }
}
