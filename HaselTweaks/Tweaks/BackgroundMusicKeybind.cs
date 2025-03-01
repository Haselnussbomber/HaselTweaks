using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using Lumina.Extensions;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class BackgroundMusicKeybind : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly IGameConfig _gameConfig;
    private readonly IKeyState _keyState;
    private readonly IFramework _framework;

    private bool _isPressingKeybind;

    public string InternalName => nameof(BackgroundMusicKeybind);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private bool IsBgmMuted
    {
        get => _gameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => _gameConfig.System.Set("IsSndBgm", value);
    }

    public void OnInitialize() { }

    public void OnEnable()
    {
        _framework.Update += OnFrameworkUpdate;
    }

    public void OnDisable()
    {
        _framework.Update -= OnFrameworkUpdate;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
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
