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

public unsafe partial class CustomChatTimestamp(
    PluginConfig PluginConfig,
    ConfigGui ConfigGui,
    TextService TextService,
    ILogger<CustomChatTimestamp> Logger,
    IGameInteropProvider GameInteropProvider,
    IGameConfig GameConfig)
    : IConfigurableTweak
{
    public string InternalName => nameof(CustomChatTimestamp);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private Hook<HaselRaptureTextModule.Delegates.FormatAddonText2Int>? FormatAddonText2IntHook;

    public void OnInitialize()
    {
        FormatAddonText2IntHook = GameInteropProvider.HookFromAddress<HaselRaptureTextModule.Delegates.FormatAddonText2Int>(
            HaselRaptureTextModule.MemberFunctionPointers.FormatAddonText2Int,
            FormatAddonText2IntDetour);
    }

    public void OnEnable()
    {
        FormatAddonText2IntHook?.Enable();
        ReloadChat();
    }

    public void OnDisable()
    {
        FormatAddonText2IntHook?.Disable();
        ReloadChat();
    }

    public void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        FormatAddonText2IntHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
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
                Logger.LogError(e, "Error formatting Chat Timestamp");
            }
        }

        return FormatAddonText2IntHook!.Original(self, addonRowId, value);
    }

    private static void ReloadChat()
    {
        var raptureLogModule = RaptureLogModule.Instance();
        for (var i = 0; i < 4; i++)
            raptureLogModule->ChatTabIsPendingReload[i] = true;
    }
}
