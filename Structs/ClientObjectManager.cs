using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using static HaselTweaks.Structs.HaselRaptureGearsetModule;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct ClientObjectManager
{
    [StaticAddress("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 8B 43 10")]
    public static partial ClientObjectManager* Instance();

    [FieldOffset(0x48)] public ClientObjectArray ClientObjects;

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public unsafe struct ClientObjectEntry
    {
        [FieldOffset(0x0)] public BattleChara* BattleChara;
        [FieldOffset(0x8)] public byte Unk8;
        [FieldOffset(0x14)] public short Unk14;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    public struct ClientObjectArray
    {
        public const int Length = 45;

        private fixed byte data[Length * Gearset.StructSize];

        public ClientObjectEntry* this[int i]
        {
            get
            {
                if (i < 0 || i >= Length) return null;
                fixed (byte* p = data)
                {
                    return (ClientObjectEntry*)(p + sizeof(ClientObjectEntry) * i);
                }
            }
        }
    }
}
