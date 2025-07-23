using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class BackgroundMusicKeybind : ConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IGameConfig _gameConfig;
    private readonly IKeyState _keyState;
    private readonly IFramework _framework;

    private bool _isPressingKeybind;

    private bool IsBgmMuted
    {
        get => _gameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => _gameConfig.System.Set("IsSndBgm", value);
    }

    public override void OnEnable()
    {
        _framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        _framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var allKeybindsPressed = true;

        foreach (var key in Config.Keybind)
            allKeybindsPressed &= _keyState[key];

        if (!allKeybindsPressed)
        {
            if (_isPressingKeybind)
                _isPressingKeybind = false;
            return;
        }

        // check if holding keys down
        if (_isPressingKeybind)
            return;

        var numKeysPressed = _keyState.GetValidVirtualKeys().Count(key => _keyState[key]);
        if (numKeysPressed == Config.Keybind.Length)
        {
            // prevents the game from handling the key press
            if (Config.Keybind.TryGetFirst(x => x is not (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT), out var key))
            {
                _keyState[key] = false;
            }

            IsBgmMuted = !IsBgmMuted;

            RaptureLogModule.Instance()->ShowLogMessageUInt(3861, IsBgmMuted ? 1u : 0u);
        }

        _isPressingKeybind = true;
    }
}
