namespace HaselTweaks.Interfaces;

public interface IConfigurableTweak : ITweak
{
    void DrawConfig();
    void OnConfigChange(string fieldName);
    void OnConfigClose();
}
