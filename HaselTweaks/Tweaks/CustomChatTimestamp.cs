using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;
using Microsoft.Extensions.Logging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CustomChatTimestamp : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;
    private readonly ILogger<CustomChatTimestamp> _logger;
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IGameConfig _gameConfig;

    private Hook<HaselRaptureTextModule.Delegates.FormatAddonText2Int>? _formatAddonText2IntHook;

    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _formatAddonText2IntHook = _gameInteropProvider.HookFromAddress<HaselRaptureTextModule.Delegates.FormatAddonText2Int>(
            HaselRaptureTextModule.MemberFunctionPointers.FormatAddonText2Int,
            FormatAddonText2IntDetour);
    }

    public void OnEnable()
    {
        _formatAddonText2IntHook?.Enable();
        ReloadChat();
    }

    public void OnDisable()
    {
        _formatAddonText2IntHook?.Disable();

        if (Status is TweakStatus.Enabled)
            ReloadChat();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _formatAddonText2IntHook?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private byte* FormatAddonText2IntDetour(HaselRaptureTextModule* self, uint addonRowId, int value)
    {
        if (addonRowId is 7840 or 7841 && !string.IsNullOrWhiteSpace(Config.Format))
        {
            try
            {
                var str = ((RaptureTextModule*)self)->UnkStrings1.GetPointer(1);
                str->SetString(DateTimeOffset.FromUnixTimeSeconds(value).ToLocalTime().ToString(Config.Format));
                return str->StringPtr;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error formatting Chat Timestamp");
            }
        }

        return _formatAddonText2IntHook!.Original(self, addonRowId, value);
    }

    private static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }
}
