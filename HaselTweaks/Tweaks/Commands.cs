using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Commands;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class Commands : ConfigurableTweak<CommandsConfiguration>
{
    private readonly LanguageProvider _languageProvider;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly CommandService _commandService;
    private readonly ICondition _condition;
    private readonly IFramework _framework;

    private CommandHandler? _itemLinkCommandHandler;
    private CommandHandler? _whatMountCommandCommandHandler;
    private CommandHandler? _whatEmoteCommandCommandHandler;
    private CommandHandler? _whatBardingCommandCommandHandler;
    private CommandHandler? _glamourPlateCommandCommandHandler;
    private CommandHandler? _reloadUICommandCommandHandler;

    public override void OnEnable()
    {
        _itemLinkCommandHandler = _commandService.Register(OnItemLinkCommand);
        _whatMountCommandCommandHandler = _commandService.Register(OnWhatMountCommand);
        _whatEmoteCommandCommandHandler = _commandService.Register(OnWhatEmoteCommand);
        _whatBardingCommandCommandHandler = _commandService.Register(OnWhatBardingCommand);
        _glamourPlateCommandCommandHandler = _commandService.Register(OnGlamourPlateCommand);
        _reloadUICommandCommandHandler = _commandService.Register(OnReloadUICommand);

        UpdateCommands(true);
    }

    public override void OnDisable()
    {
        UpdateCommands(false);

        _itemLinkCommandHandler?.Dispose();
        _itemLinkCommandHandler = null;

        _whatMountCommandCommandHandler?.Dispose();
        _whatMountCommandCommandHandler = null;

        _whatEmoteCommandCommandHandler?.Dispose();
        _whatEmoteCommandCommandHandler = null;

        _whatBardingCommandCommandHandler?.Dispose();
        _whatBardingCommandCommandHandler = null;

        _glamourPlateCommandCommandHandler?.Dispose();
        _glamourPlateCommandCommandHandler = null;

        _reloadUICommandCommandHandler?.Dispose();
        _reloadUICommandCommandHandler = null;
    }

    private void UpdateCommands(bool enable)
    {
        _itemLinkCommandHandler?.SetEnabled(enable && _config.EnableItemLinkCommand);
        _whatMountCommandCommandHandler?.SetEnabled(enable && _config.EnableWhatMountCommand);
        _whatEmoteCommandCommandHandler?.SetEnabled(enable && _config.EnableWhatEmoteCommand);
        _whatBardingCommandCommandHandler?.SetEnabled(enable && _config.EnableWhatBardingCommand);
        _glamourPlateCommandCommandHandler?.SetEnabled(enable && _config.EnableGlamourPlateCommand);
        _reloadUICommandCommandHandler?.SetEnabled(enable && _config.EnableReloadUICommand);
    }

    [CommandHandler("/itemlink", "Commands.Config.EnableItemLinkCommand.Description", DisplayOrder: 2)]
    private void OnItemLinkCommand(string command, string arguments)
    {
        if (!uint.TryParse(arguments.Trim(), out var id))
        {
            Chat.PrintError(_textService.Translate("Commands.InvalidArguments"));
            return;
        }

        var isEventItem = ItemUtil.IsEventItem(id);
        var existsAsEventItem = isEventItem && _excelService.GetSheet<EventItem>().HasRow(id);
        var existsAsItem = !isEventItem && _excelService.GetSheet<Item>().HasRow(ItemUtil.GetBaseId(id).ItemId);

        if (!existsAsEventItem && !existsAsItem)
        {
            Chat.PrintError(_textService.Translate("Commands.ItemLink.ItemNotFound", id));
            return;
        }

        var idStr = new SeStringBuilder()
            .PushColorType(1)
            .Append(id)
            .PopColorType()
            .ToReadOnlySeString();

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix()
            .Append(_textService.TranslateSeString("Commands.ItemLink.Item", idStr, _itemService.GetItemLink(id)));

        Chat.Print(sb.GetViewAsSpan());
    }

    [CommandHandler("/whatmount", "Commands.Config.EnableWhatMountCommand.Description", DisplayOrder: 2)]
    private void OnWhatMountCommand(string command, string arguments)
    {
        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Chat.PrintError(_textService.Translate("Commands.NoTarget"));
            return;
        }

        if (target->GetObjectKind() != ObjectKind.Pc)
        {
            Chat.PrintError(_textService.Translate("Commands.TargetIsNotAPlayer"));
            return;
        }

        var character = (Character*)target;
        if (character->Mount.MountId == 0)
        {
            Chat.PrintError(_textService.Translate("Commands.WhatMount.TargetNotMounted"));
            return;
        }

        if (!_excelService.TryGetRow<Mount>(character->Mount.MountId, out var mount))
        {
            Chat.PrintError(_textService.Translate("Commands.WhatMount.MountNotFound"));
            return;
        }

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix();

        var name = new SeStringBuilder()
            .PushColorType(1)
            .Append(_textService.GetMountName(mount.RowId))
            .PopColorType()
            .ToReadOnlySeString();

        if (!_excelService.TryFindRow<ItemAction>(row => row.Type == 1322 && row.Data[0] == mount.RowId, out var itemAction) || itemAction.RowId == 0)
        {
            Chat.Print(sb.Append(_textService.TranslateSeString("Commands.WhatMount.WithoutItem", name)).GetViewAsSpan());
            return;
        }

        if (!_excelService.TryFindRow<Item>(row => row.ItemAction.RowId == itemAction.RowId, out var item))
        {
            Chat.Print(sb.Append(_textService.TranslateSeString("Commands.WhatMount.WithoutItem", name)).GetViewAsSpan());
            return;
        }

        Chat.Print(sb.Append(_textService.TranslateSeString("Commands.WhatMount.WithItem", name, _itemService.GetItemLink(item.RowId))).GetViewAsSpan());
    }

    [CommandHandler("/whatemote", "Commands.Config.EnableWhatEmoteCommand.Description", DisplayOrder: 2)]
    private void OnWhatEmoteCommand(string command, string arguments)
    {
        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Chat.PrintError(_textService.Translate("Commands.NoTarget"));
            return;
        }

        if (target->GetObjectKind() != ObjectKind.Pc)
        {
            Chat.PrintError(_textService.Translate("Commands.TargetIsNotAPlayer"));
            return;
        }

        var character = (Character*)target;
        var emoteId = character->EmoteController.EmoteId;
        if (emoteId == 0)
        {
            Chat.PrintError(_textService.Translate("Commands.Emote.NotExecutingEmote"));
            return;
        }

        if (!_excelService.TryGetRow<Emote>(emoteId, out var emote))
        {
            Chat.PrintError(_textService.Translate("Commands.Emote.NotFound", emoteId.ToString()));
            return;
        }

        Chat.Print(new SeStringBuilder()
                .AppendHaselTweaksPrefix()
                .Append(_textService.TranslateSeString("Commands.Emote", emoteId.ToString(), _textService.GetEmoteName(emoteId)))
                .GetViewAsSpan());
    }

    [CommandHandler("/whatbarding", "Commands.Config.EnableWhatBardingCommand.Description", DisplayOrder: 2)]
    private void OnWhatBardingCommand(string command, string arguments)
    {
        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            Chat.PrintError(_textService.Translate("Commands.NoTarget"));
            return;
        }

        if (target->GetObjectKind() != ObjectKind.BattleNpc || target->SubKind != 3) // BattleNpcSubKind.Chocobo
        {
            Chat.PrintError(_textService.Translate("Commands.TargetIsNotAChocobo"));
            return;
        }

        var character = (Character*)target;

        var hasTopRow = _excelService.TryFindRow<BuddyEquip>(row => row.ModelTop == (int)character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Value, out var topRow);
        var hasBodyRow = _excelService.TryFindRow<BuddyEquip>(row => row.ModelBody == (int)character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body).Value, out var bodyRow);
        var hasLegsRow = _excelService.TryFindRow<BuddyEquip>(row => row.ModelLegs == (int)character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet).Value, out var legsRow);

        var name = new SeStringBuilder()
            .PushColorType(1)
            .Append(character->GameObject.NameString)
            .PopColorType()
            .ToReadOnlySeString();

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix()
            .Append(_textService.TranslateSeString("Commands.WhatBarding.AppearanceOf", name))
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4987)}: ")
            .Append(_textService.GetStainName(character->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Legs).Stain0))
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4991)}: {(hasTopRow ? topRow.Name.ToString() : _textService.GetAddonText(4994))}")
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4992)}: {(hasBodyRow ? bodyRow.Name.ToString() : _textService.GetAddonText(4994))}")
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4993)}: {(hasLegsRow ? legsRow.Name.ToString() : _textService.GetAddonText(4994))}");

        Chat.Print(sb.GetViewAsSpan());
    }

    [CommandHandler("/glamourplate", "Commands.Config.EnableGlamourPlateCommand.Description", DisplayOrder: 2)]
    private void OnGlamourPlateCommand(string command, string arguments)
    {
        if (!byte.TryParse(arguments.Trim(), out var glamourPlateId) || glamourPlateId == 0 || glamourPlateId > 20)
        {
            Chat.PrintError(_textService.Translate("Commands.InvalidArguments"));
            return;
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (!raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex))
        {
            Chat.PrintError(_textService.Translate("Commands.GlamourPlate.InvalidGearset"));
            return;
        }

        raptureGearsetModule->EquipGearset(raptureGearsetModule->CurrentGearsetIndex, glamourPlateId);
    }

    [CommandHandler("/reloadui", "Commands.Config.EnableReloadUICommand.Description")]
    private unsafe void OnReloadUICommand(string command, string arguments)
    {
        var raptureAtkModule = RaptureAtkModule.Instance();

        if (raptureAtkModule->UiMode != 1 || !_condition.OnlyAny(
            ConditionFlag.NormalConditions,
            ConditionFlag.InThatPosition,
            ConditionFlag.Jumping,
            ConditionFlag.Mounted,
            ConditionFlag.InFlight,
            ConditionFlag.UsingFashionAccessory,
            ConditionFlag.OnFreeTrial))
        {
            Chat.PrintError(_textService.GetLogMessage(10775));
            return;
        }

        // Note: The game calls Hide on all agents, which causes AgentHousingPlant to unblock inventory slots.
        // This triggers an inventory/shop/retainer/recipe, and most importantly, a map markers update.
        // For that, EventFramework sends a packet and the server responds with a packet, triggering the map markers update.
        // This seems to be a very normal and frequent thing to occur, with a back-off of 2 seconds, so nothing to worry about.
        raptureAtkModule->ChangeUiMode(0);
        _framework.RunOnTick(() => raptureAtkModule->ChangeUiMode(1));
    }
}
