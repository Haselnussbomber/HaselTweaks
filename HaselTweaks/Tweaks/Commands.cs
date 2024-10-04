using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselCommon.Commands;
using HaselCommon.Extensions.Strings;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.Enums;
using HaselTweaks.Extensions;
using HaselTweaks.Interfaces;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using BattleNpcSubKind = Dalamud.Game.ClientState.Objects.Enums.BattleNpcSubKind;
using DalamudObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselTweaks.Tweaks;

public unsafe partial class Commands(
    PluginConfig PluginConfig,
    TextService TextService,
    ExcelService ExcelService,
    ItemService ItemService,
    CommandService CommandService,
    IChatGui ChatGui,
    ITargetManager TargetManager,
    ConfigGui ConfigGui)
    : IConfigurableTweak
{
    public string InternalName => nameof(Commands);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private CommandHandler? ItemLinkCommandHandler;
    private CommandHandler? WhatMountCommandCommandHandler;
    private CommandHandler? WhatEmoteCommandCommandHandler;
    private CommandHandler? WhatBardingCommandCommandHandler;
    private CommandHandler? GlamourPlateCommandCommandHandler;

    public void OnInitialize()
    {
        ItemLinkCommandHandler = CommandService.Register(OnItemLinkCommand);
        WhatMountCommandCommandHandler = CommandService.Register(OnWhatMountCommand);
        WhatEmoteCommandCommandHandler = CommandService.Register(OnWhatEmoteCommand);
        WhatBardingCommandCommandHandler = CommandService.Register(OnWhatBardingCommand);
        GlamourPlateCommandCommandHandler = CommandService.Register(OnGlamourPlateCommand);
    }

    public void OnEnable()
    {
        UpdateCommands(true);
    }

    public void OnDisable()
    {
        UpdateCommands(false);
    }

    public void Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        ItemLinkCommandHandler?.Dispose();
        WhatMountCommandCommandHandler?.Dispose();
        WhatEmoteCommandCommandHandler?.Dispose();
        WhatBardingCommandCommandHandler?.Dispose();
        GlamourPlateCommandCommandHandler?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void UpdateCommands(bool enable)
    {
        ItemLinkCommandHandler?.SetEnabled(enable && Config.EnableItemLinkCommand);
        WhatMountCommandCommandHandler?.SetEnabled(enable && Config.EnableWhatMountCommand);
        WhatEmoteCommandCommandHandler?.SetEnabled(enable && Config.EnableWhatEmoteCommand);
        WhatBardingCommandCommandHandler?.SetEnabled(enable && Config.EnableWhatBardingCommand);
        GlamourPlateCommandCommandHandler?.SetEnabled(enable && Config.EnableGlamourPlateCommand);
    }

    [CommandHandler("/itemlink", "Commands.Config.EnableItemLinkCommand.Description")]
    private void OnItemLinkCommand(string command, string arguments)
    {
        uint id;
        try
        {
            id = Convert.ToUInt32(arguments.Trim());
        }
        catch (Exception e)
        {
            ChatGui.PrintError(e.Message);
            return;
        }

        var isEventItem = ItemService.IsEventItem(id);
        var existsAsEventItem = isEventItem && ExcelService.GetRow<EventItem>(id) != null;
        var existsAsItem = !isEventItem && ExcelService.GetRow<Item>(ItemService.GetBaseItemId(id)) != null;

        if (!existsAsEventItem && !existsAsItem)
        {
            ChatGui.PrintError(TextService.Translate("Commands.ItemLink.ItemNotFound", id));
            return;
        }

        var idStr = new SeStringBuilder()
            .PushColorType(1)
            .Append(id)
            .PopColorType()
            .ToReadOnlySeString();

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix()
            .Append(TextService.TranslateSeString("Commands.ItemLink.Item", idStr, ItemService.GetItemLink(id)));

        ChatGui.Print(new XivChatEntry
        {
            Message = sb.ToDalamudString(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/whatmount", "Commands.Config.EnableWhatMountCommand.Description")]
    private void OnWhatMountCommand(string command, string arguments)
    {
        var target = (Character*)(TargetManager.Target?.Address ?? 0);
        if (target == null)
        {
            ChatGui.PrintError(TextService.Translate("Commands.NoTarget"));
            return;
        }

        if (target->GameObject.GetObjectKind() != ObjectKind.Pc)
        {
            ChatGui.PrintError(TextService.Translate("Commands.TargetIsNotAPlayer"));
            return;
        }

        if (target->Mount.MountId == 0)
        {
            ChatGui.PrintError(TextService.Translate("Commands.WhatMount.TargetNotMounted"));
            return;
        }

        var mount = ExcelService.GetRow<Mount>(target->Mount.MountId);
        if (mount == null)
        {
            ChatGui.PrintError(TextService.Translate("Commands.WhatMount.MountNotFound"));
            return;
        }

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix();

        var name = new SeStringBuilder()
            .PushColorType(1)
            .Append(TextService.GetMountName(mount.RowId))
            .PopColorType()
            .ToReadOnlySeString();

        var itemAction = ExcelService.FindRow<ItemAction>(row => row?.Type == 1322 && row.Data[0] == mount.RowId);
        if (itemAction == null || itemAction.RowId == 0)
        {
            ChatGui.Print(new XivChatEntry
            {
                Message = sb
                    .Append(TextService.TranslateSeString("Commands.WhatMount.WithoutItem", name))
                    .ToDalamudString(),
                Type = XivChatType.Echo
            });
            return;
        }

        var item = ExcelService.FindRow<Item>(row => row?.ItemAction.Row == itemAction!.RowId);
        if (item == null)
        {
            ChatGui.Print(new XivChatEntry
            {
                Message = sb
                    .Append(TextService.TranslateSeString("Commands.WhatMount.WithoutItem", name))
                    .ToDalamudString(),
                Type = XivChatType.Echo
            });
            return;
        }

        sb.Append(TextService.TranslateSeString("Commands.WhatMount.WithItem", name, ItemService.GetItemLink(item.RowId)));

        ChatGui.Print(new XivChatEntry
        {
            Message = sb.ToDalamudString(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/whatemote", "Commands.Config.EnableWhatEmoteCommand.Description")]
    private void OnWhatEmoteCommand(string command, string arguments)
    {
        var target = TargetManager.Target;
        if (target == null)
        {
            ChatGui.PrintError(TextService.Translate("Commands.NoTarget"));
            return;
        }

        if (target.ObjectKind != DalamudObjectKind.Player)
        {
            ChatGui.PrintError(TextService.Translate("Commands.TargetIsNotAPlayer"));
            return;
        }

        var gameObject = (Character*)target.Address;

        var emoteId = gameObject->EmoteController.EmoteId;
        if (emoteId == 0)
        {
            ChatGui.PrintError(TextService.Translate("Commands.Emote.NotExecutingEmote"));
            return;
        }

        var emote = ExcelService.GetRow<Emote>(emoteId);
        if (emote == null)
        {
            ChatGui.PrintError(TextService.Translate("Commands.Emote.NotFound", emoteId.ToString()));
            return;
        }

        ChatGui.Print(new XivChatEntry
        {
            Message = new SeStringBuilder()
                .AppendHaselTweaksPrefix()
                .Append(TextService.TranslateSeString("Commands.Emote", emoteId.ToString(), TextService.GetEmoteName(emoteId)))
                .ToDalamudString(),
            Type = XivChatType.Echo
        });
    }
    [CommandHandler("/whatbarding", "Commands.Config.EnableWhatBardingCommand.Description")]
    private void OnWhatBardingCommand(string command, string arguments)
    {
        var target = TargetManager.Target;
        if (target == null)
        {
            ChatGui.PrintError(TextService.Translate("Commands.NoTarget"));
            return;
        }

        if (target.ObjectKind != DalamudObjectKind.BattleNpc || target.SubKind != (byte)BattleNpcSubKind.Chocobo)
        {
            ChatGui.PrintError(TextService.Translate("Commands.TargetIsNotAChocobo"));
            return;
        }

        var targetCharacter = (Character*)target.Address;

        var topRow = ExcelService.FindRow<BuddyEquip>(row => row?.ModelTop == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Value);
        var bodyRow = ExcelService.FindRow<BuddyEquip>(row => row?.ModelBody == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body).Value);
        var legsRow = ExcelService.FindRow<BuddyEquip>(row => row?.ModelLegs == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet).Value);

        var stain = ExcelService.GetRow<Stain>(targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Legs).Stain0)!;
        var name = new SeStringBuilder()
            .PushColorType(1)
            .Append(targetCharacter->GameObject.NameString)
            .PopColorType()
            .ToReadOnlySeString();

        var sb = new SeStringBuilder()
            .AppendHaselTweaksPrefix()
            .Append(TextService.TranslateSeString("Commands.WhatBarding.AppearanceOf", name))
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4987)}: ")
            .Append(stain.Name.ToString().FirstCharToUpper())
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4991)}: {topRow?.Name.ExtractText() ?? TextService.GetAddonText(4994)}")
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4992)}: {bodyRow?.Name.ExtractText() ?? TextService.GetAddonText(4994)}")
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4993)}: {legsRow?.Name.ExtractText() ?? TextService.GetAddonText(4994)}");

        ChatGui.Print(new XivChatEntry
        {
            Message = sb.ToDalamudString(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/glamourplate", "Commands.Config.EnableGlamourPlateCommand.Description")]
    private void OnGlamourPlateCommand(string command, string arguments)
    {
        if (!byte.TryParse(arguments, out var glamourPlateId) || glamourPlateId == 0 || glamourPlateId > 20)
        {
            ChatGui.PrintError(TextService.Translate("Commands.InvalidArguments"));
            return;
        }

        var raptureGearsetModule = RaptureGearsetModule.Instance();
        if (!raptureGearsetModule->IsValidGearset(raptureGearsetModule->CurrentGearsetIndex))
        {
            ChatGui.PrintError(TextService.Translate("Commands.GlamourPlate.InvalidGearset"));
            return;
        }

        raptureGearsetModule->EquipGearset(raptureGearsetModule->CurrentGearsetIndex, glamourPlateId);
    }
}
