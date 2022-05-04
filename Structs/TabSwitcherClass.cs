using System;
using System.Runtime.InteropServices;

namespace HaselTweaks.Structs;

// ctor E8 ?? ?? ?? ?? BF ?? ?? ?? ?? 48 8D AB
[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public unsafe partial struct TabSwitcherClass
{
    [FieldOffset(0x78)] public int CurrentTabIndex;
    [FieldOffset(0x7C)] public int NumTabs;
    [FieldOffset(0x80)] public void* Callback;
    [FieldOffset(0x88)] public void* Addon;

    //[FieldOffset(0x98)] public bool Enabled;

    public delegate IntPtr CallbackDelegate(int tabIndex, IntPtr addon);
}
