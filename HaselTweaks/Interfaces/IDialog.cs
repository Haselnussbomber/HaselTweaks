using HaselTweaks.ImGuiComponents;

namespace HaselTweaks.Interfaces;

public interface IDialog
{
    string WindowName { get; }
    void Show();
    void Hide();
    void AddButton(ConfirmationButton button);
    bool DrawCondition();
    void PreDraw();
    void PostDraw();
    void InnerDraw();
}
