using System.Numerics;
using HaselCommon.Gui.Yoga;
using HaselCommon.Gui.Yoga.Components;
using HaselCommon.Services;
using ImGuiNET;
using YogaSharp;

namespace HaselTweaks.Windows;

public partial class TestWindow : YogaWindow
{
    public TestWindow(WindowManager windowManager) : base(windowManager, "HaselTweaks TestWindow")
    {
        Size = new Vector2(766, 600);
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(766, 600),
            MaximumSize = new Vector2(4096, 2160)
        };

        SizeCondition = ImGuiCond.Always;

        Flags |= ImGuiWindowFlags.NoResize;

        AllowClickthrough = false;
        AllowPinning = false;

        RootNode.Overflow = YGOverflow.Hidden;
        RootNode.Gap = ImGui.GetStyle().ItemSpacing.X;

        RootNode.Add(new Panel
        {
            FlexGrow = 1,
            Children = [
                new TextNode() { Text = "Test"u8 }
            ]
        });
    }
}
