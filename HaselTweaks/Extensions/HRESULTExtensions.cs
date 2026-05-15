using System.Runtime.CompilerServices;
using TerraFX.Interop.Windows;

namespace HaselTweaks.Extensions;

public static class HRESULTExtensions
{
    extension(HRESULT hr)
    {
        // copied from Dalamud.Utility.Util
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowOnError()
        {
            if (hr.FAILED)
                Marshal.ThrowExceptionForHR(hr.Value);
        }
    }
}
