using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Raii;
using HaselCommon.Utils;
using ImGuiNET;

namespace HaselTweaks.ImGuiComponents;

public abstract class ConfirmationDialog
{
    public delegate void InnerDrawDelegate();

    public string WindowName { get; init; }

    private readonly List<ConfirmationButton> _buttons = new();
    private bool _shouldDraw;
    private bool _openCalled = true;
    private Vector2? _position;
    private int _initialPositionFrame;

    public ConfirmationDialog(string title)
    {
        WindowName = title;
    }

    public void Show()
    {
        _shouldDraw = true;
        _openCalled = false;
        _position = null;
        _initialPositionFrame = 0;
    }

    public void Hide()
        => _shouldDraw = true;

    public void AddButton(ConfirmationButton button)
        => _buttons.Add(button);

    public virtual bool DrawCondition()
        => _shouldDraw;

    public virtual void PreDraw() { }

    public virtual void PostDraw() { }

    public abstract void InnerDraw();

    public void Draw()
    {
        if (!DrawCondition()) return;

        PreDraw();

        if (_initialPositionFrame == 3 && _position != null)
        {
            ImGui.SetNextWindowPos(_position.Value);
            _initialPositionFrame++;
        }
        else if (_initialPositionFrame < 3)
        {
            _initialPositionFrame++;
        }

        if (!_openCalled)
        {
            ImGui.OpenPopup(WindowName);
            _openCalled = true;
        }

        using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(14, 10));
        if (!ImGui.BeginPopupModal(WindowName, ref _shouldDraw, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            return;

        try
        {
            InnerDraw();
        }
        catch (Exception ex)
        {
            ImGuiUtils.TextUnformattedColored(Colors.Red, ex.Message);
        }

        ImGui.Spacing();

        var buttonSpacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonWidth = (int)Math.Max(80f, (ImGui.GetContentRegionAvail().X - buttonSpacing * (_buttons.Count - 1)) / _buttons.Count - 80);
        var totalWidth = _buttons.Count * buttonWidth + buttonSpacing * (_buttons.Count - 1);

        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - totalWidth) / 2f);

        for (var i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];

            if (button.Draw(i, buttonWidth))
            {
                _shouldDraw = false;
                ImGui.CloseCurrentPopup();
            }

            if (i < _buttons.Count)
            {
                ImGui.SameLine();
            }
        }

        if (_initialPositionFrame == 2 && _position == null)
        {
            _position = ImGui.GetMainViewport().GetCenter() - ImGui.GetWindowSize() / 2f;
        }

        ImGui.EndPopup();

        PostDraw();
    }
}
