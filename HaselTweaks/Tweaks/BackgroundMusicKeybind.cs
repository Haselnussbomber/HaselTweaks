using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Extensions;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

public unsafe partial class BackgroundMusicKeybind(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    IGameConfig GameConfig,
    IKeyState KeyState,
    IFramework Framework)
    : IConfigurableTweak
{
    public string InternalName => nameof(BackgroundMusicKeybind);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private bool IsBgmMuted
    {
        get => GameConfig.System.TryGet("IsSndBgm", out bool value) && value;
        set => GameConfig.System.Set("IsSndBgm", value);
    }

    private bool _isPressingKeybind;

    public void OnInitialize() { }

    public void OnEnable()
    {
        Framework.Update += OnFrameworkUpdate;
    }

    public void OnDisable()
    {
        Framework.Update -= OnFrameworkUpdate;
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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
}
