using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.Exd;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GlamourDresserKeyboardNavigation : Tweak
{
    private static readonly VirtualKey[] NavKeys = [VirtualKey.LEFT, VirtualKey.RIGHT, VirtualKey.UP, VirtualKey.DOWN];

    private readonly IFramework _framework;
    private readonly IKeyState _keyState;

    public override void OnEnable()
    {
        _framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        _framework.Update -= OnFrameworkUpdate;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var agent = AgentMiragePrismPrismBox.Instance();
        if (agent == null || agent->Data == null || !agent->IsAgentActive())
            return;

        if (!TryGetAddon<AddonMiragePrismPrismBox>((ushort)agent->GetAddonId(), out var addon))
            return;

        if (!TryGetAddon<AgentMiragePrismMiragePlate>((ushort)AgentMiragePrismMiragePlate.Instance()->GetAddonId(), out var mirageplateaddon))
            return;

        var unitManager = RaptureAtkUnitManager.Instance();
        if (unitManager == null || (unitManager->FocusedAddon != addon && unitManager->FocusedAddon != mirageplateaddon))
            return;

        EnsureVisibleItemsAreLoaded(agent);

        if (!NavKeys.TryGetFirst(key => _keyState[key], out var key))
            return;

        var holdingShift = _keyState[VirtualKey.SHIFT];
        var itemIndex = (uint)agent->Data->SelectedPageIndex;
        var itemCount = GetItemCount();
        var targetIndex = 0u;

        switch (key)
        {
            case VirtualKey.LEFT when holdingShift:
                targetIndex = itemIndex / 10 * 10;

                if (targetIndex != itemIndex)
                {
                    _logger.LogDebug("SHIFT+LEFT: jump to first column");

                    SetItemIndex(itemIndex / 10 * 10);
                }
                else if (TryGoToPrevPage())
                {
                    _logger.LogDebug("SHIFT+LEFT: already in the first column -> go to previous page");

                }
                else
                {
                    _logger.LogDebug("SHIFT+LEFT: nowhere to go");
                }

                _keyState[key] = false;
                break;

            case VirtualKey.LEFT:
                if (itemIndex > 0)
                {
                    _logger.LogDebug("LEFT");

                    SetItemIndex(itemIndex - 1);
                }
                else if (TryGoToPrevPage())
                {
                    _logger.LogDebug("LEFT: no more items -> go to previous page");

                    // we know it has 50 items
                    SetItemIndex(49);
                }
                else
                {
                    _logger.LogDebug("LEFT: nowhere to go");
                }

                _keyState[key] = false;
                break;

            case VirtualKey.RIGHT when holdingShift:
                _logger.LogDebug("SHIFT+RIGHT: jump to last possible column");

                targetIndex = itemIndex / 10 * 10 + 9;

                if (targetIndex >= itemCount)
                    targetIndex = (uint)itemCount;

                SetItemIndex(targetIndex);

                _keyState[key] = false;
                break;

            case VirtualKey.RIGHT:
                if (itemIndex < itemCount - 1)
                {
                    _logger.LogDebug("RIGHT");

                    SetItemIndex(itemIndex + 1);
                }
                else if (TryGoToNextPage())
                {
                    _logger.LogDebug("RIGHT: no more items -> go to next page");

                    SetItemIndex(0);
                }
                else
                {
                    _logger.LogDebug("RIGHT: nowhere to go");
                }

                _keyState[key] = false;
                break;

            case VirtualKey.UP when holdingShift:
                _logger.LogDebug("SHIFT+UP: jump to first row");

                SetItemIndex(itemIndex % 10);

                _keyState[key] = false;
                break;

            case VirtualKey.UP:
                if (itemIndex >= 10)
                {
                    _logger.LogDebug("UP");

                    SetItemIndex(itemIndex - 10);
                }
                else if (TryGoToPrevPage())
                {
                    _logger.LogDebug("UP: no more items -> go to previous page");

                    itemCount = GetItemCount();

                    targetIndex = itemIndex % 10;
                    while (targetIndex + 10 < itemCount)
                        targetIndex += 10;

                    SetItemIndex(targetIndex);

                }
                else
                {
                    _logger.LogDebug("UP: nowhere to go");
                }

                _keyState[key] = false;
                break;

            case VirtualKey.DOWN when holdingShift:
                _logger.LogDebug("SHIFT+DOWN: jump to last possible row");

                targetIndex = itemIndex;

                while (targetIndex + 10 < itemCount)
                    targetIndex += 10;

                SetItemIndex(targetIndex);

                _keyState[key] = false;
                break;

            case VirtualKey.DOWN:
                if (itemIndex + 10 < itemCount)
                {
                    _logger.LogDebug("DOWN");

                    SetItemIndex(itemIndex + 10);
                }
                else if (TryGoToNextPage())
                {
                    _logger.LogDebug("DOWN: no more items -> go to next page");

                    targetIndex = itemIndex % 10;
                    if (targetIndex >= GetItemCount())
                        targetIndex = 0; // if not enough items, reset to first item

                    SetItemIndex(targetIndex);
                }
                else
                {
                    _logger.LogDebug("DOWN: nowhere to go");
                }

                _keyState[key] = false;
                break;
        }

        int GetItemCount()
        {
            var itemCount = 0;

            for (var i = 0; i < agent->Data->PageItemIndexes.Length; i++)
            {
                if (agent->Data->PageItemIndexes[i] >= 8000)
                    break;
                itemCount++;
            }

            return itemCount;
        }

        bool TryGoToNextPage()
        {
            if (!addon->NextButton->IsEnabled)
                return false;

            agent->PageIndex++;
            agent->UpdateItems(false, false);
            return true;
        }

        bool TryGoToPrevPage()
        {
            if (!addon->PrevButton->IsEnabled)
                return false;

            agent->PageIndex--;
            agent->UpdateItems(false, false);
            return true;
        }

        void SetItemIndex(uint index)
        {
            if (index > 49)
                index = 0;

            UIGlobals.PlaySoundEffect(1);

            var retVal = stackalloc AtkValue[1];
            var values = stackalloc AtkValue[3];
            values[0].SetUInt(2);
            values[1].SetUInt(index);
            values[2].Ctor();
            agent->ReceiveEvent(retVal, values, 3, 0);

            agent->UpdateItems(false, false);
        }
    }

    private void EnsureVisibleItemsAreLoaded(AgentMiragePrismPrismBox* agent)
    {
        foreach (var itemIndex in agent->Data->PageItemIndexes)
        {
            if (itemIndex >= 8000)
                continue;

            ExdModule.GetItemRowById(agent->Data->PrismBoxItems[itemIndex].ItemId % 500000);
        }
    }
}
