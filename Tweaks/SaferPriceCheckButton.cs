using System;
using System.IO;
using Dalamud.Game;
using Dalamud.Game.Network;
using Dalamud.Game.Network.Structures;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe class SaferPriceCheckButton : Tweak
{
    public override string Name => "Safer Price Check Button";
    public override string Description => "Disables the price check button when a request is in progress.";

    private bool isWorking = false;
    private bool isWorkingLastFrame = false;

    public override void Enable()
    {
        Service.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
    }

    public override void Disable()
    {
        Service.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
    }

    public override void OnFrameworkUpdate(Framework framework)
    {
        if (isWorking == isWorkingLastFrame) return;

        var addon = (AddonRetainerSell*)AtkUtils.GetUnitBase("RetainerSell");
        if (addon == null || addon->PriceCheckButton == null) return;

        addon->PriceCheckButton->AtkComponentBase.SetEnabledState(!isWorking);

        isWorkingLastFrame = isWorking;
    }

    private void GameNetwork_NetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
    {
        if (direction != NetworkMessageDirection.ZoneDown) return;
        if (!Service.Data.IsDataReady) return;

        if (opCode == Service.Data.ServerOpCodes["MarketBoardItemRequestStart"])
        {
            using var stream = new UnmanagedMemoryStream((byte*)dataPtr.ToPointer(), 1544);
            using var reader = new BinaryReader(stream);

            stream.Position += 0xB;
            var AmountToArrive = reader.ReadByte();

            // when the server is ratelimiting requests it sends a response with AmountToArrive = 0
            isWorking = AmountToArrive != 0;
            return;
        }

        if (opCode == Service.Data.ServerOpCodes["MarketBoardOfferings"])
        {
            var listing = MarketBoardCurrentOfferings.Read(dataPtr);

            // last offerings packet has ListingIndexEnd = 0
            if (listing.ListingIndexEnd == 0)
            {
                isWorking = false;
            }

            return;
        }
    }
}
