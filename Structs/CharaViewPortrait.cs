using System;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Utils;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x390)]
public unsafe partial struct CharaViewPortrait : ICreatable
{
    [FieldOffset(0)] public CharaView Base;

    // not sure on these vectors
    [FieldOffset(0x2A0)] public Vector4 CameraPosition;
    [FieldOffset(0x2B0)] public Vector4 CameraTargetPosition;
    [FieldOffset(0x2C0)] public Vector3 CameraRotation;
    [FieldOffset(0x2CC)] public short ImageRotation;
    [FieldOffset(0x2CE)] public byte CameraZoom;

    [FieldOffset(0x2D0)] public float CameraZoomNormalized;
    [FieldOffset(0x2D4)] public byte DirectionalLightingColorRed;
    [FieldOffset(0x2D5)] public byte DirectionalLightingColorGreen;
    [FieldOffset(0x2D6)] public byte DirectionalLightingColorBlue;
    [FieldOffset(0x2D7)] public byte DirectionalLightingBrightness;
    [FieldOffset(0x2D8)] public short CameraVerticalAngle;
    [FieldOffset(0x2DA)] public short CameraHorizontalAngle;
    [FieldOffset(0x2DC)] public byte AmbientLightingColorRed;
    [FieldOffset(0x2DD)] public byte AmbientLightingColorGreen;
    [FieldOffset(0x2DE)] public byte AmbientLightingColorBlue;
    [FieldOffset(0x2DF)] public byte AmbientLightingBrightness;

    [FieldOffset(0x2E6)] public short Background;
    [FieldOffset(0x2E8)] public byte BackgroundLoadingState; // 1 = to load, 3 = loaded ?

    [FieldOffset(0x2F0)] public AtkTexture Texture;

    [FieldOffset(0x308)] public CharaViewCharacterData PortraitCharacterData;
    [FieldOffset(0x370)] public bool Unk370;
    [FieldOffset(0x371)] public bool CharacterDataCopied;
    [FieldOffset(0x372)] public bool CharacterLoaded; // maybe?

    public Span<CharaViewItem> CharaViewItemSpan
    {
        get
        {
            fixed (byte* ptr = Base.Items)
            {
                return new Span<CharaViewItem>(ptr, sizeof(CharaViewItem));
            }
        }
    }

    public static CharaViewPortrait* Create()
    {
        return IMemorySpace.GetUISpace()->Create<CharaViewPortrait>();
    }

    [MemberFunction("E8 ?? ?? ?? ?? 48 89 83 ?? ?? ?? ?? EB 0B")]
    public partial void Ctor();

    [MemberFunction("48 89 5C 24 ?? 57 48 83 EC 20 48 8B F9 8B DA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? F6 C3 01 74 0D BA ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 48 8B C7 48 8B 5C 24 ?? 48 83 C4 20 5F C3 CC CC CC CC CC CC CC CC CC CC CC CC CC 33 C0")]
    public partial void Dtor();

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 43 10 C6 80 ?? ?? ?? ?? ?? 48 8B 4B 10")]
    public partial void Initialize(int a2, CharaViewCharacterData* characterData, long a4 = 0, int a5 = 0, long a6 = 0); // a2 = 4

    [MemberFunction("E8 ?? ?? ?? ?? 49 8B 4C 24 ?? 48 8B 01 FF 90 ?? ?? ?? ??")]
    public partial void SetupCamera(); // sets position, target, zoom etc.

    [MemberFunction("E8 ?? ?? ?? ?? 48 85 C0 75 05 0F 57 C9")]
    public partial IntPtr GetGameObject();

    [MemberFunction("E8 ?? ?? ?? ?? 0F 57 C9 F3 0F 5F C8 48 8B CF")]
    public partial float GetAnimationProgress();

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 93 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? E8 ?? ?? ?? ?? BE")]
    public partial void SetAmbientLightingColor(uint red, uint green, uint blue);

    [MemberFunction("E8 ?? ?? ?? ?? BE ?? ?? ?? ?? 48 8D 4D C7 8B C6 33 D2 89 11")]
    public partial void SetAmbientLightingBrightness(byte brightness);

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 93 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F B7 83")]
    public partial void SetDirectionalLightingColor(uint red, uint green, uint blue);

    [MemberFunction("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8D 4F 20 E8 ?? ?? ?? ?? 44 0F B7 83")]
    public partial void SetDirectionalLightingBrightness(byte brightness);

    [MemberFunction("E8 ?? ?? ?? ?? EB 5A 48 8D 4F 20")]
    public partial void SetDirectionalLightingAngle(short vertical, short horizontal);

    [MemberFunction("E8 ?? ?? ?? ?? EB 17 48 8D 4F 20")]
    public partial void SetCameraZoom(byte zoom);

    [MemberFunction("E8 ?? ?? ?? ?? 41 B5 01 49 63 46 48")]
    public partial void SetBackground(short id);

    [MemberFunction("E8 ?? ?? ?? ?? 48 89 BC 24 ?? ?? ?? ?? 84 DB")]
    public partial void SetPose(short id);

    [MemberFunction("E8 ?? ?? ?? ?? 84 DB 0F 84 ?? ?? ?? ?? 48 63 87 ?? ?? ?? ?? 45 33 C9")]
    public partial void SetExpression(byte id);

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 45 F2")]
    public partial IntPtr ExportPortraitData(ExportedPortraitData* output);

    [MemberFunction("E8 ?? ?? ?? ?? 83 BE ?? ?? ?? ?? ?? 4C 8B B4 24")]
    public partial IntPtr ImportPortraitData(ExportedPortraitData* input);
}

// exported by E8 ?? ?? ?? ?? 0F B7 45 F2
// imported by E8 ?? ?? ?? ?? 83 BE ?? ?? ?? ?? ?? 4C 8B B4 24
[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public unsafe struct ExportedPortraitData
{
    // compressed coordinates, see E8 ?? ?? ?? ?? 0F B7 43 10 48 8D 54 24
    [FieldOffset(0x0)] public short CameraPositionX;
    [FieldOffset(0x2)] public short CameraPositionY;
    [FieldOffset(0x4)] public short CameraPositionZ;
    [FieldOffset(0x6)] public short CameraPositionW;
    [FieldOffset(0x8)] public short CameraTargetX;
    [FieldOffset(0xA)] public short CameraTargetY;
    [FieldOffset(0xC)] public short CameraTargetZ;
    [FieldOffset(0xE)] public short CameraTargetW;
    [FieldOffset(0x10)] public short ImageRotation;
    [FieldOffset(0x12)] public byte CameraZoom;
    [FieldOffset(0x14)] public short Pose;
    [FieldOffset(0x18)] public float AnimationProgress;
    [FieldOffset(0x1C)] public byte Expression;
    [FieldOffset(0x1E)] public short HeadDirection1;
    [FieldOffset(0x20)] public short HeadDirection2;
    [FieldOffset(0x22)] public short EyeDirection1;
    [FieldOffset(0x24)] public short EyeDirection2;
    [FieldOffset(0x26)] public byte DirectionalLightingColorRed;
    [FieldOffset(0x27)] public byte DirectionalLightingColorGreen;
    [FieldOffset(0x28)] public byte DirectionalLightingColorBlue;
    [FieldOffset(0x29)] public byte DirectionalLightingBrightness;
    [FieldOffset(0x2A)] public short DirectionalLightingVerticalAngle;
    [FieldOffset(0x2C)] public short DirectionalLightingHorizontalAngle;
    [FieldOffset(0x2E)] public byte AmbientLightingColorRed;
    [FieldOffset(0x2F)] public byte AmbientLightingColorGreen;
    [FieldOffset(0x30)] public byte AmbientLightingColorBlue;
    [FieldOffset(0x31)] public byte AmbientLightingBrightness;
    [FieldOffset(0x32)] public short Background;
}
