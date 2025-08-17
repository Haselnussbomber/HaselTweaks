using System.IO;
using System.Text.Json.Serialization;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Math;
using HaselTweaks.JsonConverters;

namespace HaselTweaks.Records.PortraitHelper;

[JsonConverter(typeof(PortraitPresetConverter))]
public sealed record PortraitPreset
{
    public const int Magic = 0x53505448; // HTPS => HaselTweaks Preset String
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

    public string ToExportedString()
    {
        using var outputStream = new MemoryStream();
        using var writer = new BinaryWriter(outputStream);

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(CameraPosition);
        writer.Write(CameraTarget);
        writer.Write(ImageRotation);
        writer.Write(CameraZoom);
        writer.Write(BannerTimeline);
        writer.Write(AnimationProgress);
        writer.Write(Expression);
        writer.Write(HeadDirection);
        writer.Write(EyeDirection);
        writer.Write(DirectionalLightingColorRed);
        writer.Write(DirectionalLightingColorGreen);
        writer.Write(DirectionalLightingColorBlue);
        writer.Write(DirectionalLightingBrightness);
        writer.Write(DirectionalLightingVerticalAngle);
        writer.Write(DirectionalLightingHorizontalAngle);
        writer.Write(AmbientLightingColorRed);
        writer.Write(AmbientLightingColorGreen);
        writer.Write(AmbientLightingColorBlue);
        writer.Write(AmbientLightingBrightness);
        writer.Write(BannerBg);
        writer.Write(BannerFrame);
        writer.Write(BannerDecoration);
        writer.Flush();

        return Convert.ToBase64String(outputStream.ToArray());
    }

    public static PortraitPreset? FromExportedString(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] rawInput;
        try
        {
            rawInput = Convert.FromBase64String(input);
        }
        catch
        {
            return null;
        }

        if (rawInput.Length < 8)
            return null;

        using var inputStream = new MemoryStream(rawInput);
        using var reader = new BinaryReader(inputStream);

        var magic = reader.ReadInt32();
        if (magic != Magic)
            return null;

        var preset = new PortraitPreset
        {
            Version = reader.ReadUInt16()
        };

        switch (preset.Version)
        {
            case 1:
                preset.CameraPosition = reader.ReadHalfVector4();
                preset.CameraTarget = reader.ReadHalfVector4();
                preset.ImageRotation = reader.ReadInt16();
                preset.CameraZoom = reader.ReadByte();
                preset.BannerTimeline = reader.ReadUInt16();
                preset.AnimationProgress = reader.ReadSingle();
                preset.Expression = reader.ReadByte();
                preset.HeadDirection = reader.ReadHalfVector2();
                preset.EyeDirection = reader.ReadHalfVector2();
                preset.DirectionalLightingColorRed = reader.ReadByte();
                preset.DirectionalLightingColorGreen = reader.ReadByte();
                preset.DirectionalLightingColorBlue = reader.ReadByte();
                preset.DirectionalLightingBrightness = reader.ReadByte();
                preset.DirectionalLightingVerticalAngle = reader.ReadInt16();
                preset.DirectionalLightingHorizontalAngle = reader.ReadInt16();
                preset.AmbientLightingColorRed = reader.ReadByte();
                preset.AmbientLightingColorGreen = reader.ReadByte();
                preset.AmbientLightingColorBlue = reader.ReadByte();
                preset.AmbientLightingBrightness = reader.ReadByte();
                preset.BannerBg = reader.ReadUInt16();
                preset.BannerFrame = reader.ReadUInt16();
                preset.BannerDecoration = reader.ReadUInt16();
                break;

            default:
                throw new Exception($"Unknown Preset version {preset.Version}");
        }

        return preset;
    }


    public static unsafe PortraitPreset FromState()
    {
        var state = AgentBannerEditor.Instance()->EditorState;
        var preset = new PortraitPreset();

        var portraitData = stackalloc ExportedPortraitData[1];
        state->CharaView->ExportPortraitData(portraitData);
        preset.ReadExportedPortraitData(portraitData);

        preset.BannerFrame = state->BannerEntry.BannerFrame;
        preset.BannerDecoration = state->BannerEntry.BannerDecoration;

        return preset;
    }
}
