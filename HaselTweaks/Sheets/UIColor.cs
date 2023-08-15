using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace HaselTweaks.Sheets;

public class UIColor : Lumina.Excel.GeneratedSheets.UIColor
{
    private const int NumThemes = 4;

    public uint[] Colors { get; set; } = new uint[NumThemes];

    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        base.PopulateData(parser, gameData, language); // :rolling_eyes:

        for (var i = 0; i < NumThemes; i++)
            Colors[i] = parser.ReadColumn<uint>(i);
    }
}
