using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x2A8)]
public unsafe partial struct CharaViewTryOn : ICreatable
{
    [FieldOffset(0)] public CharaView Base;
    [FieldOffset(0x2A0)] public bool DoUpdate; // beware: fetches data from agent too, happens in vf10
    [FieldOffset(0x2A1)] public byte Unk2A1;
    [FieldOffset(0x2A2)] public bool HideVisor;
    [FieldOffset(0x2A3)] public bool HideWeapon;
    [FieldOffset(0x2A4)] public bool DrawWeapon;
    [FieldOffset(0x2A5)] public byte Unk2A5;
    [FieldOffset(0x2A6)] public byte Unk2A6;

    public static CharaViewTryOn* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewTryOn>();
    }

    [MemberFunction("40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 48 89 03 48 8B C3 66 C7 83 ?? ?? ?? ?? ?? ?? C6 83 ?? ?? ?? ?? ?? 48 83 C4 20")]
    public partial void Ctor();

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    // TryonCharaView_vf10
    [VirtualFunction(10)]
    public partial void Vf10(IntPtr a2, IntPtr a3);

    // TryonCharaView_vf13
    // Initialize with loading gear and some config options
    [VirtualFunction(13)]
    public partial void Vf13(IntPtr agent, int clientObjectId, IntPtr fn, bool a5);
}
