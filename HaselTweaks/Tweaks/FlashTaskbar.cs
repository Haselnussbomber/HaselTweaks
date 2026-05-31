using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class FlashTaskbar : ConfigurableTweak<FlashTaskbarConfiguration>
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly IChatGui _chatGui;
    private readonly ICondition _condition;

    private Hook<AtkModuleInterface.AtkEventInterface.Delegates.ReceiveEvent>? _normalCraftCallbackHook;

    public override void OnEnable()
    {
        _chatGui.LogMessage += OnLogMessage;
        _condition.ConditionChange += OnConditionChange;

        // Client::Game::Event::NormalCraftCallback.ReceiveEvent
        _normalCraftCallbackHook = _gameInteropProvider.HookFromSignature<AtkModuleInterface.AtkEventInterface.Delegates.ReceiveEvent>(
            "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC ?? 49 8B F0 48 8B FA 4C 8B F1 45 85 C9",
            NormalCraftCallbackDetour);

        _normalCraftCallbackHook.Enable();
    }

    public override void OnDisable()
    {
        _chatGui.LogMessage -= OnLogMessage;
        _condition.ConditionChange -= OnConditionChange;

        _normalCraftCallbackHook?.Dispose();
        _normalCraftCallbackHook = null;
    }

    private void OnLogMessage(ILogMessage message)
    {
        if (_config.FlashOnAlarm && message.LogMessageId == 3906)
        {
            Flash("Alarm!");
        }
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (_config.FlashOnCombat && flag == ConditionFlag.InCombat && value)
        {
            Flash("Combat started!");
        }
    }

    private AtkValue* NormalCraftCallbackDetour(AtkModuleInterface.AtkEventInterface* thisPtr, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind)
    {
        // status values:
        // -1 = cancelled
        // -2 = completed

        if (_config.FlashOnCraftEnd && valueCount > 0 && values[0].TryGetInt(out var status) && status == -2)
        {
            Flash("Crafting ended!");
        }

        return _normalCraftCallbackHook!.Original(thisPtr, returnValue, values, valueCount, eventKind);
    }

    private void Flash(string? reason = null)
    {
        var framework = Framework.Instance();
        if (framework == null || framework->GameWindow == null || !framework->WindowInactive)
            return;

        if (reason != null)
            _logger.LogInformation("{reason} Flashing taskbar...", reason);

        PInvoke.FlashWindowEx(new FLASHWINFO()
        {
            cbSize = (uint)sizeof(FLASHWINFO),
            uCount = uint.MaxValue,
            dwTimeout = 0,
            dwFlags = FLASHWINFO_FLAGS.FLASHW_ALL | FLASHWINFO_FLAGS.FLASHW_TIMERNOFG,
            hwnd = (HWND)framework->GameWindow->WindowHandle,
        });
    }
}
