using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using HaselTweaks.Utils;
using ImGuiNET;
using static Dalamud.Game.Command.CommandInfo;

namespace HaselTweaks.Windows;

public partial class PluginWindow : Window
{
    private const uint SidebarWidth = 250;
    private const uint ConfigWidth = SidebarWidth * 2;

    private string SelectedTweak = string.Empty;
    private readonly GameFontHandle FontAxis36;

    [GeneratedRegex("\\.0$")]
    private static partial Regex VersionPatchZeroRegex();

    public PluginWindow() : base("HaselTweaks")
    {
        var width = SidebarWidth + ConfigWidth + ImGui.GetStyle().ItemSpacing.X + ImGui.GetStyle().FramePadding.X * 2;

        Size = new Vector2(width, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(width, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Appearing;

        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        Flags |= ImGuiWindowFlags.NoSavedSettings;

        FontAxis36 = Service.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis36));
    }

    public override void OnClose()
    {
        SelectedTweak = string.Empty;

        foreach (var tweak in Plugin.Tweaks)
        {
            if (tweak.Enabled && tweak.HasCustomConfig)
            {
                tweak.OnConfigWindowClose();
            }
        }
    }

    public override bool DrawConditions()
    {
        return FontAxis36.Available;
    }

    public override void Draw()
    {
        DrawSidebar();
        ImGui.SameLine();
        DrawConfig();
    }

    private void DrawSidebar()
    {
        using var child = ImRaii.Child("##HaselTweaks_Sidebar", new Vector2(SidebarWidth, -1), true);
        if (!child || !child.Success)
            return;

        using var table = ImRaii.Table("##HaselTweaks_SidebarTable", 2, ImGuiTableFlags.NoSavedSettings);
        if (!table || !table.Success)
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
                var pos = ImGui.GetWindowPos() + startPos;
                var frameHeight = ImGui.GetFrameHeight();

                var size = new Vector2(frameHeight);
                ImGui.SetCursorPos(startPos);
                ImGui.Dummy(size);

                if (ImGui.IsItemHovered())
                {
                    var (status, color) = GetTweakStatus(tweak);
                    using var tooltip = ImRaii.Tooltip();
                    if (tooltip != null && tooltip.Success)
                    {
                        ImGui.TextColored(color, status);
                    }
                }

                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

                var pad = frameHeight / 4f;
                pos += new Vector2(pad);
                size -= new Vector2(pad) * 2;

                drawList.PathLineTo(pos);
                drawList.PathLineTo(pos + size);
                drawList.PathStroke(ImGui.GetColorU32(ImGuiUtils.ColorRed), ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(ImGui.GetColorU32(ImGuiUtils.ColorRed), ImDrawFlags.None, frameHeight / 5f * 0.5f);

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
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3); // if i only knew why this happens
            }

            if (!tweak.Ready || tweak.Outdated)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorRed);
            }
            else if (!enabled)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGrey);
            }

            if (ImGui.Selectable($"{tweak.Name}##Selectable_{tweak.InternalName}", SelectedTweak == tweak.InternalName))
            {
                SelectedTweak = tweak.InternalName;
            }

            if (!tweak.Ready || tweak.Outdated || !enabled)
            {
                ImGui.PopStyleColor();
            }
        }
    }

    private void DrawConfig()
    {
        using var child = ImRaii.Child("##HaselTweaks_Config", new Vector2(ConfigWidth, -1), true);
        if (!child || !child.Success)
            return;

        if (string.IsNullOrEmpty(SelectedTweak))
        {
            var drawList = ImGui.GetWindowDrawList();
            var font = FontAxis36.ImFont;
            var cursorPos = ImGui.GetCursorPos();
            var absolutePos = ImGui.GetWindowPos() + cursorPos;
            var contentAvail = ImGui.GetContentRegionAvail();

            // I miss CSS...
            var offset = new Vector2(0, -8);
            var pluginNameSize = new Vector2(88, 18);
            var spacing = new Vector2(0, 28);

            drawList.AddText(
                font, 34,
                absolutePos + contentAvail / 2 - pluginNameSize - spacing / 2 + offset,
                ImGui.GetColorU32(ImGuiUtils.ColorWhite),
                "HaselTweaks"
            );

            drawList.AddLine(
                absolutePos + new Vector2(contentAvail.X / 5, contentAvail.Y / 2) + spacing / 2 + offset,
                absolutePos + new Vector2(contentAvail.X / 5 * 4, contentAvail.Y / 2) + spacing / 2 + offset,
                ImGui.GetColorU32(ImGuiUtils.ColorOrange)
            );

            // links, bottom left
            ImGui.SetCursorPos(cursorPos + new Vector2(0, contentAvail.Y - ImGui.CalcTextSize(" ").Y));
            ImGuiUtils.DrawLink("GitHub", "Visit the HaselTweaks GitHub Repository", "https://github.com/Haselnussbomber/HaselTweaks");
            ImGuiUtils.BulletSeparator();
            ImGuiUtils.DrawLink("Ko-fi", "Support me on Ko-fi", "https://ko-fi.com/haselnussbomber");

            // version, bottom right
            var version = GetType().Assembly.GetName().Version;
            if (version != null)
            {
                var versionString = "v" + VersionPatchZeroRegex().Replace(version.ToString(), "");
                ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(versionString));
                ImGuiUtils.DrawLink(versionString, "Visit Release Notes", $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
            }

            return;
        }

        var tweak = Plugin.Tweaks.FirstOrDefault(t => t.InternalName == SelectedTweak);
        if (tweak == null)
            return;

        ImGui.TextColored(ImGuiUtils.ColorGold, tweak.Name);

        var (status, color) = GetTweakStatus(tweak);

        var posX = ImGui.GetCursorPosX();
        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(status);

        ImGui.SameLine(windowX - textSize.X);

        ImGui.TextColored(color, status);

        if (tweak.HasDescription)
        {
            ImGuiUtils.DrawPaddedSeparator();
            tweak.DrawDescription();
        }

        if (tweak.HasIncompatibilityWarning)
        {
            ImGuiUtils.DrawSection("Incompatibility Warning");
            tweak.DrawIncompatibilityWarning();
        }

#if DEBUG
        if (tweak.LastException != null)
        {
            ImGuiUtils.DrawSection("[DEBUG] Exception");
            ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorRed, tweak.LastException.Message.Replace("HaselTweaks.Tweaks.", ""));
            ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorGrey2, tweak.LastException.StackTrace ?? "");
        }
#endif

        if (tweak.SlashCommands.Any())
        {
            ImGuiUtils.DrawSection("Slash Commands");

            foreach (var methodInfo in tweak.SlashCommands)
            {
                var attr = (SlashCommandAttribute?)methodInfo.GetCustomAttribute(typeof(SlashCommandAttribute));
                if (attr == null || Delegate.CreateDelegate(typeof(HandlerDelegate), tweak, methodInfo, false) == null)
                    continue;

                ImGui.Text("•");
                ImGui.SameLine();
                ImGui.Text(attr.Command);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                if (ImGui.IsItemClicked())
                {
                    methodInfo.Invoke(tweak, new string[] { attr.Command, "" });
                }

                if (!string.IsNullOrEmpty(attr.HelpMessage))
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().IndentSpacing);
                    ImGui.TextColored(ImGuiUtils.ColorGrey, attr.HelpMessage);
                }
            }
        }

        if (tweak.HasCustomConfig)
        {
            ImGuiUtils.DrawSection("Configuration");
            tweak.DrawCustomConfig();
        }
        else
        {
            var config = Plugin.Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(Plugin.Config.Tweaks);
            if (config != null)
            {
                ImGuiUtils.DrawSection("Configuration");
                var configType = config.GetType();

                foreach (var field in configType.GetFields())
                {
                    var attr = (ConfigFieldAttribute?)Attribute.GetCustomAttribute(field, typeof(ConfigFieldAttribute));

                    var hasDependency = !string.IsNullOrEmpty(attr?.DependsOn);
                    if (hasDependency)
                        ImGui.Indent();

                    var isDisabled = hasDependency && (bool?)configType.GetField(attr!.DependsOn)?.GetValue(config) == false;
                    if (isDisabled)
                        ImGui.BeginDisabled();

                    if (attr == null)
                    {
#if DEBUG
                        ImGui.TextColored(ImGuiUtils.ColorRed, $"No ConfigFieldAttribute for {field.Name}");
#endif
                    }
                    else if (attr.Type == ConfigFieldTypes.Ignore)
                    {
                        // hidden
                    }
                    else if (attr.Type == ConfigFieldTypes.Auto)
                    {
                        var data = Activator.CreateInstance(typeof(ConfigDrawData<>).MakeGenericType(new Type[] { field.FieldType }))!;

                        data.GetType().GetProperty("Tweak")!.SetValue(data, tweak);
                        data.GetType().GetProperty("Config")!.SetValue(data, config);
                        data.GetType().GetProperty("Field")!.SetValue(data, field);
                        data.GetType().GetProperty("Attr")!.SetValue(data, attr);

                        switch (field.FieldType.Name)
                        {
                            case nameof(String): DrawString((ConfigDrawData<string>)data); break;
                            case nameof(Single): DrawFloat((ConfigDrawData<float>)data); break;
                            case nameof(Boolean): DrawBool((ConfigDrawData<bool>)data); break;

                            default: DrawNoDrawingFunctionError(field); break;
                        }
                    }
                    else if (attr.Type == ConfigFieldTypes.SingleSelect)
                    {
                        if (field.FieldType.IsEnum)
                        {
                            var enumType = tweak.GetType().GetNestedType(attr.Options);
                            if (enumType == null)
                            {
                                DrawNoDrawingFunctionError(field);
                            }
                            else
                            {
                                var underlyingType = Enum.GetUnderlyingType(enumType);
                                var data = Activator.CreateInstance(typeof(ConfigDrawData<>).MakeGenericType(new Type[] { underlyingType }))!;

                                data.GetType().GetProperty("Tweak")!.SetValue(data, tweak);
                                data.GetType().GetProperty("Config")!.SetValue(data, config);
                                data.GetType().GetProperty("Field")!.SetValue(data, field);
                                data.GetType().GetProperty("Attr")!.SetValue(data, attr);

                                switch (underlyingType.Name)
                                {
                                    case nameof(Int32): DrawSingleSelectEnumInt32((ConfigDrawData<int>)data, enumType); break;

                                    default: DrawNoDrawingFunctionError(field); break;
                                }
                            }
                        }
                        else
                        {
                            var options = tweak.GetType().GetField(attr.Options)?.GetValue(tweak);
                            if (options is Dictionary<ClientLanguage, List<string>> opts)
                            {
                                var data = new ConfigDrawData<string>()
                                {
                                    Tweak = tweak,
                                    Config = config,
                                    Field = field,
                                    Attr = attr,
                                };
                                var list = opts[Service.ClientState.ClientLanguage];
                                DrawSingleSelect(data, list);
                            }
                            else
                            {
                                DrawNoDrawingFunctionError(field);
                            }
                        }
                    }
                    else
                    {
                        DrawNoDrawingFunctionError(field);
                    }

                    if (hasDependency)
                        ImGui.Unindent();

                    if (isDisabled)
                        ImGui.EndDisabled();
                }
            }
        }
    }

    private static (string, Vector4) GetTweakStatus(Tweak tweak)
    {
        var status = "???";
        var color = ImGuiUtils.ColorGrey3;

        if (tweak.Outdated)
        {
            status = "Outdated";
            color = ImGuiUtils.ColorRed;
        }
        else if (!tweak.Ready)
        {
            status = "Initialization failed";
            color = ImGuiUtils.ColorRed;
        }
        else if (tweak.Enabled)
        {
            status = "Enabled";
            color = ImGuiUtils.ColorGreen;
        }
        else if (!tweak.Enabled)
        {
            status = "Disabled";
        }

        return (status, color);
    }

    private static void DrawLabel(IConfigDrawData data)
    {
        ImGui.Text(data.Label);

        if (!string.IsNullOrEmpty(data.Description))
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
            ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorGrey, data.Description);
        }
    }

    private static void DrawNoDrawingFunctionError(FieldInfo field)
    {
        ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorRed, $"Could not find suitable drawing function for field \"{field.Name}\" (Type {field.FieldType.Name}).");
    }

    private static void DrawSingleSelectEnumInt32(ConfigDrawData<int> data, Type enumType)
    {
        var selectedLabel = "Invalid Option";

        var selectedName = Enum.GetName(enumType, data.Value);
        if (string.IsNullOrEmpty(selectedName))
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Missing Name for Value {data.Value} in {enumType.Name}.");
        }
        else
        {
            var selectedAttr = (EnumOptionAttribute?)enumType.GetField(selectedName)?.GetCustomAttribute(typeof(EnumOptionAttribute));
            if (selectedAttr == null)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Missing EnumOptionAttribute for {selectedName} in {enumType.Name}.");
            }
            else
            {
                selectedLabel = selectedAttr.Label;
            }
        }

        DrawLabel(data);

        using var combo = ImRaii.Combo(data.Key, selectedLabel);
        if (!combo || !combo.Success)
            return;

        var names = Enum.GetNames(enumType)
            .Select(name => (
                Name: name,
                Attr: (EnumOptionAttribute?)enumType.GetField(name)?.GetCustomAttribute(typeof(EnumOptionAttribute))
            ))
            .Where(tuple => tuple.Attr != null)
            .OrderBy((tuple) => tuple.Attr == null ? "" : tuple.Attr.Label);

        foreach (var (Name, Attr) in names)
        {
            var value = (int)Enum.Parse(enumType, Name);

            if (ImGui.Selectable(Attr!.Label, data.Value == value))
                data.Value = value;

            if (data.Value == value)
                ImGui.SetItemDefaultFocus();
        }
    }

    private static void DrawSingleSelect(ConfigDrawData<string> data, List<string> options)
    {
        DrawLabel(data);

        using var combo = ImRaii.Combo(data.Key, data.Value ?? "");
        if (!combo || !combo.Success)
            return;

        foreach (var item in options)
        {
            if (ImGui.Selectable(item, data.Value == item))
                data.Value = item;

            if (data.Value == item)
                ImGui.SetItemDefaultFocus();
        }
    }

    private static void DrawString(ConfigDrawData<string> data)
    {
        var value = data.Value;

        DrawLabel(data);

        if (ImGui.InputText(data.Key, ref value, 50))
            data.Value = value;

        DrawResetButton(data);
    }

    private static void DrawFloat(ConfigDrawData<float> data)
    {
        var min = data.Attr != null ? data.Attr.Min : 0f;
        var max = data.Attr != null ? data.Attr.Max : 100f;

        var value = data.Value;

        DrawLabel(data);

        if (ImGui.SliderFloat(data.Key, ref value, min, max))
            data.Value = value;

        DrawResetButton(data);
    }

    private static void DrawBool(ConfigDrawData<bool> data)
    {
        var value = data.Value;

        if (ImGui.Checkbox(data.Label + data.Key, ref value))
            data.Value = value;

        if (!string.IsNullOrEmpty(data.Description))
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
            ImGuiUtils.TextColoredWrapped(ImGuiUtils.ColorGrey, data.Description);
        }
    }

    private static void DrawResetButton<T>(ConfigDrawData<T> data)
    {
        if (data.Attr?.DefaultValue != null)
        {
            ImGui.SameLine();
            if (ImGuiUtils.IconButton(FontAwesomeIcon.Undo))
            {
                data.Value = (T)data.Attr!.DefaultValue;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"Reset to Default: {(T)data.Attr!.DefaultValue}");
            }
        }
    }
}
