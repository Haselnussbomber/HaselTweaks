using System.Collections.Generic;
using Dalamud.Game.ClientState.GamePad;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks;

public static unsafe class GamepadUtils
{
    public static Dictionary<GamepadButtons, ConfigOption> ButtonConfigMapping = new()
    {
        [GamepadButtons.North] = ConfigOption.PadButton_Triangle,
        [GamepadButtons.East] = ConfigOption.PadButton_Circle,
        [GamepadButtons.South] = ConfigOption.PadButton_Cross,
        [GamepadButtons.West] = ConfigOption.PadButton_Square,
    };

    public enum GamepadBinding
    {
        Jump,
        Accept,
        Cancel,
        Map_Sub,
        MainCommand,
        HUD_Select
    }

    public static GamepadButtons GetButton(GamepadBinding binding)
    {
        var systemConfigBase = Framework.Instance()->SystemConfig.CommonSystemConfig.ConfigBase;
        foreach (var kv in ButtonConfigMapping)
        {
            var entry = systemConfigBase.ConfigEntry[(int)kv.Value];
            if (entry.Value.String != null && entry.Value.String->ToString() == binding.ToString())
            {
                return kv.Key;
            }
        }
        return GamepadButtons.South; // Default
    }
}
