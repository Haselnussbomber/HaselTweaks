using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Utils;
using HaselTweaks.Enums;
using HaselTweaks.Structs;
using HaselTweaks.Structs.Agents;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

[Tweak]
public unsafe partial class MaterialAllocation : Tweak
{
    public static Configuration Config => Plugin.Config.Tweaks.MaterialAllocation;

    private uint _nextMJIGatheringNoteBookItemId;

    public class Configuration
    {
        [BoolConfig]
        public bool SaveLastSelectedTab = true;
        public byte LastSelectedTab = 2;

        [BoolConfig]
        public bool OpenGatheringLogOnItemClick = true;
    }

    public override void Enable()
    {
        _nextMJIGatheringNoteBookItemId = 0;
    }

    [VTableHook<AddonMJICraftMaterialConfirmation>(48)]
    public nint AddonMJICraftMaterialConfirmation_vf48(AddonMJICraftMaterialConfirmation* addon, int numAtkValues, nint atkValues)
    {
        if (Config.SaveLastSelectedTab)
        {
            if (Config.LastSelectedTab > 2)
                Config.LastSelectedTab = 2;

            GetAgent<AgentMJICraftSchedule>()->TabIndex = Config.LastSelectedTab;

            for (var i = 0; i < 3; i++)
            {
                var button = AsPointer(ref addon->RadioButtonsSpan[i]);
                if (button->Value != null)
                {
                    button->Value->SetSelected(i == Config.LastSelectedTab);
                }
            }
        }

        return AddonMJICraftMaterialConfirmation_vf48Hook.OriginalDisposeSafe(addon, numAtkValues, atkValues);
    }

    public override void OnAddonOpen(string addonName)
    {
        if (addonName != "MJICraftMaterialConfirmation")
            return;

        if (!TryGetAddon<AddonMJICraftMaterialConfirmation>(addonName, out var addon))
            return;

        if (addon->ItemList != null && addon->ItemList->AtkComponentBase.OwnerNode != null)
        {
            //addon->ItemList->AtkComponentBase.OwnerNode->AtkResNode.AddEvent(31, 9901, (AtkEventListener*)addon, null, false); // MouseDown
            addon->ItemList->AtkComponentBase.OwnerNode->AtkResNode.AddEvent(32, 9902, (AtkEventListener*)addon, null, false); // MouseUp
        }
    }

    [VTableHook<AgentMJIGatheringNoteBook>((int)AgentInterfaceVfs.Update)]
    public void AgentMJIGatheringNoteBook_Update(AgentMJIGatheringNoteBook* agent)
    {
        var handleUpdate = Config.OpenGatheringLogOnItemClick
            && _nextMJIGatheringNoteBookItemId != 0
            && agent->Data != null
            && agent->Data->Status == 3
            && (agent->Data->Flags & 2) != 2 // refresh pending
            && agent->Data->GatherItemPtrs != null;

        AgentMJIGatheringNoteBook_UpdateHook.OriginalDisposeSafe(agent);

        if (handleUpdate)
        {
            UpdateGatheringNoteBookItem(agent, _nextMJIGatheringNoteBookItemId);
            _nextMJIGatheringNoteBookItemId = 0;
        }
    }

    [VTableHook<AddonMJICraftMaterialConfirmation>((int)AtkResNodeVfs.ReceiveEvent)]
    public void AddonMJICraftMaterialConfirmation_ReceiveEvent(AddonMJICraftMaterialConfirmation* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if (eventParam is > 0 and < 4 && Config.SaveLastSelectedTab)
        {
            Config.LastSelectedTab = (byte)(eventParam - 1);
        }

        if (eventParam == 9902)
        {
            if (!Config.OpenGatheringLogOnItemClick)
                goto handled;

            //var itemRenderer = *(AtkComponentListItemRenderer**)a5;
            var index = *(int*)(a5 + 0x10);
            //var list = *(HaselTweaks.Structs.AtkComponentList**)(a5 + 0x150);
            var id = addon->AtkUnitBase.AtkValues[index + 4].UInt; // MJIItemPouch RowId

            var pouchRow = GetRow<MJIItemPouch>(id);
            if (pouchRow == null || pouchRow.Item.Row == 0)
                goto handled;

            var itemId = pouchRow.Item.Row;

            var mjiGatheringItemRow = FindRow<MJIGatheringItem>(row => row?.Item.Row == itemId);
            if (mjiGatheringItemRow == null)
            {
                var item = pouchRow.Item.Value;
                if (itemId != 0 && item != null)
                {
                    Service.ChatGui.PrintChat(new XivChatEntry
                    {
                        Message = tSe("MaterialAllocation.ItemIsNotGatherable", ItemUtils.GetItemLink(item.RowId)),
                        Type = XivChatType.Echo
                    });
                }
                goto handled;
            }

            var agentMJIGatheringNoteBook = GetAgent<AgentMJIGatheringNoteBook>();
            if (IsAddonOpen(AgentId.MJIGatheringNoteBook))
            {
                // just switch item
                UpdateGatheringNoteBookItem(agentMJIGatheringNoteBook, itemId);
                _nextMJIGatheringNoteBookItemId = 0;
            }
            else
            {
                // open with item
                _nextMJIGatheringNoteBookItemId = itemId;
                agentMJIGatheringNoteBook->AgentInterface.Show();
            }

handled:
            atkEvent->SetEventIsHandled();
            return;
        }

        AddonMJICraftMaterialConfirmation_ReceiveEventHook.OriginalDisposeSafe(addon, eventType, eventParam, atkEvent, a5);
    }

    private void UpdateGatheringNoteBookItem(AgentMJIGatheringNoteBook* agent, uint itemId)
    {
        for (var index = 0u; index < agent->Data->ItemCount; index++)
        {
            var gatherItem = agent->Data->GatherItemPtrs[index];
            if (gatherItem == null || gatherItem->ItemId != itemId)
                continue;

            agent->Data->SelectedItemIndex = index;
            agent->Data->Flags |= 2;
            break;
        }
    }
}
