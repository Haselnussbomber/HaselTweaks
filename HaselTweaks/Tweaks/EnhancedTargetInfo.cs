using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs.Agents;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedTargetInfo : IConfigurableTweak
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly TextService _textService;

    private Hook<HaselAgentHUD.Delegates.UpdateTargetInfo> _hook;

    public string InternalName => nameof(EnhancedTargetInfo);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _hook = _gameInteropProvider.HookFromAddress<HaselAgentHUD.Delegates.UpdateTargetInfo>(
            HaselAgentHUD.MemberFunctionPointers.UpdateTargetInfo,
            UpdateTargetInfoDetour);
    }

    public void OnEnable()
    {
        _hook.Enable();
    }

    public void OnDisable()
    {
        _hook.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _hook.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void UpdateTargetInfoDetour(HaselAgentHUD* thisPtr)
    {
        _hook.Original(thisPtr);
        UpdateTargetInfoStatuses();
    }

    private void UpdateTargetInfoStatuses()
    {
        var target = TargetSystem.Instance()->GetTargetObject();
        if (target == null || target->GetObjectKind() != ObjectKind.Pc)
            return;

        var chara = (BattleChara*)target;

        if (Config.DisplayMountStatus && chara->Mount.MountId != 0)
        {
            TargetStatusUtils.AddPermanentStatus(0, 216201, 0, 0, default, _textService.GetMountName(chara->Mount.MountId));
        }
        else if (Config.DisplayOrnamentStatus && chara->OrnamentData.OrnamentId != 0)
        {
            TargetStatusUtils.AddPermanentStatus(0, 216234, 0, 0, default, _textService.GetOrnamentName(chara->OrnamentData.OrnamentId));
        }
    }
}
