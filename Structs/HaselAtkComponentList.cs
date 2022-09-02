using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe partial struct HaselAtkComponentList
{
    [VirtualFunction(35)]
    public partial uint GetListLength();

    [MemberFunction("E8 ?? ?? ?? ?? 41 FE 85")]
    public partial IntPtr SetListLength(short value);

    [MemberFunction("E8 ?? ?? ?? ?? 45 38 A4 3E")]
    public partial void SetEntryText(uint index, byte* text);
}
