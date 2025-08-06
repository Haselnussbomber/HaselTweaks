using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class EnhancedTargetInfo : ConfigurableTweak
{
    private readonly IGameInteropProvider _gameInteropProvider;
    private readonly PluginConfig _pluginConfig;
    private readonly ConfigGui _configGui;
    private readonly IClientState _clientState;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ISeStringEvaluator _seStringEvaluator;

    private Hook<AgentHUD.Delegates.UpdateTargetInfo>? _updateTargetInfoHook;
    private Hook<RaptureTextModule.Delegates.FormatAddonText2IntIntUInt>? _formatAddonText2IntIntUIntHook;

    private ReadOnlySeString? _rewrittenHealthPercentageText;

    public override void OnEnable()
    {
        if (_rewrittenHealthPercentageText == null && _excelService.TryGetRow<Addon>(2057, _clientState.ClientLanguage, out var row))
        {
            var builder = new SeStringBuilder();

            foreach (var payload in row.Text)
            {
                // Replace "<if([lnum1>0],<digit(lnum1,2)>,<num(lnum1)>)>" with just "<num(lnum1)>"
                if (payload.Type == ReadOnlySePayloadType.Macro && payload.MacroCode == MacroCode.If)
                {
                    builder.BeginMacro(MacroCode.Num).AppendLocalNumberExpression(1).EndMacro();
                    continue;
                }

                builder.Append(payload);
            }

            _rewrittenHealthPercentageText = builder.ToReadOnlySeString();
        }

        _updateTargetInfoHook = _gameInteropProvider.HookFromAddress<AgentHUD.Delegates.UpdateTargetInfo>(
            AgentHUD.MemberFunctionPointers.UpdateTargetInfo,
            UpdateTargetInfoDetour);

        _formatAddonText2IntIntUIntHook = _gameInteropProvider.HookFromAddress<RaptureTextModule.Delegates.FormatAddonText2IntIntUInt>(
            RaptureTextModule.MemberFunctionPointers.FormatAddonText2IntIntUInt,
            FormatAddonText2IntIntUIntDetour);

        _updateTargetInfoHook.Enable();
        _formatAddonText2IntIntUIntHook.Enable();
    }

    public override void OnDisable()
    {
        _updateTargetInfoHook?.Dispose();
        _updateTargetInfoHook = null;

        _formatAddonText2IntIntUIntHook?.Dispose();
        _formatAddonText2IntIntUIntHook = null;
    }

    private void UpdateTargetInfoDetour(AgentHUD* thisPtr)
    {
        _updateTargetInfoHook!.Original(thisPtr);

        if (Config.DisplayMountStatus || Config.DisplayOrnamentStatus)
            UpdateTargetInfoStatuses();
    }

    private void UpdateTargetInfoStatuses()
    {
        var target = TargetSystem.Instance()->GetTargetObject();
        if (target == null || target->GetObjectKind() != ObjectKind.Pc)
            return;

        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null || localPlayer->GetObjectKind() != ObjectKind.Pc)
            return;

        var chara = (BattleChara*)target;

        if (Config.DisplayMountStatus && chara->Mount.MountId != 0)
        {
            using var rssb = new RentedSeStringBuilder();
            var sb = rssb.Builder;

            sb.Append(_textService.GetMountName(chara->Mount.MountId));

            if (target->EntityId != localPlayer->EntityId)
            {
                sb.AppendNewLine();

                var isUnlocked = PlayerState.Instance()->IsMountUnlocked(chara->Mount.MountId);

                sb.PushColorType(isUnlocked ? 43u : 518);
                sb.Append(_textService.Translate(isUnlocked
                    ? "EnhancedTargetInfo.Unlocked"
                    : "EnhancedTargetInfo.NotUnlocked"));
                sb.PopColorType();
            }

            TargetStatusUtils.AddPermanentStatus(0, 216201, 0, 0, default, sb.ToReadOnlySeString());
        }
        else if (Config.DisplayOrnamentStatus && chara->OrnamentData.OrnamentId != 0)
        {
            using var rssb = new RentedSeStringBuilder();
            var sb = rssb.Builder;

            sb.Append(_textService.GetOrnamentName(chara->OrnamentData.OrnamentId));

            if (target->EntityId != localPlayer->EntityId)
            {
                sb.AppendNewLine();

                var isUnlocked = PlayerState.Instance()->IsOrnamentUnlocked(chara->OrnamentData.OrnamentId);

                sb.PushColorType(isUnlocked ? 43u : 518);
                sb.Append(_textService.Translate(isUnlocked
                    ? "EnhancedTargetInfo.Unlocked"
                    : "EnhancedTargetInfo.NotUnlocked"));
                sb.PopColorType();
            }

            TargetStatusUtils.AddPermanentStatus(0, 216234, 0, 0, default, sb.ToReadOnlySeString());
        }
    }

    private CStringPointer FormatAddonText2IntIntUIntDetour(RaptureTextModule* thisPtr, uint addonRowId, int value1, int value2, uint value3)
    {
        if (addonRowId == 2057 && Config.RemoveLeadingZeroInHPPercentage)
        {
            var str = thisPtr->UnkStrings1.GetPointer(1);
            str->SetString(_seStringEvaluator.Evaluate(_rewrittenHealthPercentageText!.Value, [value1, value2, value3], _clientState.ClientLanguage));
            return str->StringPtr;
        }

        return _formatAddonText2IntIntUIntHook!.Original(thisPtr, addonRowId, value1, value2, value3);
    }
}
