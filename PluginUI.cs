using Dalamud;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace HaselTweaks
{
    public class PluginUi : IDisposable
    {
        private Plugin Plugin { get; }

        private bool _show;

        internal bool Show
        {
            get => this._show;
            set => this._show = value;
        }

        public PluginUi(Plugin plugin)
        {
            this.Plugin = plugin;

            this.Plugin.PluginInterface.UiBuilder.Draw += this.Draw;
            this.Plugin.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfig;
        }

        public void Dispose()
        {
            this.Plugin.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfig;
            this.Plugin.PluginInterface.UiBuilder.Draw -= this.Draw;
        }

        private void OpenConfig()
        {
            this.Show = true;
        }

        private void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(350f, 450f), ImGuiCond.FirstUseEver);

            if (!this.Show)
            {
                return;
            }

            if (ImGui.Begin(Plugin.Name, ref this._show))
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

                    var config = Plugin.Config.Tweaks.GetType().GetProperty(tweak.InternalName)?.GetValue(Plugin.Config.Tweaks);
                    if (config != null)
                    {
                        if (ImGui.TreeNodeEx(tweak.Name))
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
                                    switch (field.FieldType.Name)
                                    {
                                        case "String": DrawString(key, label, config, field, (string)value!); break;
                                        default: DrawNoDrawingFunctionError(field.Name); break;
                                    }
                                }
                                else if (attr.Type == ConfigFieldTypes.SingleSelect)
                                {
                                    var options = tweak.GetType().GetField(attr.Options)?.GetValue(tweak);
                                    if (options is Dictionary<ClientLanguage, List<string>>)
                                    {
                                        var list = ((Dictionary<ClientLanguage, List<string>>)options)[Service.ClientState.ClientLanguage];
                                        DrawSingleSelect(key, label, config, field, (string)value!, list);
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

                                if (attr != null && !string.IsNullOrEmpty(attr.Description))
                                    ImGui.Text(attr.Description);
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
                    }

                    if (tweak != Plugin.Tweaks[^1])
                        ImGui.Separator();
                }

                ImGui.End();
            }

            ImGui.End();
        }

        private void DrawNoDrawingFunctionError(string fieldName)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF0000FF);
            ImGui.Text($"Could not find suitable drawing function for field \"{fieldName}\".");
            ImGui.PopStyleColor();
        }

        private void DrawSingleSelect(string key, string label, object config, FieldInfo field, string value, List<string> options)
        {
            if (ImGui.BeginCombo(label + key, value))
            {
                foreach (var item in options)
                {
                    if (ImGui.Selectable(item, value == item))
                    {
                        field.SetValue(config, item);
                        Plugin.SaveConfig();
                    }

                    if (value == item)
                        ImGui.SetItemDefaultFocus();
                }
                
                ImGui.EndCombo();
            }
        }

        private void DrawString(string key, string label, object config, FieldInfo field, string value)
        {
            ImGui.Text(label);
            ImGui.SameLine();

            if (ImGui.InputText(key, ref value, 50)) {
                field.SetValue(config, value);
                Plugin.SaveConfig();
            }
        }
    }
}
