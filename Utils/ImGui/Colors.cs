using System.Numerics;
using HaselTweaks.Structs;

namespace HaselTweaks.Utils;

public static class Colors
{
    public static ImColor Transparent { get; } = Vector4.Zero;
    public static ImColor White { get; } = Vector4.One;
    public static ImColor Black { get; } = new(0, 0, 0);
    public static ImColor Orange { get; } = new(1, 0.6f, 0);
    public static ImColor Gold { get; } = new(0.847f, 0.733f, 0.49f);
    public static ImColor Green { get; } = new(0, 1, 0);
    public static ImColor Red { get; } = new(1, 0, 0);
    public static ImColor Grey { get; } = new(0.73f, 0.73f, 0.73f);
    public static ImColor Grey2 { get; } = new(0.87f, 0.87f, 0.87f);
    public static ImColor Grey3 { get; } = new(0.6f, 0.6f, 0.6f);
    public static ImColor Grey4 { get; } = new(0.3f, 0.3f, 0.3f);
}
