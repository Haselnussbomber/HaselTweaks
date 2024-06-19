namespace HaselTweaks.Interfaces;

public interface IConfigurableTweak : ITweak
{
    void DrawConfig();
    void OnConfigOpen();
    void OnConfigChange(string fieldName);
    void OnConfigClose();
}
