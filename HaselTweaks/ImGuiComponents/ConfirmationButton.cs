namespace HaselTweaks.ImGuiComponents;

public class ConfirmationButton
{
    public delegate void ClickCallbackDelegate();

    public string Label;
    public ClickCallbackDelegate ClickCallback;

    public bool Disabled { get; set; }

    public ConfirmationButton(string label, ClickCallbackDelegate clickCallback)
    {
        Label = label;
        ClickCallback = clickCallback;
    }

    internal bool Draw(int buttonIndex, float buttonWidth)
    {
        using var id = ImRaii.PushId(buttonIndex);

        if (Disabled)
            ImGui.BeginDisabled();

        var clicked = ImGui.Button(Label, new Vector2(buttonWidth, ImGui.GetFrameHeight()));

        if (Disabled)
            ImGui.EndDisabled();

        if (clicked)
        {
            ClickCallback();
        }

        return clicked;
    }
}
