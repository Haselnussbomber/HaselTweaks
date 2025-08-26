namespace HaselTweaks.ImGuiComponents;

public class ConfirmationButton(string label, ConfirmationButton.ClickCallbackDelegate clickCallback)
{
    public delegate void ClickCallbackDelegate();

    public string Label = label;
    public ClickCallbackDelegate ClickCallback = clickCallback;

    public bool Disabled { get; set; }

    internal bool Draw(int buttonIndex, float buttonWidth)
    {
        using var id = ImRaii.PushId(buttonIndex);
        using var disabled = ImRaii.Disabled(Disabled);

        if (ImGui.Button(Label, new Vector2(buttonWidth, ImGui.GetFrameHeight())))
        {
            ClickCallback();
            return true;
        }

        return false;
    }
}
