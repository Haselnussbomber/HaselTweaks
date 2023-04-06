using System.Globalization;
using HaselTweaks.JsonConverters;
using HaselTweaks.Structs;
using Newtonsoft.Json;

namespace HaselTweaks.Records.PortraitHelper;

[JsonConverter(typeof(PortraitPresetConverter))]
public sealed record PortraitPreset
{
    public ushort Version = 1;

    public HalfVector4 CameraPosition;
    public HalfVector4 CameraTarget;
    public short ImageRotation;
    public byte CameraZoom;
    public ushort BannerTimeline;
    public float AnimationProgress;
    public byte Expression;
    public HalfVector2 HeadDirection;
    public HalfVector2 EyeDirection;
    public byte DirectionalLightingColorRed;
    public byte DirectionalLightingColorGreen;
    public byte DirectionalLightingColorBlue;
    public byte DirectionalLightingBrightness;
    public short DirectionalLightingVerticalAngle;
    public short DirectionalLightingHorizontalAngle;
    public byte AmbientLightingColorRed;
    public byte AmbientLightingColorGreen;
    public byte AmbientLightingColorBlue;
    public byte AmbientLightingBrightness;
    public ushort BannerBg;

    public ushort BannerFrame;
    public ushort BannerDecoration;

    public unsafe bool ReadExportedPortraitData(ExportedPortraitData* portraitData)
    {
        if (portraitData == null)
            return false;

        CameraPosition = portraitData->CameraPosition;
        CameraTarget = portraitData->CameraTarget;
        ImageRotation = portraitData->ImageRotation;
        CameraZoom = portraitData->CameraZoom;
        BannerTimeline = portraitData->BannerTimeline;
        AnimationProgress = portraitData->AnimationProgress;
        Expression = portraitData->Expression;
        HeadDirection = portraitData->HeadDirection;
        EyeDirection = portraitData->EyeDirection;
        DirectionalLightingColorRed = portraitData->DirectionalLightingColorRed;
        DirectionalLightingColorGreen = portraitData->DirectionalLightingColorGreen;
        DirectionalLightingColorBlue = portraitData->DirectionalLightingColorBlue;
        DirectionalLightingBrightness = portraitData->DirectionalLightingBrightness;
        DirectionalLightingVerticalAngle = portraitData->DirectionalLightingVerticalAngle;
        DirectionalLightingHorizontalAngle = portraitData->DirectionalLightingHorizontalAngle;
        AmbientLightingColorRed = portraitData->AmbientLightingColorRed;
        AmbientLightingColorGreen = portraitData->AmbientLightingColorGreen;
        AmbientLightingColorBlue = portraitData->AmbientLightingColorBlue;
        AmbientLightingBrightness = portraitData->AmbientLightingBrightness;
        BannerBg = portraitData->BannerBg;

        return true;
    }

    public unsafe bool WriteExportedPortraitData(ExportedPortraitData* portraitData)
    {
        if (portraitData == null)
            return false;

        portraitData->CameraPosition = CameraPosition;
        portraitData->CameraTarget = CameraTarget;
        portraitData->ImageRotation = ImageRotation;
        portraitData->CameraZoom = CameraZoom;
        portraitData->BannerTimeline = BannerTimeline;
        portraitData->AnimationProgress = AnimationProgress;
        portraitData->Expression = Expression;
        portraitData->HeadDirection = HeadDirection;
        portraitData->EyeDirection = EyeDirection;
        portraitData->DirectionalLightingColorRed = DirectionalLightingColorRed;
        portraitData->DirectionalLightingColorGreen = DirectionalLightingColorGreen;
        portraitData->DirectionalLightingColorBlue = DirectionalLightingColorBlue;
        portraitData->DirectionalLightingBrightness = DirectionalLightingBrightness;
        portraitData->DirectionalLightingVerticalAngle = DirectionalLightingVerticalAngle;
        portraitData->DirectionalLightingHorizontalAngle = DirectionalLightingHorizontalAngle;
        portraitData->AmbientLightingColorRed = AmbientLightingColorRed;
        portraitData->AmbientLightingColorGreen = AmbientLightingColorGreen;
        portraitData->AmbientLightingColorBlue = AmbientLightingColorBlue;
        portraitData->AmbientLightingBrightness = AmbientLightingBrightness;
        portraitData->BannerBg = BannerBg;

        return true;
    }

    public string Serialize()
        => Version.ToString() + ":"
        + CameraPosition.X.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraPosition.Y.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraPosition.Z.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraPosition.W.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraTarget.X.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraTarget.Y.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraTarget.Z.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + CameraTarget.W.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + ImageRotation.ToString() + ":"
        + CameraZoom.ToString() + ":"
        + BannerTimeline.ToString() + ":"
        + AnimationProgress.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + Expression.ToString() + ":"
        + HeadDirection.X.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + HeadDirection.Y.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + EyeDirection.X.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + EyeDirection.Y.ToString("0.000", CultureInfo.InvariantCulture) + ":"
        + DirectionalLightingColorRed.ToString() + ":"
        + DirectionalLightingColorGreen.ToString() + ":"
        + DirectionalLightingColorBlue.ToString() + ":"
        + DirectionalLightingBrightness.ToString() + ":"
        + DirectionalLightingVerticalAngle.ToString() + ":"
        + DirectionalLightingHorizontalAngle.ToString() + ":"
        + AmbientLightingColorRed.ToString() + ":"
        + AmbientLightingColorGreen.ToString() + ":"
        + AmbientLightingColorBlue.ToString() + ":"
        + AmbientLightingBrightness.ToString() + ":"
        + BannerBg.ToString() + ":"
        + BannerFrame.ToString() + ":"
        + BannerDecoration.ToString();

    public static PortraitPreset Deserialize(string input)
    {
        var preset = new PortraitPreset();
        var parsed = false;

        var splitted = input.Split(':');
        if (splitted.Length == 0)
            throw new Exception("Invalid PortraitPreset string");

        preset.Version = ushort.Parse(splitted[0]);

        if (preset.Version == 1 && splitted.Length == 31)
        {
            preset.CameraPosition.X = Half.Parse(splitted[1], CultureInfo.InvariantCulture);
            preset.CameraPosition.Y = Half.Parse(splitted[2], CultureInfo.InvariantCulture);
            preset.CameraPosition.Z = Half.Parse(splitted[3], CultureInfo.InvariantCulture);
            preset.CameraPosition.W = Half.Parse(splitted[4], CultureInfo.InvariantCulture);
            preset.CameraTarget.X = Half.Parse(splitted[5], CultureInfo.InvariantCulture);
            preset.CameraTarget.Y = Half.Parse(splitted[6], CultureInfo.InvariantCulture);
            preset.CameraTarget.Z = Half.Parse(splitted[7], CultureInfo.InvariantCulture);
            preset.CameraTarget.W = Half.Parse(splitted[8], CultureInfo.InvariantCulture);
            preset.ImageRotation = short.Parse(splitted[9]);
            preset.CameraZoom = byte.Parse(splitted[10]);
            preset.BannerTimeline = ushort.Parse(splitted[11]);
            preset.AnimationProgress = float.Parse(splitted[12], CultureInfo.InvariantCulture);
            preset.Expression = byte.Parse(splitted[13]);
            preset.HeadDirection.X = Half.Parse(splitted[14], CultureInfo.InvariantCulture);
            preset.HeadDirection.Y = Half.Parse(splitted[15], CultureInfo.InvariantCulture);
            preset.EyeDirection.X = Half.Parse(splitted[16], CultureInfo.InvariantCulture);
            preset.EyeDirection.Y = Half.Parse(splitted[17], CultureInfo.InvariantCulture);
            preset.DirectionalLightingColorRed = byte.Parse(splitted[18]);
            preset.DirectionalLightingColorGreen = byte.Parse(splitted[19]);
            preset.DirectionalLightingColorBlue = byte.Parse(splitted[20]);
            preset.DirectionalLightingBrightness = byte.Parse(splitted[21]);
            preset.DirectionalLightingVerticalAngle = short.Parse(splitted[22]);
            preset.DirectionalLightingHorizontalAngle = short.Parse(splitted[23]);
            preset.AmbientLightingColorRed = byte.Parse(splitted[24]);
            preset.AmbientLightingColorGreen = byte.Parse(splitted[25]);
            preset.AmbientLightingColorBlue = byte.Parse(splitted[26]);
            preset.AmbientLightingBrightness = byte.Parse(splitted[27]);
            preset.BannerBg = ushort.Parse(splitted[28]);

            preset.BannerFrame = ushort.Parse(splitted[29]);
            preset.BannerDecoration = ushort.Parse(splitted[30]);

            parsed = true;
        }

        return !parsed ? throw new Exception("Invalid PortraitPreset string") : preset;
    }
}
