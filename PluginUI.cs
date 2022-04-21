using ImGuiNET;
using System;
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
                                switch (field.FieldType.Name)
                                {
                                    case "String": DrawString(key, label, config, field, value); break;
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

        private void DrawString(string key, string label, object config, FieldInfo field, object? value)
        {
            var str = "";
            if (value != null) str = (string)value;

            ImGui.Text(label);
            ImGui.SameLine();

            if (ImGui.InputText(key, ref str, 50))
                field.SetValue(config, str);
        }
    }
}
