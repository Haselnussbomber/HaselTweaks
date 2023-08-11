using System.IO;
using HaselTweaks.Structs;

namespace HaselTweaks.Extensions;

internal static class BinaryReaderExtension
{
    public static HalfVector2 ReadHalfVector2(this BinaryReader reader) => new()
    {
        X = reader.ReadHalf(),
        Y = reader.ReadHalf()
    };

    public static HalfVector4 ReadHalfVector4(this BinaryReader reader) => new()
    {
        X = reader.ReadHalf(),
        Y = reader.ReadHalf(),
        Z = reader.ReadHalf(),
        W = reader.ReadHalf()
    };
}
