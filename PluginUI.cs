using ImGuiNET;
using System;
using System.Numerics;

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
                    ImGui.Text(tweak.Name);

                    // TODO: tweak config
                    /*
                    if (ImGui.TreeNodeEx($"{tweak.Name}"))
                    {
                        ImGui.TreePop();
                    }
                    */

                    if (tweak != Plugin.Tweaks[^1])
                        ImGui.Separator();
                }

                ImGui.End();
            }

            ImGui.End();
        }
    }
}