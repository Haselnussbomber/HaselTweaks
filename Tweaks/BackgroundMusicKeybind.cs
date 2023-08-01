using System.Collections.Generic;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Raii;
using HaselTweaks.Structs;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Background Music Keybind",
    Description: "A configurable keybind to toggle the background music.",
    HasCustomConfig: true
)]
public unsafe class BackgroundMusicKeybind : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.BackgroundMusicKeybind;

    public class Configuration
    {
        public VirtualKey[] Keybind = new[] { VirtualKey.CONTROL, VirtualKey.M };
    }

    public override void DrawCustomConfig()
    {
        ImGui.Text("Keybind:");
        ImGui.Spacing();
        using var indent = ImRaii.PushIndent();

        var shift = Config.Keybind.Contains(VirtualKey.SHIFT);
        if (ImGui.Checkbox("Shift", ref shift))
        {
            var set = new HashSet<VirtualKey>(Config.Keybind);

            if (shift && !Config.Keybind.Contains(VirtualKey.SHIFT))
            {
                set.Add(VirtualKey.SHIFT);
            }
            if (!shift && Config.Keybind.Contains(VirtualKey.SHIFT))
            {
                set.Remove(VirtualKey.SHIFT);
            }

            Config.Keybind = set.Order().ToArray();
            Plugin.Config.Save();
        }

        ImGui.SameLine();

        var ctrl = Config.Keybind.Contains(VirtualKey.CONTROL);
        if (ImGui.Checkbox("Control", ref ctrl))
        {
            var set = new HashSet<VirtualKey>(Config.Keybind);

            if (ctrl && !Config.Keybind.Contains(VirtualKey.CONTROL))
            {
                set.Add(VirtualKey.CONTROL);
            }
            if (!ctrl && Config.Keybind.Contains(VirtualKey.CONTROL))
            {
                set.Remove(VirtualKey.CONTROL);
            }

            Config.Keybind = set.Order().ToArray();
            Plugin.Config.Save();
        }

        ImGui.SameLine();

        var alt = Config.Keybind.Contains(VirtualKey.MENU);
        if (ImGui.Checkbox("Alt", ref alt))
        {
            var set = new HashSet<VirtualKey>(Config.Keybind);

            if (alt && !Config.Keybind.Contains(VirtualKey.MENU))
            {
                set.Add(VirtualKey.MENU);
            }
            if (!alt && Config.Keybind.Contains(VirtualKey.MENU))
            {
                set.Remove(VirtualKey.MENU);
            }

            Config.Keybind = set.Order().ToArray();
            Plugin.Config.Save();
        }

        ImGui.SameLine();

        var previewValue = "None";
        var hasKey = Config.Keybind.FindFirst(x => x is not (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT), out var key);
        if (hasKey)
            previewValue = key.GetFancyName();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().IndentSpacing);
        using var combo = ImRaii.Combo("##Key", previewValue);
        if (!combo.Success)
            return;

        foreach (var _key in Service.KeyState.GetValidVirtualKeys())
        {
            if (_key is VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT)
                continue;

            var _keySet = Config.Keybind.Contains(_key);
            if (ImGui.Selectable(_key.GetFancyName(), ref _keySet))
            {
                var set = new HashSet<VirtualKey>(Config.Keybind);

                // unset current key
                if (hasKey && Config.Keybind.Contains(key))
                {
                    set.Remove(key);
                }

                if (_keySet && !Config.Keybind.Contains(_key))
                {
                    set.Add(_key);
                }

                Config.Keybind = set.Order().ToArray();
                Plugin.Config.Save();
            }
        }
    }

    private static bool IsBgmMuted
    {
        get => Service.GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => Service.GameConfig.System.Set("IsSndBgm", value);
    }

    private bool _isPressingKeybind;

    public override void OnFrameworkUpdate(Framework framework)
    {
        if (!Config.Keybind.All(key => Service.KeyState[key]))
        {
            if (_isPressingKeybind)
                _isPressingKeybind = false;
            return;
        }

        // check if holding keys down
        if (_isPressingKeybind)
            return;

        var numKeysPressed = Service.KeyState.GetValidVirtualKeys().Count(key => Service.KeyState[key]);
        if (numKeysPressed == Config.Keybind.Length)
        {
            // prevents the game from handling the key press
            if (Config.Keybind.FindFirst(x => x is not (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT), out var key))
            {
                Service.KeyState[key] = false;
            }

            IsBgmMuted = !IsBgmMuted;

            HaselRaptureLogModule.Instance()->ShowLogMessageUInt(3861, IsBgmMuted ? 1u : 0u);
        }

        _isPressingKeybind = true;
    }
}
