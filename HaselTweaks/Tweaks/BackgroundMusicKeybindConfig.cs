using Dalamud.Game.ClientState.Keys;

namespace HaselTweaks.Tweaks;

public class BackgroundMusicKeybindConfiguration
{
    public VirtualKey[] Keybind = [VirtualKey.CONTROL, VirtualKey.M];
}

public unsafe partial class BackgroundMusicKeybind
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();

        var shift = _config.Keybind.Contains(VirtualKey.SHIFT);
        if (ImGui.Checkbox(_textService.Translate("BackgroundMusicKeybind.Config.ShiftKeyCheckbox.Label"), ref shift))
        {
            var set = new HashSet<VirtualKey>(_config.Keybind);

            if (shift && !_config.Keybind.Contains(VirtualKey.SHIFT))
            {
                set.Add(VirtualKey.SHIFT);
            }
            if (!shift && _config.Keybind.Contains(VirtualKey.SHIFT))
            {
                set.Remove(VirtualKey.SHIFT);
            }

            _config.Keybind = [.. set.Order()];
            _pluginConfig.Save();
        }

        ImGui.SameLine();

        var ctrl = _config.Keybind.Contains(VirtualKey.CONTROL);
        if (ImGui.Checkbox(_textService.Translate("BackgroundMusicKeybind.Config.ControlKeyCheckbox.Label"), ref ctrl))
        {
            var set = new HashSet<VirtualKey>(_config.Keybind);

            if (ctrl && !_config.Keybind.Contains(VirtualKey.CONTROL))
            {
                set.Add(VirtualKey.CONTROL);
            }
            if (!ctrl && _config.Keybind.Contains(VirtualKey.CONTROL))
            {
                set.Remove(VirtualKey.CONTROL);
            }

            _config.Keybind = [.. set.Order()];
            _pluginConfig.Save();
        }

        ImGui.SameLine();

        var alt = _config.Keybind.Contains(VirtualKey.MENU);
        if (ImGui.Checkbox(_textService.Translate("BackgroundMusicKeybind.Config.AltKeyCheckbox.Label"), ref alt))
        {
            var set = new HashSet<VirtualKey>(_config.Keybind);

            if (alt && !_config.Keybind.Contains(VirtualKey.MENU))
            {
                set.Add(VirtualKey.MENU);
            }
            if (!alt && _config.Keybind.Contains(VirtualKey.MENU))
            {
                set.Remove(VirtualKey.MENU);
            }

            _config.Keybind = [.. set.Order()];
            _pluginConfig.Save();
        }

        ImGui.SameLine();

        var previewValue = _textService.Translate("BackgroundMusicKeybind.Config.KeyCombo.Preview.None");
        var hasKey = _config.Keybind.TryGetFirst(x => x is not (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT), out var key);
        if (hasKey)
            previewValue = key.GetFancyName();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        using var combo = ImRaii.Combo("##Key", previewValue);
        if (!combo)
            return;

        foreach (var _key in _keyState.GetValidVirtualKeys())
        {
            if (_key is VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT)
                continue;

            var _keySet = _config.Keybind.Contains(_key);
            if (ImGui.Selectable(_key.GetFancyName(), ref _keySet))
            {
                var set = new HashSet<VirtualKey>(_config.Keybind);

                // unset current key
                if (hasKey && _config.Keybind.Contains(key))
                {
                    set.Remove(key);
                }

                if (_keySet && !_config.Keybind.Contains(_key))
                {
                    set.Add(_key);
                }

                _config.Keybind = [.. set.Order()];
                _pluginConfig.Save();
            }
        }
    }
}
