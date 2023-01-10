using LanguageExt;
using Microsoft.CodeAnalysis;
using static HaselTweaks.InteropSourceGenerators.DiagnosticDescriptors;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators.Models;

internal sealed record SignatureInfo(string Signature)
{
    public static Validation<DiagnosticInfo, SignatureInfo> GetValidatedSignature(string signature,
        IMethodSymbol location)
    {
        return IsValid(signature)
            ? Success<DiagnosticInfo, SignatureInfo>(new SignatureInfo(PadSignature(signature)))
            : Fail<DiagnosticInfo, SignatureInfo>(
                DiagnosticInfo.Create(
                    InvalidSignature,
                    location,
                    signature
                ));
    }

    private static bool IsValid(string signature)
    {
        return signature.Split(' ').All(subString => subString.Length == 2);
    }

    private static string PadSignature(string signature)
    {
        var paddingNeeded = 8 - (signature.Length / 3 + 1) % 8;
        if (paddingNeeded == 0)
            return signature;

        var result = new Span<char>(new char[signature.Length + paddingNeeded * 3]);
        signature.AsSpan().CopyTo(result);

        var repeated = " ??".AsSpan();

        for (var repeatIndex = 0; repeatIndex < paddingNeeded; repeatIndex++)
        {
            repeated.CopyTo(result.Slice(signature.Length + repeatIndex * repeated.Length));
        }

        return result.ToString();
    }

    public string GetByteArrayString()
    {
        var groupedSig = Signature.Replace("??", "00").Split()
            .Map((i, x) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / 8 * 3)
            .Map(x => x.Select(v => v.Value).ToSeq())
            .Map(x => "0x" + string.Join(string.Empty, x.Reverse()));

        return "new ulong[] {" + string.Join(", ", groupedSig) + "}";
    }

    public string GetMaskArrayString()
    {
        var groupedSig = Signature.Split()
            .Map(s => s == "??" ? "00" : "FF")
            .Map((i, x) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / 8 * 3)
            .Map(x => x.Select(v => v.Value).ToSeq())
            .Map(x => "0x" + string.Join(string.Empty, x.Reverse()));

        return "new ulong[] {" + string.Join(", ", groupedSig) + "}";
    }
}
