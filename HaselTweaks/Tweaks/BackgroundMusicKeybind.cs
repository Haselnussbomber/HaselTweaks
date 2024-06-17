using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks.Tweaks;

public sealed class BackgroundMusicKeybindConfiguration
{
    public VirtualKey[] Keybind = [VirtualKey.CONTROL, VirtualKey.M];
}

public sealed unsafe class BackgroundMusicKeybind(
    Configuration PluginConfig,
    TranslationManager TranslationManager,
    IGameConfig GameConfig,
    IKeyState KeyState,
    IFramework Framework)
    : Tweak<BackgroundMusicKeybindConfiguration>(PluginConfig, TranslationManager)
{
    private bool IsBgmMuted
    {
        get => GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => GameConfig.System.Set("IsSndBgm", value);
    }

    private bool _isPressingKeybind;

    public override void OnEnable()
    {
        Framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var allKeybindsPressed = true;

        foreach (var key in Config.Keybind)
            allKeybindsPressed &= KeyState[key];

        if (!allKeybindsPressed)
        {
            if (_isPressingKeybind)
                _isPressingKeybind = false;
            return;
        }

        // check if holding keys down
        if (_isPressingKeybind)
            return;

        var numKeysPressed = KeyState.GetValidVirtualKeys().Count(key => KeyState[key]);
        if (numKeysPressed == Config.Keybind.Length)
        {
            // prevents the game from handling the key press
            if (Config.Keybind.FindFirst(x => x is not (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT), out var key))
            {
                KeyState[key] = false;
            }

            IsBgmMuted = !IsBgmMuted;

            RaptureLogModule.Instance()->ShowLogMessageUInt(3861, IsBgmMuted ? 1u : 0u);
        }

        _isPressingKeybind = true;
    }

    public override void DrawConfig()
    {
        ImGuiUtils.DrawSection(t("HaselTweaks.Config.SectionTitle.Configuration"));

        var shift = Config.Keybind.Contains(VirtualKey.SHIFT);
        if (ImGui.Checkbox(t("BackgroundMusicKeybind.Config.ShiftKeyCheckbox.Label"), ref shift))
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

            Config.Keybind = [.. set.Order()];
            PluginConfig.Save();
        }

        ImGui.SameLine();

        var ctrl = Config.Keybind.Contains(VirtualKey.CONTROL);
        if (ImGui.Checkbox(t("BackgroundMusicKeybind.Config.ControlKeyCheckbox.Label"), ref ctrl))
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

            Config.Keybind = [.. set.Order()];
            PluginConfig.Save();
        }

        ImGui.SameLine();

        var alt = Config.Keybind.Contains(VirtualKey.MENU);
        if (ImGui.Checkbox(t("BackgroundMusicKeybind.Config.AltKeyCheckbox.Label"), ref alt))
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

            Config.Keybind = [.. set.Order()];
            PluginConfig.Save();
        }

        ImGui.SameLine();

        var previewValue = t("BackgroundMusicKeybind.Config.KeyCombo.Preview.None");
        var hasKey = Config.Keybind.FindFirst(x => x is not (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT), out var key);
        if (hasKey)
            previewValue = key.GetFancyName();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        using var combo = ImRaii.Combo("##Key", previewValue);
        if (!combo.Success)
            return;

        foreach (var _key in KeyState.GetValidVirtualKeys())
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

                Config.Keybind = [.. set.Order()];
                PluginConfig.Save();
            }
        }
    }
}
