using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselCommon.Services;
using HaselTweaks.Structs;
using Lumina.Excel.GeneratedSheets;

namespace HaselTweaks.Tweaks;

public class MaterialAllocationConfiguration
{
    [BoolConfig]
    public bool SaveLastSelectedTab = true;
    public byte LastSelectedTab = 2;

    [BoolConfig]
    public bool OpenGatheringLogOnItemClick = true;
}

public sealed unsafe class MaterialAllocation(
    IGameInteropProvider GameInteropProvider,
    Configuration PluginConfig,
    TranslationManager TranslationManager,
    IAddonLifecycle AddonLifecycle,
    IChatGui ChatGui,
    AddonObserver AddonObserver)
    : Tweak<MaterialAllocationConfiguration>(PluginConfig, TranslationManager)
{
    private uint NextMJIGatheringNoteBookItemId;

    private Hook<AgentMJIGatheringNoteBook.Delegates.Update>? AgentMJIGatheringNoteBookUpdateHook;

    public override void OnInitialize()
    {
        AgentMJIGatheringNoteBookUpdateHook = GameInteropProvider.HookFromAddress<AgentMJIGatheringNoteBook.Delegates.Update>(
            GetAgent<AgentMJIGatheringNoteBook>()->VirtualTable->Update,
            AgentMJIGatheringNoteBookUpdateDetour);
    }

    public override void OnEnable()
    {
        NextMJIGatheringNoteBookItemId = 0;

        AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PostReceiveEvent);
        AddonLifecycle.RegisterListener(AddonEvent.PreSetup, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PreSetup);

        AddonObserver.AddonOpen += OnAddonOpen;

        AgentMJIGatheringNoteBookUpdateHook?.Enable();
    }

    public override void OnDisable()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostReceiveEvent, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PostReceiveEvent);
        AddonLifecycle.UnregisterListener(AddonEvent.PreSetup, "MJICraftMaterialConfirmation", AddonMJICraftMaterialConfirmation_PreSetup);

        AddonObserver.AddonOpen -= OnAddonOpen;

        AgentMJIGatheringNoteBookUpdateHook?.Disable();
    }

    private void AddonMJICraftMaterialConfirmation_PreSetup(AddonEvent type, AddonArgs args)
    {
        if (!Config.SaveLastSelectedTab)
            return;

        if (Config.LastSelectedTab > 2)
            Config.LastSelectedTab = 2;

        GetAgent<AgentMJICraftSchedule>()->CurReviewMaterialsTab = Config.LastSelectedTab;

        var addon = (AddonMJICraftMaterialConfirmation*)args.Addon;
        for (var i = 0; i < 3; i++)
        {
            var button = addon->RadioButtons.GetPointer(i);
            if (button->Value != null)
            {
                button->Value->IsSelected = i == Config.LastSelectedTab;
            }
        }
    }

    private void OnAddonOpen(string addonName)
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

    private void AgentMJIGatheringNoteBookUpdateDetour(AgentMJIGatheringNoteBook* agent, uint frameCount)
    {
        var handleUpdate = Config.OpenGatheringLogOnItemClick
            && NextMJIGatheringNoteBookItemId != 0
            && agent->Data != null
            && agent->Data->Status == 3
            && (agent->Data->Flags & 2) != 2 // refresh pending
            && agent->Data->SortedGatherItems.LongCount != 0;

        AgentMJIGatheringNoteBookUpdateHook!.Original(agent, frameCount);

        if (handleUpdate)
        {
            UpdateGatheringNoteBookItem(agent, NextMJIGatheringNoteBookItemId);
            NextMJIGatheringNoteBookItemId = 0;
        }
    }

    private void AddonMJICraftMaterialConfirmation_PostReceiveEvent(AddonEvent type, AddonArgs args)
    {
        if (type != AddonEvent.PostReceiveEvent || args is not AddonReceiveEventArgs receiveEventArgs)
            return;

        if (receiveEventArgs.EventParam is > 0 and < 4 && Config.SaveLastSelectedTab)
        {
            Config.LastSelectedTab = (byte)(receiveEventArgs.EventParam - 1);
            PluginConfig.Save();
            return;
        }

        if (receiveEventArgs.EventParam == 9902)
        {
            if (!Config.OpenGatheringLogOnItemClick)
                return;

            var addon = (AddonMJICraftMaterialConfirmation*)receiveEventArgs.Addon;
            //var itemRenderer = *(AtkComponentListItemRenderer**)a5;
            var index = *(int*)(receiveEventArgs.Data + 0x10);
            //var list = *(HaselTweaks.Structs.AtkComponentList**)(a5 + 0x150);
            var id = addon->AtkUnitBase.AtkValues[index + 4].UInt; // MJIItemPouch RowId

            var pouchRow = GetRow<MJIItemPouch>(id);
            if (pouchRow == null || pouchRow.Item.Row == 0)
                return;

            var itemId = pouchRow.Item.Row;

            var mjiGatheringItemRow = FindRow<MJIGatheringItem>(row => row?.Item.Row == itemId);
            if (mjiGatheringItemRow == null)
            {
                var item = pouchRow.Item.Value;
                if (itemId != 0 && item != null)
                {
                    ChatGui.Print(new XivChatEntry
                    {
                        Message = tSe("MaterialAllocation.ItemIsNotGatherable", SeString.CreateItemLink(item.RowId, false, GetItemName(item.RowId))),
                        Type = XivChatType.Echo
                    });
                }
                return;
            }

            var agentMJIGatheringNoteBook = GetAgent<AgentMJIGatheringNoteBook>();
            if (IsAddonOpen(AgentId.MJIGatheringNoteBook))
            {
                // just switch item
                UpdateGatheringNoteBookItem(agentMJIGatheringNoteBook, itemId);
                NextMJIGatheringNoteBookItemId = 0;
            }
            else
            {
                // open with item
                NextMJIGatheringNoteBookItemId = itemId;
                agentMJIGatheringNoteBook->AgentInterface.Show();
            }
        }
    }

    private void UpdateGatheringNoteBookItem(AgentMJIGatheringNoteBook* agent, uint itemId)
    {
        for (var index = 0u; index < agent->Data->SortedGatherItems.LongCount; index++)
        {
            var gatherItemPtr = agent->Data->SortedGatherItems[index];
            if (gatherItemPtr.Value == null || gatherItemPtr.Value->ItemId != itemId)
                continue;

            agent->SelectItem(index);
            break;
        }
    }
}
