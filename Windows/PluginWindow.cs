using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud;
using Dalamud.Interface.Windowing;
using HaselTweaks.Attributes;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.Windows;

public class PluginWindow : Window
{
    private Plugin Plugin { get; }

    public PluginWindow(Plugin plugin) : base("HaselTweaks")
    {
        Plugin = plugin;

        Size = new Vector2(420, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(420, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        var Config = Configuration.Instance;

        foreach (var tweak in Plugin.Tweaks.OrderBy(t => t.Name))
        {
            void drawTooltip()
            {
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20.0f);

                    ImGui.Text(tweak.Name);

                    var status = "???";
                    var color = 0xFF999999; // gray

                    if (tweak.Outdated)
                    {
                        status = "Outdated";
                        color = 0xFF0000FF; // red
                    }
                    else if (!tweak.Ready)
                    {
                        status = "Not ready";
                        color = 0xFF0000FF; // red
                    }
                    else if (tweak.Enabled)
                    {
                        status = "Enabled";
                        color = 0xFF00FF00; // green
                    }
                    else if (!tweak.Enabled)
                    {
                        status = "Disabled";
                    }

                    if (status != "")
                    {
                        var posX = ImGui.GetCursorPosX();
                        var windowX = ImGui.GetFontSize() * 20.0f; //ImGui.GetWindowSize().X;
                        var textSize = ImGui.CalcTextSize(status);

                        ImGui.SameLine(windowX - textSize.X);

                        ImGui.PushStyleColor(ImGuiCol.Text, color);
                        ImGui.Text(status);
                        ImGui.PopStyleColor();
                    }

                    if (tweak.HasDescription)
                    {
                        ImGuiUtils.DrawPaddedSeparator();
                        tweak.DrawDescription();
                    }

                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }
            };

            if (!tweak.Ready || tweak.Outdated)
            {
                // padding left
                ImGui.SetCursorPosX(ImGui.GetFrameHeight() + ImGui.GetStyle().FramePadding.X * 10 + 1);

                // padding top
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);

                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
                ImGui.Text(tweak.Name);
                ImGui.PopStyleColor();

                // padding bottom
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);

                drawTooltip();

                if (tweak != Plugin.Tweaks[^1])
                    ImGui.Separator();

                continue;
            }

            var enabled = tweak.Enabled;
            if (ImGui.Checkbox($"##Enabled_{tweak.Name}", ref enabled))
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

            ImGui.SameLine();

            var config = Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(Config.Tweaks);

            if (config != null)
            {
                var isOpen = ImGui.TreeNodeEx(tweak.Name, ImGuiTreeNodeFlags.SpanAvailWidth);

                drawTooltip();

                if (isOpen)
                {
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

                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0);
                ImGui.TreeNodeEx(tweak.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                drawTooltip();
            }

            if (tweak != Plugin.Tweaks[^1])
                ImGui.Separator();
        }

        ImGui.End();
    }

    private static void DrawLabel(IConfigDrawData data)
    {
        ImGui.Text(data.Label);

        if (!string.IsNullOrEmpty(data.Description))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFBBBBBB);
            ImGui.TextWrapped(data.Description);
            ImGui.PopStyleColor();
        }

        if (data.SeparatorAfter)
            ImGui.Separator();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().IndentSpacing);
    }

    private static void DrawNoDrawingFunctionError(FieldInfo field)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
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
    }
}
