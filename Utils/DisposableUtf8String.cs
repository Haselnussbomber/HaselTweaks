using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace HaselTweaks.Utils;

public unsafe class DisposableUtf8String : DisposableCreatable<Utf8String>, IDisposable
{
    public DisposableUtf8String() : base()
    {
    }

    public DisposableUtf8String(byte* text) : base()
    {
        Ptr->SetString(text);
    }

    public DisposableUtf8String(string text) : base()
    {
        Ptr->SetString(text);
    }

    public DisposableUtf8String(SeString text) : base()
    {
        Ptr->SetString(text.Encode());
    }

    public new void Dispose()
    {
        Ptr->Dtor();
        base.Dispose();
    }
}
