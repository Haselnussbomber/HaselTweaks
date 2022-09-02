using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Dalamud;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Windowing;
using HaselTweaks.Utils;
using ImGuiNET;
using static Dalamud.Game.Command.CommandInfo;

namespace HaselTweaks.Windows;

public class PluginWindow : Window
{
    private const uint SidebarWidth = 250;
    private const uint ConfigWidth = SidebarWidth * 2;

    private Plugin Plugin { get; }
    private string SelectedTweak = string.Empty;
    private readonly GameFontHandle FontAxis36;

    public PluginWindow(Plugin plugin) : base("HaselTweaks")
    {
        Plugin = plugin;

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
        var Config = Configuration.Instance;

        if (!ImGui.BeginChild("##HaselTweaks_Sidebar", new Vector2(SidebarWidth, -1), true))
        {
            ImGui.EndChild();
            return;
        }


        if (!ImGui.BeginTable("##HaselTweaks_SidebarTable", 2, ImGuiTableFlags.NoSavedSettings))
        {
            return;
        }

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
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorLightRed);

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
                    ImGui.SetTooltip(status);
                }

                drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg), 3f, ImDrawFlags.RoundCornersAll);

                var pad = frameHeight / 4f;
                pos += new Vector2(pad);
                size -= new Vector2(pad) * 2;

                drawList.PathLineTo(pos);
                drawList.PathLineTo(pos + size);
                drawList.PathStroke(ImGui.GetColorU32(ImGuiCol.Text), ImDrawFlags.None, frameHeight / 5f * 0.5f);

                drawList.PathLineTo(pos + new Vector2(0, size.Y));
                drawList.PathLineTo(pos + new Vector2(size.X, 0));
                drawList.PathStroke(ImGui.GetColorU32(ImGuiCol.Text), ImDrawFlags.None, frameHeight / 5f * 0.5f);

                ImGui.PopStyleColor();

                fixY = true;
            }
            else
            {
                if (ImGui.Checkbox($"##Enabled_{tweak.InternalName}", ref enabled))
                {
                    if (!enabled)
                    {
                        tweak.DisableInternal();

                        if (Config.EnabledTweaks.Contains(tweak.InternalName))
                        {
                            Config.EnabledTweaks.Remove(tweak.InternalName);
                            Configuration.Save();
                        }
                    }
                    else
                    {
                        tweak.EnableInternal();

                        if (!Config.EnabledTweaks.Contains(tweak.InternalName))
                        {
                            Config.EnabledTweaks.Add(tweak.InternalName);
                            Configuration.Save();
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
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorLightRed);
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

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private void DrawConfig()
    {
        var Config = Configuration.Instance;

        if (!ImGui.BeginChild("##HaselTweaks_Config", new Vector2(ConfigWidth, -1), true))
        {
            ImGui.EndChild();
            return;
        }

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
                ImGuiUtils.ColorWhite,
                "HaselTweaks"
            );

            drawList.AddLine(
                absolutePos + new Vector2(contentAvail.X / 5, contentAvail.Y / 2) + spacing / 2 + offset,
                absolutePos + new Vector2(contentAvail.X / 5 * 4, contentAvail.Y / 2) + spacing / 2 + offset,
                ImGuiUtils.ColorOrange
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
                var versionString = "v" + Regex.Replace(version.ToString(), @"\.0$", "");
                ImGui.SetCursorPos(cursorPos + contentAvail - ImGui.CalcTextSize(versionString));
                ImGuiUtils.DrawLink(versionString, "Visit Release Notes", $"https://github.com/Haselnussbomber/HaselTweaks/releases/tag/{versionString}");
            }

            ImGui.EndChild();
            return;
        }

        Tweak? tweak = null;
        foreach (var t in Plugin.Tweaks)
        {
            if (t.InternalName == SelectedTweak)
            {
                tweak = t;
                break;
            }
        }
        if (tweak == null)
        {
            ImGui.EndChild();
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGold);
        ImGui.Text(tweak.Name);
        ImGui.PopStyleColor();

        var (status, color) = GetTweakStatus(tweak);

        var posX = ImGui.GetCursorPosX();
        var windowX = ImGui.GetContentRegionAvail().X;
        var textSize = ImGui.CalcTextSize(status);

        ImGui.SameLine(windowX - textSize.X);

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(status);
        ImGui.PopStyleColor();

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
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorRed);
            ImGui.TextWrapped(tweak.LastException.Message.Replace("HaselTweaks.Tweaks.", ""));
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGrey2);
            ImGui.TextWrapped(tweak.LastException.StackTrace);
            ImGui.PopStyleColor();
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
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGrey);
                    ImGui.Text(attr.HelpMessage);
                    ImGui.PopStyleColor();
                }
            }
        }

        var config = Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(Config.Tweaks);
        if (config != null)
        {
            ImGuiUtils.DrawSection("Configuration");

            foreach (var field in config.GetType().GetFields())
            {
                var attr = (ConfigFieldAttribute?)Attribute.GetCustomAttribute(field, typeof(ConfigFieldAttribute));

                if (attr == null || attr.Type == ConfigFieldTypes.Auto)
                {
                    var data = Activator.CreateInstance(typeof(ConfigDrawData<>).MakeGenericType(new Type[] { field.FieldType }))!;

                    data.GetType().GetProperty("Plugin")!.SetValue(data, Plugin);
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

                            data.GetType().GetProperty("Plugin")!.SetValue(data, Plugin);
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
                                Plugin = Plugin,
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
            }
        }

        ImGui.EndChild();
    }

    private static (string, uint) GetTweakStatus(Tweak tweak)
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
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGrey);
            ImGui.TextWrapped(data.Description);
            ImGui.PopStyleColor();
        }
    }

    private static void DrawNoDrawingFunctionError(FieldInfo field)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorRed);
        ImGui.TextWrapped($"Could not find suitable drawing function for field \"{field.Name}\" (Type {field.FieldType.Name}).");
        ImGui.PopStyleColor();
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

        if (ImGui.BeginCombo(data.Key, selectedLabel))
        {
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

            ImGui.EndCombo();
        }
    }

    private static void DrawSingleSelect(ConfigDrawData<string> data, List<string> options)
    {
        DrawLabel(data);

        if (ImGui.BeginCombo(data.Key, data.Value))
        {
            foreach (var item in options)
            {
                if (ImGui.Selectable(item, data.Value == item))
                    data.Value = item;

                if (data.Value == item)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
    }

    private static void DrawString(ConfigDrawData<string> data)
    {
        var value = data.Value;

        DrawLabel(data);

        if (ImGui.InputText(data.Key, ref value, 50))
            data.Value = value;
    }

    private static void DrawFloat(ConfigDrawData<float> data)
    {
        var min = data.Attr != null ? data.Attr.Min : 0f;
        var max = data.Attr != null ? data.Attr.Max : 100f;

        var value = data.Value;

        DrawLabel(data);

        if (ImGui.SliderFloat(data.Key, ref value, min, max))
            data.Value = value;
    }

    private static void DrawBool(ConfigDrawData<bool> data)
    {
        var value = data.Value;

        if (ImGui.Checkbox(data.Label + data.Key, ref value))
            data.Value = value;

        if (!string.IsNullOrEmpty(data.Description))
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiUtils.ColorGrey);
            ImGui.TextWrapped(data.Description);
            ImGui.PopStyleColor();
        }
    }
}
