namespace HaselTweaks.ImGuiComponents;

public abstract class ConfirmationDialog : IDialog
{
    public delegate void InnerDrawDelegate();

    public string WindowName { get; set; } = string.Empty;

    private readonly List<ConfirmationButton> _buttons = [];
    private bool _shouldDraw;
    private bool _openCalled = true;

    public void Show()
    {
        _shouldDraw = true;
        _openCalled = false;
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

        if (!_openCalled)
        {
            ImGui.OpenPopup(WindowName);
            _openCalled = true;
        }

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        using var windowPadding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(14, 10));
        if (!ImGui.BeginPopupModal(WindowName, ref _shouldDraw, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar))
            return;

        try
        {
            InnerDraw();
        }
        catch (Exception ex)
        {
            ImGuiUtils.TextUnformattedColored(Color.Red, ex.Message);
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

        ImGui.EndPopup();

        PostDraw();
    }
}
