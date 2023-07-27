using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Caches;
using Lumina.Excel.GeneratedSheets;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselTweaks.Tweaks;

[Tweak(
    Name: "Commands",
    Description: "A variety of useful chat commands."
)]
public unsafe class Commands : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.Commands;

    private const string ItemLinkCommand = "/itemlink";
    private const string WhatMountCommand = "/whatmount";
    private const string WhatBardingCommand = "/whatbarding";

    public class Configuration
    {
        [ConfigField(Label = ItemLinkCommand, Description = "Prints an item link for the given item id in chat.\nUsage: {ItemLinkCommand} <id>", OnChange = nameof(OnConfigChange))]
        public bool EnableItemLinkCommand = true;

        [ConfigField(Label = WhatMountCommand, Description = "Target a player and execute the command to see what mount\nyour target is riding and which item teaches this mount.", OnChange = nameof(OnConfigChange))]
        public bool EnableWhatMountCommand = true;

        [ConfigField(Label = WhatBardingCommand, Description = "Target a players chocobo companion and execute the command to see\nwhat barding it is wearing.", OnChange = nameof(OnConfigChange))]
        public bool EnableWhatBardingCommand = true;
    }

    private static void OnConfigChange()
    {
        DisableCommands();
        EnableCommands();
    }

    public override void Enable()
    {
        EnableCommands();
    }

    public override void Disable()
    {
        DisableCommands(true);
    }

    private static void EnableCommands()
    {
        if (Config.EnableItemLinkCommand)
        {
            Service.Commands.RemoveHandler(ItemLinkCommand);
            Service.Commands.AddHandler(ItemLinkCommand, new CommandInfo(OnItemLinkCommand)
            {
                HelpMessage = $"Usage: {ItemLinkCommand} <id>",
                ShowInHelp = true
            });
        }

        if (Config.EnableWhatMountCommand)
        {
            Service.Commands.RemoveHandler(WhatMountCommand);
            Service.Commands.AddHandler(WhatMountCommand, new CommandInfo(OnWhatMountCommand)
            {
                HelpMessage = $"Usage: {WhatMountCommand}",
                ShowInHelp = true
            });
        }

        if (Config.EnableWhatBardingCommand)
        {
            Service.Commands.RemoveHandler(WhatBardingCommand);
            Service.Commands.AddHandler(WhatBardingCommand, new CommandInfo(OnWhatBardingCommand)
            {
                HelpMessage = $"Usage: {WhatBardingCommand}",
                ShowInHelp = true
            });
        }
    }

    private static void DisableCommands(bool removeAll = false)
    {
        if (!Config.EnableItemLinkCommand || removeAll)
        {
            Service.Commands.RemoveHandler(ItemLinkCommand);
        }

        if (!Config.EnableWhatMountCommand || removeAll)
        {
            Service.Commands.RemoveHandler(WhatMountCommand);
        }

        if (!Config.EnableWhatBardingCommand || removeAll)
        {
            Service.Commands.RemoveHandler(WhatBardingCommand);
        }
    }

    public static void OnItemLinkCommand(string command, string arguments)
    {
        uint id;
        try
        {
            id = Convert.ToUInt32(arguments.Trim());
        }
        catch (Exception e)
        {
            Service.Chat.PrintError(e.Message);
            return;
        }

        var item = Service.Data.GetExcelSheet<Item>()!.GetRow(id);
        if (item == null)
        {
            Service.Chat.PrintError($"Item {id} not found");
            return;
        }

        var itemLink = new SeStringBuilder()
                    .AddUiForeground(SeIconChar.LinkMarker.ToIconString() + " ", 500)
                    .Append(MemoryHelper.ReadSeStringNullTerminated((nint)RaptureTextModule.Instance()->FormatAddonText2(2021, (int)item.RowId, 0)))
                    .Build();

        Service.Chat.PrintChat(new XivChatEntry
        {
            Message = new SeStringBuilder()
                    .AddUiForeground("\uE078 ", 32)
                    .AddText("Item ")
                    .AddUiForeground(id.ToString(), 1)
                    .AddText(": ")
                    .Append(itemLink)
                    .Build(),
            Type = XivChatType.Echo
        });
    }

    private static void OnWhatMountCommand(string command, string arguments)
    {
        var target = Service.TargetManager.Target;
        if (target == null)
        {
            Service.Chat.PrintError("No target.");
            return;
        }

        if (target.ObjectKind != ObjectKind.Player)
        {
            Service.Chat.PrintError("Target is not a player.");
            return;
        }

        var targetGameObject = (GameObject*)target.Address;
        if (targetGameObject->ObjectIndex + 1 > Service.ObjectTable.Length)
        {
            Service.Chat.PrintError("Error: mount game object index out of bounds.");
            return;
        }

        var mountObject = Service.ObjectTable[targetGameObject->ObjectIndex + 1];
        if (mountObject == null || mountObject.ObjectKind != ObjectKind.MountType)
        {
            Service.Chat.PrintError("Target is not mounted.");
            return;
        }

        var modelChara = ((Character*)mountObject.Address)->CharacterData.ModelCharaId;

        var MountSheet = Service.Data.GetExcelSheet<Mount>()!;
        var ItemSheet = Service.Data.GetExcelSheet<Item>()!;
        var ItemActionSheet = Service.Data.GetExcelSheet<ItemAction>()!;

        var mountRow = (
            from row in MountSheet
            where row.ModelChara.Row == modelChara
            select row
        ).FirstOrDefault();

        if (mountRow == null)
        {
            Service.Chat.PrintError("Mount not found.");
            return;
        }

        var name = StringCache.GetMountName(mountRow.RowId);

        var itemActionRowId = (
            from row in ItemActionSheet
            where row.Type == 1322 && row.Data[0] == mountRow.RowId
            select row.RowId
        ).FirstOrDefault();

        if (itemActionRowId == 0)
        {
            Service.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("\uE078 ", 32)
                    .AddText("Mount: ")
                    .AddUiForeground(name, 1)
                    .Build(),
                Type = XivChatType.Echo
            });
            return;
        }

        var itemRow = (
            from row in ItemSheet
            where row.ItemAction.Row == itemActionRowId
            select row
        ).FirstOrDefault();

        if (itemRow == null)
        {
            Service.Chat.PrintChat(new XivChatEntry
            {
                Message = new SeStringBuilder()
                    .AddUiForeground("\uE078 ", 32)
                    .AddText("Mount: ")
                    .AddUiForeground(name, 1)
                    .Build(),
                Type = XivChatType.Echo
            });
            return;
        }

        var sesb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32);

        var itemLink = new SeStringBuilder()
                    .AddUiForeground(SeIconChar.LinkMarker.ToIconString() + " ", 500)
                    .Append(MemoryHelper.ReadSeStringNullTerminated((nint)RaptureTextModule.Instance()->FormatAddonText2(2021, (int)itemRow.RowId, 0)))
                    .Build();

        switch (Service.ClientState.ClientLanguage)
        {
            case Dalamud.ClientLanguage.German:
                sesb.AddText("Reittier ")
                    .AddUiForeground(name, 1)
                    .AddText(" gelehrt von ")
                    .Append(itemLink);
                break;
            case Dalamud.ClientLanguage.French:
                sesb.AddText("Monture ")
                    .AddUiForeground(name, 1)
                    .AddText(" enseignée par ")
                    .Append(itemLink);
                break;
            case Dalamud.ClientLanguage.Japanese:
                sesb.Append(itemLink)
                    .AddText(" によって教えられた ")
                    .AddUiForeground(name, 1)
                    .AddText("のマウント");
                break;
            default:
                sesb.AddText("Mount ")
                    .AddUiForeground(name, 1)
                    .AddText(" taught by ")
                    .AddUiForeground(SeIconChar.LinkMarker.ToIconString() + " ", 500)
                    .Append(itemLink);
                break;
        }

        Service.Chat.PrintChat(new XivChatEntry
        {
            Message = sesb.Build(),
            Type = XivChatType.Echo
        });
    }

    private static void OnWhatBardingCommand(string command, string arguments)
    {
        var target = Service.TargetManager.Target;
        if (target == null)
        {
            Service.Chat.PrintError("No target.");
            return;
        }

        if (target.ObjectKind != ObjectKind.BattleNpc || target.SubKind != (byte)BattleNpcSubKind.Chocobo)
        {
            Service.Chat.PrintError("Target is not a chocobo.");
            return;
        }

        var targetCharacter = (Character*)target.Address;
        var BuddyEquipSheet = Service.Data.GetExcelSheet<BuddyEquip>()!;

        var topRow = (
            from _row in BuddyEquipSheet
            where _row.ModelTop == targetCharacter->DrawData.Head.Value
            select _row
        ).FirstOrDefault();

        var bodyRow = (
            from _row in BuddyEquipSheet
            where _row.ModelBody == targetCharacter->DrawData.Top.Value
            select _row
        ).FirstOrDefault();

        var legsRow = (
            from _row in BuddyEquipSheet
            where _row.ModelLegs == targetCharacter->DrawData.Feet.Value
            select _row
        ).FirstOrDefault();

        var stain = Service.Data.GetExcelSheet<Stain>()!.GetRow(targetCharacter->DrawData.Legs.Stain)!;
        var name = MemoryHelper.ReadStringNullTerminated((nint)targetCharacter->GameObject.Name);

        var sesb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32);

        switch (Service.ClientState.ClientLanguage)
        {
            case Dalamud.ClientLanguage.German:
                sesb.AddText("Aussehen von ")
                    .AddUiForeground(name, 1)
                    .AddText(":");
                break;
            case Dalamud.ClientLanguage.French:
                sesb.AddText("Apparence de ")
                    .AddUiForeground(name, 1)
                    .AddText(":");
                break;
            case Dalamud.ClientLanguage.Japanese:
                sesb.AddUiForeground(name, 1)
                    .AddText(" の外見：");
                break;
            default:
                sesb.AddText("Appearance of ")
                    .AddUiForeground(name, 1)
                    .AddText(":");
                break;
        }

        sesb.Add(NewLinePayload.Payload)
            .AddText($"  {StringCache.GetAddonText(4987)}: ")
            .Append(MemoryHelper.ReadSeStringNullTerminated((nint)RaptureTextModule.Instance()->FormatAddonText2(4986, (int)stain.RowId, 0))) // stain name
            .Add(NewLinePayload.Payload)
            .AddText($"  {StringCache.GetAddonText(4991)}: {topRow?.Name.ToDalamudString().ToString() ?? StringCache.GetAddonText(4994)}")
            .Add(NewLinePayload.Payload)
            .AddText($"  {StringCache.GetAddonText(4992)}: {bodyRow?.Name.ToDalamudString().ToString() ?? StringCache.GetAddonText(4994)}")
            .Add(NewLinePayload.Payload)
            .AddText($"  {StringCache.GetAddonText(4993)}: {legsRow?.Name.ToDalamudString().ToString() ?? StringCache.GetAddonText(4994)}");

        Service.Chat.PrintChat(new XivChatEntry
        {
            Message = sesb.Build(),
            Type = XivChatType.Echo
        });
    }
}
