using System.Collections.Generic;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace HaselTweaks.Utils;

public static unsafe class GamepadUtils
{
    public static Dictionary<GamepadButtons, string> ButtonConfigMapping { get; } = new()
    {
        [GamepadButtons.North] = "PadButton_Triangle",
        [GamepadButtons.East] = "PadButton_Circle",
        [GamepadButtons.South] = "PadButton_Cross",
        [GamepadButtons.West] = "PadButton_Square",
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
        for (var i = 0; i < systemConfigBase.ConfigCount; i++)
        {
            var entry = systemConfigBase.ConfigEntry[i];
            if (entry.Type != 4 || entry.Name == null || entry.Value.String == null)
                continue;

            var name = MemoryHelper.ReadStringNullTerminated((nint)entry.Name);
            if (string.IsNullOrEmpty(name))
                continue;

            foreach (var kv in ButtonConfigMapping)
            {
                // check config name
                if (name != kv.Value)
                    continue;

                // check config value
                if (entry.Value.String->ToString() == binding.ToString())
                    return kv.Key;
            }
        }
        return GamepadButtons.South; // Default
    }

    public static bool IsPressed(GamepadBinding binding)
    {
        return Service.GamepadState.Pressed(GetButton(binding)) == 1;
    }
}
