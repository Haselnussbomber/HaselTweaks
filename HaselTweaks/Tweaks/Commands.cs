using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Commands;
using HaselCommon.Extensions.Strings;
using HaselCommon.Services;
using HaselCommon.Utils;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Extensions;
using HaselTweaks.Interfaces;
using Lumina.Excel.Sheets;
using Lumina.Text;
using BattleNpcSubKind = Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind;
using DalamudObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class Commands : IConfigurableTweak
{
    private readonly PluginConfig _pluginConfig;
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly CommandService _commandService;
    private readonly IChatGui _chatGui;
    private readonly ITargetManager _targetManager;
    private readonly ConfigGui _configGui;

    private CommandHandler? _itemLinkCommandHandler;
    private CommandHandler? _whatMountCommandCommandHandler;
    private CommandHandler? _whatEmoteCommandCommandHandler;
    private CommandHandler? _whatBardingCommandCommandHandler;
    private CommandHandler? _glamourPlateCommandCommandHandler;

    public string InternalName => nameof(Commands);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        _itemLinkCommandHandler = _commandService.Register(OnItemLinkCommand);
        _whatMountCommandCommandHandler = _commandService.Register(OnWhatMountCommand);
        _whatEmoteCommandCommandHandler = _commandService.Register(OnWhatEmoteCommand);
        _whatBardingCommandCommandHandler = _commandService.Register(OnWhatBardingCommand);
        _glamourPlateCommandCommandHandler = _commandService.Register(OnGlamourPlateCommand);
    }

    public void OnEnable()
    {
        UpdateCommands(true);
    }

    public void OnDisable()
    {
        UpdateCommands(false);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        _itemLinkCommandHandler?.Dispose();
        _whatMountCommandCommandHandler?.Dispose();
        _whatEmoteCommandCommandHandler?.Dispose();
        _whatBardingCommandCommandHandler?.Dispose();
        _glamourPlateCommandCommandHandler?.Dispose();

        Status = TweakStatus.Disposed;
    }

    private void UpdateCommands(bool enable)
    {
        _itemLinkCommandHandler?.SetEnabled(enable && Config.EnableItemLinkCommand);
        _whatMountCommandCommandHandler?.SetEnabled(enable && Config.EnableWhatMountCommand);
        _whatEmoteCommandCommandHandler?.SetEnabled(enable && Config.EnableWhatEmoteCommand);
        _whatBardingCommandCommandHandler?.SetEnabled(enable && Config.EnableWhatBardingCommand);
        _glamourPlateCommandCommandHandler?.SetEnabled(enable && Config.EnableGlamourPlateCommand);
    }

    [CommandHandler("/itemlink", "Commands.Config.EnableItemLinkCommand.Description", DisplayOrder: 2)]
    private void OnItemLinkCommand(string command, string arguments)
    {
        ExcelRowId<Item> id;
        try
        {
            id = Convert.ToUInt32(arguments.Trim());
        }
        catch (Exception e)
        {
            _chatGui.PrintError(e.Message);
            return;
        }

        var isEventItem = id.IsEventItem();
        var existsAsEventItem = isEventItem && _excelService.GetSheet<EventItem>().HasRow(id);
        var existsAsItem = !isEventItem && _excelService.GetSheet<Item>().HasRow(id.GetBaseId());

        if (!existsAsEventItem && !existsAsItem)
        {
            _chatGui.PrintError(_textService.Translate("Commands.ItemLink.ItemNotFound", id));
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

        _chatGui.Print(new XivChatEntry
        {
            MessageBytes = sb.ToArray(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/whatmount", "Commands.Config.EnableWhatMountCommand.Description", DisplayOrder: 2)]
    private void OnWhatMountCommand(string command, string arguments)
    {
        var target = (Character*)(_targetManager.Target?.Address ?? 0);
        if (target == null)
        {
            _chatGui.PrintError(_textService.Translate("Commands.NoTarget"));
            return;
        }

        if (target->GameObject.GetObjectKind() != ObjectKind.Pc)
        {
            _chatGui.PrintError(_textService.Translate("Commands.TargetIsNotAPlayer"));
            return;
        }

        if (target->Mount.MountId == 0)
        {
            _chatGui.PrintError(_textService.Translate("Commands.WhatMount.TargetNotMounted"));
            return;
        }

        if (!_excelService.TryGetRow<Mount>(target->Mount.MountId, out var mount))
        {
            _chatGui.PrintError(_textService.Translate("Commands.WhatMount.MountNotFound"));
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
            _chatGui.Print(new XivChatEntry
            {
                MessageBytes = sb
                    .Append(_textService.TranslateSeString("Commands.WhatMount.WithoutItem", name))
                    .ToArray(),
                Type = XivChatType.Echo
            });
            return;
        }

        if (!_excelService.TryFindRow<Item>(row => row.ItemAction.RowId == itemAction.RowId, out var item))
        {
            _chatGui.Print(new XivChatEntry
            {
                MessageBytes = sb
                    .Append(_textService.TranslateSeString("Commands.WhatMount.WithoutItem", name))
                    .ToArray(),
                Type = XivChatType.Echo
            });
            return;
        }

        sb.Append(_textService.TranslateSeString("Commands.WhatMount.WithItem", name, _itemService.GetItemLink(item.RowId)));

        _chatGui.Print(new XivChatEntry
        {
            MessageBytes = sb.ToArray(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/whatemote", "Commands.Config.EnableWhatEmoteCommand.Description", DisplayOrder: 2)]
    private void OnWhatEmoteCommand(string command, string arguments)
    {
        var target = _targetManager.Target;
        if (target == null)
        {
            _chatGui.PrintError(_textService.Translate("Commands.NoTarget"));
            return;
        }

        if (target.ObjectKind != DalamudObjectKind.Player)
        {
            _chatGui.PrintError(_textService.Translate("Commands.TargetIsNotAPlayer"));
            return;
        }

        var gameObject = (Character*)target.Address;

        var emoteId = gameObject->EmoteController.EmoteId;
        if (emoteId == 0)
        {
            _chatGui.PrintError(_textService.Translate("Commands.Emote.NotExecutingEmote"));
            return;
        }

        if (!_excelService.TryGetRow<Emote>(emoteId, out var emote))
        {
            _chatGui.PrintError(_textService.Translate("Commands.Emote.NotFound", emoteId.ToString()));
            return;
        }

        _chatGui.Print(new XivChatEntry
        {
            MessageBytes = new SeStringBuilder()
                .AppendHaselTweaksPrefix()
                .Append(_textService.TranslateSeString("Commands.Emote", emoteId.ToString(), _textService.GetEmoteName(emoteId)))
                .ToArray(),
            Type = XivChatType.Echo
        });
    }
    [CommandHandler("/whatbarding", "Commands.Config.EnableWhatBardingCommand.Description", DisplayOrder: 2)]
    private void OnWhatBardingCommand(string command, string arguments)
    {
        var target = _targetManager.Target;
        if (target == null)
        {
            _chatGui.PrintError(_textService.Translate("Commands.NoTarget"));
            return;
        }

        if (target.ObjectKind != DalamudObjectKind.BattleNpc || target.SubKind != (byte)BattleNpcSubKind.Chocobo)
        {
            _chatGui.PrintError(_textService.Translate("Commands.TargetIsNotAChocobo"));
            return;
        }

        var targetCharacter = (Character*)target.Address;

        var hasTopRow = _excelService.TryFindRow<BuddyEquip>(row => row.ModelTop == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Value, out var topRow);
        var hasBodyRow = _excelService.TryFindRow<BuddyEquip>(row => row.ModelBody == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body).Value, out var bodyRow);
        var hasLegsRow = _excelService.TryFindRow<BuddyEquip>(row => row.ModelLegs == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet).Value, out var legsRow);

        _excelService.TryGetRow<Stain>(targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Legs).Stain0, out var stain);

        var name = new SeStringBuilder()
            .PushColorType(1)
            .Append(targetCharacter->GameObject.NameString)
            .PopColorType()
            .ToReadOnlySeString();

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix()
            .Append(_textService.TranslateSeString("Commands.WhatBarding.AppearanceOf", name))
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4987)}: ")
            .Append(stain.Name.ExtractText().FirstCharToUpper())
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4991)}: {(hasTopRow ? topRow.Name.ExtractText() : _textService.GetAddonText(4994))}")
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4992)}: {(hasBodyRow ? bodyRow.Name.ExtractText() : _textService.GetAddonText(4994))}")
            .AppendNewLine()
            .Append($"  {_textService.GetAddonText(4993)}: {(hasLegsRow ? legsRow.Name.ExtractText() : _textService.GetAddonText(4994))}");

        _chatGui.Print(new XivChatEntry
        {
            MessageBytes = sb.ToArray(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/glamourplate", "Commands.Config.EnableGlamourPlateCommand.Description", DisplayOrder: 2)]
    private void OnGlamourPlateCommand(string command, string arguments)
    {
        if (!byte.TryParse(arguments, out var glamourPlateId) || glamourPlateId == 0 || glamourPlateId > 20)
        {
            _chatGui.PrintError(_textService.Translate("Commands.InvalidArguments"));
            return;
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (!raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex))
        {
            _chatGui.PrintError(_textService.Translate("Commands.GlamourPlate.InvalidGearset"));
            return;
        }

        raptureGearsetModule->EquipGearset(raptureGearsetModule->CurrentGearsetIndex, glamourPlateId);
    }
}
