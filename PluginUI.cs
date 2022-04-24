using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Dalamud;
using ImGuiNET;

namespace HaselTweaks;

public partial class PluginUi
{
    private Plugin Plugin { get; }

    private bool _show = false;

    internal bool Show
    {
        get => _show;
        set => _show = value;
    }

    public PluginUi(Plugin plugin)
    {
        Plugin = plugin;

        Plugin.PluginInterface.UiBuilder.Draw += Draw;
        Plugin.PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;

        Service.Commands.AddHandler("/haseltweaks", new(delegate { Show = !Show; })
        {
            HelpMessage = "Show Configuration"
        });
    }

    private void OpenConfig()
    {
        Show = true;
    }

    private void Draw()
    {
        ImGui.SetNextWindowSize(new Vector2(350f, 450f), ImGuiCond.FirstUseEver);

        if (!Show)
        {
            return;
        }

        if (ImGui.Begin(Plugin.Name, ref _show))
        {
            foreach (var tweak in Plugin.Tweaks)
            {
                if (tweak.ForceLoad) continue;

                var enabled = tweak.Enabled;
                if (ImGui.Checkbox($"##Enabled_{tweak.Name}", ref enabled))
                {
                    if (!enabled)
                    {
                        tweak.DisableInternal();

                        if (Plugin.Config.EnabledTweaks.Contains(tweak.InternalName))
                        {
                            Plugin.Config.EnabledTweaks.Remove(tweak.InternalName);
                            Plugin.SaveConfig();
                        }
                    }
                    else
                    {
                        tweak.EnableInternal();

                        if (!Plugin.Config.EnabledTweaks.Contains(tweak.InternalName))
                        {
                            Plugin.Config.EnabledTweaks.Add(tweak.InternalName);
                            Plugin.SaveConfig();
                        }
                    }
                }

                ImGui.SameLine();

                var drawDescription = () =>
                {
                    if (!string.IsNullOrEmpty(tweak.Description) && ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(tweak.Description);
                    }
                };

                var config = Plugin.Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(Plugin.Config.Tweaks);

                if (config != null)
                {
                    var isOpen = ImGui.TreeNodeEx(tweak.Name);

                    drawDescription();

                    if (isOpen)
                    {
                        foreach (var field in config.GetType().GetFields())
                        {
                            var key = $"###{tweak.InternalName}#{field.Name}";

                            var attr = (ConfigFieldAttribute?)Attribute.GetCustomAttribute(field, typeof(ConfigFieldAttribute));

                            var label = field.Name;
                            if (attr != null && !string.IsNullOrEmpty(attr.Label))
                                label = attr.Label;

                            var value = field.GetValue(config);

                            if (attr == null || attr.Type == ConfigFieldTypes.Auto)
                            {
                                var type = field.FieldType;
                                var configDrawDataType = typeof(ConfigDrawData<>).MakeGenericType(new Type[] { field.FieldType });
                                var data = Activator.CreateInstance(configDrawDataType)!;

                                data.GetType().GetProperty("Plugin")!.SetValue(data, Plugin);
                                data.GetType().GetProperty("Tweak")!.SetValue(data, tweak);
                                data.GetType().GetProperty("Key")!.SetValue(data, key);
                                data.GetType().GetProperty("Label")!.SetValue(data, label);
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
                                        Plugin = Plugin,
                                        Tweak = tweak,
                                        Key = key,
                                        Label = label,
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

                            if (attr != null && !string.IsNullOrEmpty(attr.Description) && ImGui.IsItemHovered())
                                ImGui.SetTooltip(attr.Description);
                        }

                        ImGui.TreePop();
                    }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0x0);
                    ImGui.PushStyleColor(ImGuiCol.HeaderActive, 0x0);
                    ImGui.TreeNodeEx(tweak.Name, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                    ImGui.PopStyleColor();
                    ImGui.PopStyleColor();

                    drawDescription();
                }

                if (tweak != Plugin.Tweaks[^1])
                    ImGui.Separator();
            }

            ImGui.End();
        }

        ImGui.End();
    }

    private static void DrawNoDrawingFunctionError(string fieldName)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
        ImGui.Text($"Could not find suitable drawing function for field \"{fieldName}\".");
        ImGui.PopStyleColor();
    }

    private void DrawSingleSelect(ConfigDrawData<string> data, List<string> options)
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
                    ImGui.SetItemDefaultFocus();
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

public sealed partial class PluginUi : IDisposable
{
    private bool isDisposed;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposed)
            return;

        if (disposing)
        {
            Plugin.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
            Plugin.PluginInterface.UiBuilder.Draw -= Draw;

            Service.Commands.RemoveHandler("/haseltweaks");
        }

        isDisposed = true;
    }
}
