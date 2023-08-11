using Dalamud.Game.ClientState.GamePad;

namespace HaselTweaks.Utils;

public static unsafe class GamepadUtils
{
    // Mapping between SystemConfigOption and Dalamuds GamepadButtons
    private static readonly (string, GamepadButtons)[] Mapping = new[]
    {
        ("PadButton_Triangle", GamepadButtons.North),
        ("PadButton_Circle", GamepadButtons.East),
        ("PadButton_Cross", GamepadButtons.South),
        ("PadButton_Square", GamepadButtons.West)
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
        var bindingName = binding.ToString();

        foreach (var (configOption, gamepadButton) in Mapping)
        {
            if (!Service.GameConfig.System.TryGet(configOption, out string value))
                continue;

            if (value == bindingName)
                return gamepadButton;
        }

        return GamepadButtons.South; // Default
    }

    public static bool IsPressed(GamepadBinding binding)
        => Service.GamepadState.Pressed(GetButton(binding)) == 1;
}
