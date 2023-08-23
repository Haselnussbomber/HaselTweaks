using System.IO;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace HaselTweaks.Extensions;

internal static class BinaryWriterExtension
{
    public static void Write(this BinaryWriter writer, HalfVector2 vec)
    {
        writer.Write(vec.X);
        writer.Write(vec.Y);
    }

    public static void Write(this BinaryWriter writer, HalfVector4 vec)
    {
        writer.Write(vec.X);
        writer.Write(vec.Y);
        writer.Write(vec.Z);
        writer.Write(vec.W);
    }
}
