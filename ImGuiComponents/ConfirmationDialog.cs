using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Raii;
using HaselTweaks.Utils;
using ImGuiNET;

namespace HaselTweaks.ImGuiComponents;

public abstract class ConfirmationDialog
{
    public delegate void InnerDrawDelegate();

    public string WindowName { get; init; }

    private readonly List<ConfirmationButton> buttons = new();
    private bool shouldDraw;
    private bool openCalled = true;
    private Vector2? position;
    private int initialPositionFrame;

    public ConfirmationDialog(string title)
    {
        WindowName = title;
    }

    public void Show()
    {
        shouldDraw = true;
        openCalled = false;
        position = null;
        initialPositionFrame = 0;
    }

    public void Hide()
        => shouldDraw = true;

    public void AddButton(ConfirmationButton button)
        => buttons.Add(button);

    public virtual bool DrawCondition()
        => shouldDraw;

    public virtual void PreDraw() { }

    public virtual void PostDraw() { }

    public abstract void InnerDraw();

    public void Draw()
    {
        if (!DrawCondition()) return;

        PreDraw();

        if (initialPositionFrame == 3 && position != null)
        {
            ImGui.SetNextWindowPos(position.Value);
            initialPositionFrame++;
        }
        else if (initialPositionFrame < 3)
        {
            initialPositionFrame++;
        }

        if (!openCalled)
        {
            ImGui.OpenPopup(WindowName);
            openCalled = true;
        }

        using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(14, 10));
        if (!ImGui.BeginPopupModal(WindowName, ref shouldDraw, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            return;

        try
        {
            InnerDraw();
        }
        catch (Exception ex)
        {
            ImGui.TextColored(ImGuiUtils.ColorRed, ex.Message);
        }

        ImGui.Spacing();

        var buttonSpacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = (int)Math.Max(80f, (ImGui.GetContentRegionAvail().X - buttonSpacing * (buttons.Count - 1)) / buttons.Count - 80);
        var totalWidth = buttons.Count * buttonWidth + buttonSpacing * (buttons.Count - 1);

        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - totalWidth) / 2f);

        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];

            if (button.Draw(i, buttonWidth))
            {
                shouldDraw = false;
                ImGui.CloseCurrentPopup();
            }

            if (i < buttons.Count)
            {
                ImGui.SameLine();
            }
        }

        if (initialPositionFrame == 2 && position == null)
        {
            position = ImGui.GetMainViewport().GetCenter() - ImGui.GetWindowSize() / 2f;
        }

        ImGui.EndPopup();

        PostDraw();
    }
}
