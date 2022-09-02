using System;
using FFXIVClientStructs.Attributes;

namespace HaselTweaks.Structs;

public partial struct HaselAtkComponentCheckBox
{
    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 DD")]
    public partial IntPtr SetValue(bool isChecked);
}
