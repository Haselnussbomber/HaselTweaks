using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace HaselTweaks.Tweaks;

public class CommandsConfiguration
{
    [BoolConfig]
    public bool EnableItemLinkCommand = true;

    [BoolConfig]
    public bool EnableWhatMountCommand = true;

    [BoolConfig]
    public bool EnableWhatBardingCommand = true;
}

[Tweak]
public unsafe class Commands : Tweak<CommandsConfiguration>
{
    [CommandHandler("/itemlink", "Commands.Config.EnableItemLinkCommand.Description", nameof(Config.EnableItemLinkCommand))]
    private void OnItemLinkCommand(string command, string arguments)
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
            .Append(tSe("Commands.ItemLink.Item", idStr, SeString.CreateItemLink(id)));

        Service.ChatGui.Print(new XivChatEntry
        {
            Message = sb.Build(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/whatmount", "Commands.Config.EnableWhatMountCommand.Description", nameof(Config.EnableWhatMountCommand))]
    private void OnWhatMountCommand(string command, string arguments)
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
            Service.ChatGui.Print(new XivChatEntry
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
            Service.ChatGui.Print(new XivChatEntry
            {
                Message = sb
                    .Append(tSe("Commands.WhatMount.WithoutItem", name))
                    .Build(),
                Type = XivChatType.Echo
            });
            return;
        }

        sb.Append(tSe("Commands.WhatMount.WithItem", name, SeString.CreateItemLink(item.RowId)));

        Service.ChatGui.Print(new XivChatEntry
        {
            Message = sb.Build(),
            Type = XivChatType.Echo
        });
    }

    [CommandHandler("/whatbarding", "Commands.Config.EnableWhatMountCommand.Description", nameof(Config.EnableWhatBardingCommand))]
    private void OnWhatBardingCommand(string command, string arguments)
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
            .AddUiForeground(MemoryHelper.ReadString((nint)targetCharacter->GameObject.Name, 0x40), 1)
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

        Service.ChatGui.Print(new XivChatEntry
        {
            Message = sb.Build(),
            Type = XivChatType.Echo
        });
    }
}
