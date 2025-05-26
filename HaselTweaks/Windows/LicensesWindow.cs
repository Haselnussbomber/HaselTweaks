using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public partial class LicensesWindow : SimpleWindow
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private string[] _lines = [];

    [GeneratedRegex("\r?\n")]
    private static partial Regex NewLineRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^\)]+)\)")]
    private static partial Regex MarkdownLinkRegex();

    [AutoPostConstruct]
    private void Initialize()
    {
        SizeCondition = ImGuiCond.Always;
        Flags |= ImGuiWindowFlags.NoResize;
        AllowClickthrough = false;
        AllowPinning = false;

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HaselTweaks.LICENSES.md");
        if (stream == null)
            return;

        using var reader = new StreamReader(stream);

        var lines = NewLineRegex().Split(reader.ReadToEnd());
        var startLine = 0;
        for (; startLine < lines.Length; startLine++)
        {
            if (lines[startLine].StartsWith("## "))
                break;
        }

        if (startLine >= lines.Length)
            startLine = 0;

        _lines = lines[startLine..];
    }

    public override void PreDraw()
    {
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (Size.HasValue)
            return;

        using (_pluginInterface.UiBuilder.MonoFontHandle.Push())
        {
            var width = _lines.Max(line => ImGui.CalcTextSize(line).X) + ImGui.GetStyle().ItemSpacing.X * 2 + ImGui.GetStyle().ScrollbarSize + ImGui.GetStyle().IndentSpacing * 2f;
            var height = width / (4f / 3.5f);
            Size = new Vector2(width, height);
        }
    }

    public override void Draw()
    {
        IDisposable? font = null;
        IDisposable? indent = null;
        var hadPrevious = false;

        ImGui.Spacing();

        foreach (var line in _lines)
        {
            if (line.StartsWith("## "))
            {
                if (hadPrevious)
                {
                    indent?.Dispose();
                    indent = null;
                    ImGuiUtils.PushCursorY(ImGui.GetFrameHeight());
                }

                hadPrevious = true;

                var match = MarkdownLinkRegex().Match(line[3..]);
                if (match.Success)
                {
                    using (_pluginInterface.UiBuilder.MonoFontHandle.Push())
                    {
                        using (Color.Gold.Push(ImGuiCol.Text))
                        using (ImRaii.PushIndent(ImGui.GetStyle().IndentSpacing / 2f))
                            ImGuiUtils.DrawLink(match.Groups[1].Value, string.Empty, match.Groups[2].Value);
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                        ImGui.Spacing();
                        indent = ImRaii.PushIndent();
                    }
                }
                else
                {
                    ImGui.TextUnformatted(line);
                }

                continue;
            }

            if (line == "```")
            {
                if (font != null)
                {
                    font.Dispose();
                    font = null;
                }
                else
                {
                    font = _pluginInterface.UiBuilder.MonoFontHandle.Push();
                }
                continue;
            }

            if (font == null && string.IsNullOrEmpty(line))
                continue;

            ImGui.TextUnformatted(line);
        }

        font?.Dispose();
        indent?.Dispose();

        ImGui.Spacing();
    }
}
