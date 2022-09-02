using System;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

public unsafe partial struct HaselAtkComponentDropDownList
{
    [MemberFunction("E8 ?? ?? ?? ?? 45 89 3E")]
    public partial IntPtr SetValue(int value);
}
