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
using Lumina.Excel.Sheets;
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

    void IDisposable.Dispose()
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
        var existsAsEventItem = isEventItem && ExcelService.GetSheet<EventItem>().HasRow(id);
        var existsAsItem = !isEventItem && ExcelService.GetSheet<Item>().HasRow(ItemService.GetBaseItemId(id));

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
            MessageBytes = sb.ToArray(),
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

        if (!ExcelService.TryGetRow<Mount>(target->Mount.MountId, out var mount))
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

        if (!ExcelService.TryFindRow<ItemAction>(row => row.Type == 1322 && row.Data[0] == mount.RowId, out var itemAction) || itemAction.RowId == 0)
        {
            ChatGui.Print(new XivChatEntry
            {
                MessageBytes = sb
                    .Append(TextService.TranslateSeString("Commands.WhatMount.WithoutItem", name))
                    .ToArray(),
                Type = XivChatType.Echo
            });
            return;
        }

        if (!ExcelService.TryFindRow<Item>(row => row.ItemAction.RowId == itemAction.RowId, out var item))
        {
            ChatGui.Print(new XivChatEntry
            {
                MessageBytes = sb
                    .Append(TextService.TranslateSeString("Commands.WhatMount.WithoutItem", name))
                    .ToArray(),
                Type = XivChatType.Echo
            });
            return;
        }

        sb.Append(TextService.TranslateSeString("Commands.WhatMount.WithItem", name, ItemService.GetItemLink(item.RowId)));

        ChatGui.Print(new XivChatEntry
        {
            MessageBytes = sb.ToArray(),
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

        if (!ExcelService.TryGetRow<Emote>(emoteId, out var emote))
        {
            ChatGui.PrintError(TextService.Translate("Commands.Emote.NotFound", emoteId.ToString()));
            return;
        }

        ChatGui.Print(new XivChatEntry
        {
            MessageBytes = new SeStringBuilder()
                .AppendHaselTweaksPrefix()
                .Append(TextService.TranslateSeString("Commands.Emote", emoteId.ToString(), TextService.GetEmoteName(emoteId)))
                .ToArray(),
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

        var hasTopRow = ExcelService.TryFindRow<BuddyEquip>(row => row.ModelTop == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Head).Value, out var topRow);
        var hasBodyRow = ExcelService.TryFindRow<BuddyEquip>(row => row.ModelBody == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Body).Value, out var bodyRow);
        var hasLegsRow = ExcelService.TryFindRow<BuddyEquip>(row => row.ModelLegs == (int)targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Feet).Value, out var legsRow);

        ExcelService.TryGetRow<Stain>(targetCharacter->DrawData.Equipment(DrawDataContainer.EquipmentSlot.Legs).Stain0, out var stain);

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
            .Append(stain.Name.ExtractText().FirstCharToUpper())
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4991)}: {(hasTopRow ? topRow.Name.ExtractText() : TextService.GetAddonText(4994))}")
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4992)}: {(hasBodyRow ? bodyRow.Name.ExtractText() : TextService.GetAddonText(4994))}")
            .AppendNewLine()
            .Append($"  {TextService.GetAddonText(4993)}: {(hasLegsRow ? legsRow.Name.ExtractText() : TextService.GetAddonText(4994))}");

        ChatGui.Print(new XivChatEntry
        {
            MessageBytes = sb.ToArray(),
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
