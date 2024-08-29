namespace HaselTweaks.Interfaces;

public interface IConfigurableTweak : ITweak
{
    void OnConfigOpen();
    void OnConfigClose();
    void OnConfigChange(string fieldName);
    void DrawConfig();
}
