using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HaselTweaks;

public class PluginWindow : Window
{
    private Plugin Plugin { get; }

    public PluginWindow(Plugin plugin) : base("HaselTweaks")
    {
        this.Plugin = plugin;

        base.Size = new Vector2(420, 690);
#if DEBUG
        base.SizeCondition = ImGuiCond.Appearing;
#else
        base.SizeCondition = ImGuiCond.FirstUseEver;
#endif
    }

    public override void Draw()
    {
        foreach (var tweak in this.Plugin.Tweaks.OrderBy(t => t.Name))
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

                    if (!string.IsNullOrEmpty(tweak.Description))
                    {
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);
                        ImGui.Separator();

                        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFBBBBBB);
                        ImGui.TextWrapped(tweak.Description);
                        ImGui.PopStyleColor();
                    }

                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }
            };

            if (!tweak.Ready)
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

                if (tweak != this.Plugin.Tweaks[^1])
                {
                    ImGui.Separator();
                }

                continue;
            }

            var enabled = tweak.Enabled;
            if (ImGui.Checkbox($"##Enabled_{tweak.Name}", ref enabled))
            {
                if (!enabled)
                {
                    tweak.DisableInternal();

                    if (this.Plugin.Config.EnabledTweaks.Contains(tweak.InternalName))
                    {
                        this.Plugin.Config.EnabledTweaks.Remove(tweak.InternalName);
                        this.Plugin.Config.Save();
                    }
                }
                else
                {
                    tweak.EnableInternal();

                    if (!this.Plugin.Config.EnabledTweaks.Contains(tweak.InternalName))
                    {
                        this.Plugin.Config.EnabledTweaks.Add(tweak.InternalName);
                        this.Plugin.Config.Save();
                    }
                }
            }

            ImGui.SameLine();

            var config = this.Plugin.Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(this.Plugin.Config.Tweaks);

            if (config != null)
            {
                var isOpen = ImGui.TreeNodeEx(tweak.Name);

                drawTooltip();

                if (isOpen)
                {
                    foreach (var field in config.GetType().GetFields())
                    {
                        var attr = (ConfigFieldAttribute?)Attribute.GetCustomAttribute(field, typeof(ConfigFieldAttribute));

                        if (attr == null || attr.Type == ConfigFieldTypes.Auto)
                        {
                            var data = Activator.CreateInstance(typeof(ConfigDrawData<>).MakeGenericType(new Type[] { field.FieldType }))!;

                            data.GetType().GetProperty("Plugin")!.SetValue(data, this.Plugin);
                            data.GetType().GetProperty("Tweak")!.SetValue(data, tweak);
                            data.GetType().GetProperty("Config")!.SetValue(data, config);
                            data.GetType().GetProperty("Field")!.SetValue(data, field);
                            data.GetType().GetProperty("Attr")!.SetValue(data, attr);

                            switch (field.FieldType.Name)
                            {
                                case nameof(String): DrawString((ConfigDrawData<string>)data); break;
                                case nameof(Single): DrawFloat((ConfigDrawData<float>)data); break;
                                case nameof(Boolean): DrawBool((ConfigDrawData<bool>)data); break;

                                default: DrawNoDrawingFunctionError(field.Name); break;
                            }
                        }
                        else if (attr.Type == ConfigFieldTypes.SingleSelect)
                        {
                            var options = tweak.GetType().GetField(attr.Options)?.GetValue(tweak);
                            if (options is Dictionary<ClientLanguage, List<string>> opts)
                            {
                                var data = new ConfigDrawData<string>()
                                {
                                    Plugin = this.Plugin,
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
                                DrawNoDrawingFunctionError(field.Name);
                            }
                        }
                        else
                        {
                            DrawNoDrawingFunctionError(field.Name);
                        }

                        if (attr != null)
                        {
                            if (!string.IsNullOrEmpty(attr.Description) && ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip(attr.Description);
                            }

                            if (attr.SeparatorAfter)
                            {
                                ImGui.Separator();
                            }
                        }
                    }

                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0);
                ImGui.TreeNodeEx(tweak.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                ImGui.PopStyleColor();
                ImGui.PopStyleColor();

                drawTooltip();
            }

            if (tweak != this.Plugin.Tweaks[^1])
            {
                ImGui.Separator();
            }
        }

        ImGui.End();
    }

    private static void DrawNoDrawingFunctionError(string fieldName)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
        ImGui.Text($"Could not find suitable drawing function for field \"{fieldName}\".");
        ImGui.PopStyleColor();
    }

    private static void DrawSingleSelect(ConfigDrawData<string> data, List<string> options)
    {
        if (ImGui.BeginCombo(data.Label + data.Key, data.Value))
        {
            foreach (var item in options)
            {
                if (ImGui.Selectable(item, data.Value == item))
                {
                    data.Value = item;
                }

                if (data.Value == item)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }
    }

    private static void DrawString(ConfigDrawData<string> data)
    {
        var value = data.Value;

        if (ImGui.InputText(data.Label + data.Key, ref value, 50))
        {
            data.Value = value;
        }
    }

    private static void DrawFloat(ConfigDrawData<float> data)
    {
        var min = data.Attr != null ? data.Attr.Min : 0f;
        var max = data.Attr != null ? data.Attr.Max : 100f;

        var value = data.Value;

        if (ImGui.SliderFloat(data.Label + data.Key, ref value, min, max))
        {
            data.Value = value;
        }
    }

    private static void DrawBool(ConfigDrawData<bool> data)
    {
        var value = data.Value;

        if (ImGui.Checkbox(data.Label + data.Key, ref value))
        {
            data.Value = value;
        }
    }
}
