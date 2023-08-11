using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using HaselTweaks.Utils;
using Lumina.Excel.GeneratedSheets;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe class Commands : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.Commands;

    private const string ItemLinkCommand = "/itemlink";
    private const string WhatMountCommand = "/whatmount";
    private const string WhatBardingCommand = "/whatbarding";

    public class Configuration
    {
        [BoolConfig]
        public bool EnableItemLinkCommand = true;

        [BoolConfig]
        public bool EnableWhatMountCommand = true;

        [BoolConfig]
        public bool EnableWhatBardingCommand = true;
    }

    public override void Enable()
    {
        RegisterCommands();
    }

    public override void Disable()
    {
        UnregisterCommands(true);
    }

    public override void OnConfigChange(string fieldName)
    {
        UnregisterCommands();
        RegisterCommands();
    }

    private static void RegisterCommands()
    {
        if (Config.EnableItemLinkCommand)
        {
            Service.CommandManager.RemoveHandler(ItemLinkCommand);
            Service.CommandManager.AddHandler(ItemLinkCommand, new CommandInfo(OnItemLinkCommand)
            {
                HelpMessage = $"Usage: {ItemLinkCommand} <id>",
                ShowInHelp = true
            });
        }

        if (Config.EnableWhatMountCommand)
        {
            Service.CommandManager.RemoveHandler(WhatMountCommand);
            Service.CommandManager.AddHandler(WhatMountCommand, new CommandInfo(OnWhatMountCommand)
            {
                HelpMessage = $"Usage: {WhatMountCommand}",
                ShowInHelp = true
            });
        }

        if (Config.EnableWhatBardingCommand)
        {
            Service.CommandManager.RemoveHandler(WhatBardingCommand);
            Service.CommandManager.AddHandler(WhatBardingCommand, new CommandInfo(OnWhatBardingCommand)
            {
                HelpMessage = $"Usage: {WhatBardingCommand}",
                ShowInHelp = true
            });
        }
    }

    private static void UnregisterCommands(bool removeAll = false)
    {
        if (!Config.EnableItemLinkCommand || removeAll)
        {
            Service.CommandManager.RemoveHandler(ItemLinkCommand);
        }

        if (!Config.EnableWhatMountCommand || removeAll)
        {
            Service.CommandManager.RemoveHandler(WhatMountCommand);
        }

        if (!Config.EnableWhatBardingCommand || removeAll)
        {
            Service.CommandManager.RemoveHandler(WhatBardingCommand);
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
            Service.ChatGui.PrintError(e.Message);
            return;
        }

        var item = GetRow<Item>(id);
        if (item == null)
        {
            Service.ChatGui.PrintError(t("Commands.ItemLink.ItemNotFound", id));
            return;
        }

        var idStr = new SeStringBuilder()
            .AddUiForeground(id.ToString(), 1)
            .Build();

        var sb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .Append(tSe("Commands.ItemLink.Item", idStr, ItemUtils.GetItemLink(id)));

        Service.ChatGui.PrintChat(new XivChatEntry
        {
            Message = sb.Build(),
            Type = XivChatType.Echo
        });
    }

    private static void OnWhatMountCommand(string command, string arguments)
    {
        var target = Service.TargetManager.Target;
        if (target == null)
        {
            Service.ChatGui.PrintError(t("Commands.NoTarget"));
            return;
        }

        if (target.ObjectKind != ObjectKind.Player)
        {
            Service.ChatGui.PrintError(t("Commands.TargetIsNotAPlayer"));
            return;
        }

        var targetGameObject = (GameObject*)target.Address;
        if (targetGameObject->ObjectIndex + 1 > Service.ObjectTable.Length)
        {
            Service.ChatGui.PrintError("Error: mount game object index out of bounds.");
            return;
        }

        var mountObject = Service.ObjectTable[targetGameObject->ObjectIndex + 1];
        if (mountObject == null || mountObject.ObjectKind != ObjectKind.MountType)
        {
            Service.ChatGui.PrintError(t("Commands.WhatMount.TargetNotMounted"));
            return;
        }

        var modelChara = ((Character*)mountObject.Address)->CharacterData.ModelCharaId;

        var mount = FindRow<Mount>(row => row?.ModelChara.Row == modelChara);
        if (mount == null)
        {
            Service.ChatGui.PrintError(t("Commands.WhatMount.MountNotFound"));
            return;
        }

        var sb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32);

        var name = new SeStringBuilder()
            .AddUiForeground(GetMountName(mount.RowId), 1)
            .Build();

        var itemAction = FindRow<ItemAction>(row => row?.Type == 1322 && row.Data[0] == mount.RowId);
        if (itemAction == null || itemAction.RowId == 0)
        {
            Service.ChatGui.PrintChat(new XivChatEntry
            {
                Message = sb
                    .Append(tSe("Commands.WhatMount.WithoutItem", name))
                    .Build(),
                Type = XivChatType.Echo
            });
            return;
        }

        var item = FindRow<Item>(row => row?.ItemAction.Row == itemAction!.RowId);
        if (item == null)
        {
            Service.ChatGui.PrintChat(new XivChatEntry
            {
                Message = sb
                    .Append(tSe("Commands.WhatMount.WithoutItem", name))
                    .Build(),
                Type = XivChatType.Echo
            });
            return;
        }

        var seItemLink = ItemUtils.GetItemLink(item.RowId);
        sb.Append(tSe("Commands.WhatMount.WithItem", name, seItemLink));

        Service.ChatGui.PrintChat(new XivChatEntry
        {
            Message = sb.Build(),
            Type = XivChatType.Echo
        });
    }

    private static void OnWhatBardingCommand(string command, string arguments)
    {
        var target = Service.TargetManager.Target;
        if (target == null)
        {
            Service.ChatGui.PrintError(t("Commands.NoTarget"));
            return;
        }

        if (target.ObjectKind != ObjectKind.BattleNpc || target.SubKind != (byte)BattleNpcSubKind.Chocobo)
        {
            Service.ChatGui.PrintError(t("Commands.TargetIsNotAChocobo"));
            return;
        }

        var targetCharacter = (Character*)target.Address;

        var topRow = FindRow<BuddyEquip>(row => row?.ModelTop == targetCharacter->DrawData.Head.Value);
        var bodyRow = FindRow<BuddyEquip>(row => row?.ModelBody == targetCharacter->DrawData.Top.Value);
        var legsRow = FindRow<BuddyEquip>(row => row?.ModelLegs == targetCharacter->DrawData.Feet.Value);

        var stain = GetRow<Stain>(targetCharacter->DrawData.Legs.Stain)!;
        var name = new SeStringBuilder()
            .AddUiForeground(MemoryHelper.ReadStringNullTerminated((nint)targetCharacter->GameObject.Name), 1)
            .Build();

        var sb = new SeStringBuilder()
            .AddUiForeground("\uE078 ", 32)
            .Append(tSe("Commands.WhatBarding.AppearanceOf", name))
            .Add(NewLinePayload.Payload)
            .AddText($"  {GetAddonText(4987)}: ")
            .Append(MemoryHelper.ReadSeStringNullTerminated((nint)RaptureTextModule.Instance()->FormatAddonText2(4986, (int)stain.RowId, 0))) // stain name
            .Add(NewLinePayload.Payload)
            .AddText($"  {GetAddonText(4991)}: {topRow?.Name.ToDalamudString().ToString() ?? GetAddonText(4994)}")
            .Add(NewLinePayload.Payload)
            .AddText($"  {GetAddonText(4992)}: {bodyRow?.Name.ToDalamudString().ToString() ?? GetAddonText(4994)}")
            .Add(NewLinePayload.Payload)
            .AddText($"  {GetAddonText(4993)}: {legsRow?.Name.ToDalamudString().ToString() ?? GetAddonText(4994)}");

        Service.ChatGui.PrintChat(new XivChatEntry
        {
            Message = sb.Build(),
            Type = XivChatType.Echo
        });
    }
}
