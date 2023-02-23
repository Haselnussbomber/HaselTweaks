using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public unsafe class Commands : Tweak
{
    public override string Name => "Commands";
    public override string Description => "A variety of useful chat commands.";
    public static Configuration Config => Plugin.Config.Tweaks.Commands;

    private const string ItemLinkCommand = "/itemlink";
    private const string WhatMountCommand = "/whatmount";

    public class Configuration
    {
        [ConfigField(Label = ItemLinkCommand, Description = $"Prints an item link for the given item id in chat.\nUsage: {ItemLinkCommand} <id>", OnChange = nameof(OnConfigChange))]
        public bool EnableItemLinkCommand = true;

        [ConfigField(Label = WhatMountCommand, Description = $"Target a player and execute the command to see what mount\nyour target is riding and which item teaches this mount.", OnChange = nameof(OnConfigChange))]
        public bool EnableWhatMountCommand = true;
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
        if (Config.EnableItemLinkCommand && !Service.Commands.Commands.ContainsKey(ItemLinkCommand))
        {
            Service.Commands.AddHandler(ItemLinkCommand, new CommandInfo(OnItemLinkCommand)
            {
                HelpMessage = $"Usage: {ItemLinkCommand} <id>",
                ShowInHelp = true
            });
        }

        if (Config.EnableWhatMountCommand && !Service.Commands.Commands.ContainsKey(WhatMountCommand))
        {
            Service.Commands.AddHandler(WhatMountCommand, new CommandInfo(OnWhatMountCommand)
            {
                HelpMessage = $"Usage: {WhatMountCommand}",
                ShowInHelp = true
            });
        }
    }

    private static void DisableCommands(bool removeAll = false)
    {
        if ((!Config.EnableItemLinkCommand || removeAll) && Service.Commands.Commands.ContainsKey(ItemLinkCommand))
        {
            Service.Commands.RemoveHandler(ItemLinkCommand);
        }

        if ((!Config.EnableWhatMountCommand || removeAll) && Service.Commands.Commands.ContainsKey(WhatMountCommand))
        {
            Service.Commands.RemoveHandler(WhatMountCommand);
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
            Service.Chat.PrintError($"Item #{id} not found");
            return;
        }

        var fgColor = (ushort)(549 + (item.Rarity - 1) * 2);
        var glowColor = (ushort)(fgColor + 1);

        var sb = new SeStringBuilder()
            .AddText($"Item #{id}: ")
            .AddUiForeground(fgColor)
            .AddUiGlow(glowColor)
            .AddItemLink(item.RowId, false)
            .AddUiForeground(500)
            .AddUiGlow(501)
            .AddText(SeIconChar.LinkMarker.ToIconString() + " ")
            .AddUiForegroundOff()
            .AddUiGlowOff()
            .AddText(item.Name)
            .Add(new RawPayload(new byte[] { 0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03 })) // LinkTerminator
            .Add(new RawPayload(new byte[] { 0x02, 0x13, 0x02, 0xEC, 0x03 })); // SaintCoinach says 0x13 is named Color, but idk

        Service.Chat.PrintChat(new XivChatEntry
        {
            Message = sb.BuiltString,
            Type = XivChatType.Echo
        });
    }

    private static void OnWhatMountCommand(string command, string arguments)
    {
        var target = Service.TargetManager.Target;
        if (target == null)
        {
            Service.Chat.PrintError($"No target.");
            return;
        }

        var targetGameObject = (GameObject*)target.Address;
        if (targetGameObject->ObjectKind != (byte)ObjectKind.Pc)
        {
            Service.Chat.PrintError($"Target is not a player.");
            return;
        }

        if (targetGameObject->ObjectIndex + 1 > Service.ObjectTable.Length)
        {
            Service.Chat.PrintError($"Could not get mount object: not inside object table.");
            return;
        }

        var mountObject = Service.ObjectTable[targetGameObject->ObjectIndex + 1];
        if (mountObject == null || (byte)mountObject.ObjectKind != (byte)ObjectKind.Mount)
        {
            Service.Chat.PrintError($"Target is not mounted.");
            return;
        }

        var modelChara = MemoryHelper.Read<uint>(mountObject.Address + 0x1B4);

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
            Service.Chat.PrintError($"Mount not found.");
            return;
        }

        var itemActionRowId = (
            from row in ItemActionSheet
            where row.Type == 1322 && row.Data[0] == mountRow.RowId
            select row.RowId
        ).FirstOrDefault();

        if (itemActionRowId == 0)
        {
            Service.Chat.PrintChat(new XivChatEntry
            {
                Message = $"Mount: {mountRow.Singular}",
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
                Message = $"Mount: {mountRow.Singular}",
                Type = XivChatType.Echo
            });
            return;
        }

        var fgColor = (ushort)(549 + (itemRow.Rarity - 1) * 2);
        var glowColor = (ushort)(fgColor + 1);

        var sb = new SeStringBuilder()
            .AddText($"Mount {mountRow.Singular} learned by ")
            .AddUiForeground(fgColor)
            .AddUiGlow(glowColor)
            .AddItemLink(itemRow.RowId, false)
            .AddUiForeground(500)
            .AddUiGlow(501)
            .AddText(SeIconChar.LinkMarker.ToIconString() + " ")
            .AddUiForegroundOff()
            .AddUiGlowOff()
            .AddText(itemRow.Name)
            .Add(new RawPayload(new byte[] { 0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03 })) // LinkTerminator
            .Add(new RawPayload(new byte[] { 0x02, 0x13, 0x02, 0xEC, 0x03 })); // SaintCoinach says 0x13 is named Color, but idk

        Service.Chat.PrintChat(new XivChatEntry
        {
            Message = sb.BuiltString,
            Type = XivChatType.Echo
        });
    }
}
