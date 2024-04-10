namespace HaselTweaks.Structs;

// ctor "E8 ?? ?? ?? ?? BF ?? ?? ?? ?? 48 8D AB"
[StructLayout(LayoutKind.Explicit, Size = 0xB0)]
public struct TabSwitcher
{
    [FieldOffset(0x80)] public int TabIndex;
    [FieldOffset(0x84)] public int TabCount;

    [FieldOffset(0x90)] public nint CallbackPtr;
    [FieldOffset(0x98)] public nint Addon;

    [FieldOffset(0xA8)] public bool Enabled;

    public delegate nint CallbackDelegate(int tabIndex, nint addon);

    public readonly void InvokeCallback(int tabIndex, nint addon)
    {
        var callbackAddress = CallbackPtr;
        if (callbackAddress != 0)
            Marshal.GetDelegateForFunctionPointer<CallbackDelegate>(callbackAddress)(tabIndex, addon);
    }
}
