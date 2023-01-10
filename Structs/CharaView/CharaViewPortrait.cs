using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x390)]
public unsafe partial struct CharaViewPortrait : ICreatable
{
    [FieldOffset(0)] public CharaView Base;

    // not sure on these floats. i think it's a Spherical Camera
    [FieldOffset(0x2A0)] public float CameraPhi;
    [FieldOffset(0x2A4)] public float CameraTheta;
    [FieldOffset(0x2A8)] public float CameraR;
    [FieldOffset(0x2AC)] public float CameraUnk2AC; // always 0?
    [FieldOffset(0x2B0)] public float CameraTargetX;
    [FieldOffset(0x2B4)] public float CameraTargetY;
    [FieldOffset(0x2B8)] public float CameraTargetZ;
    [FieldOffset(0x2BC)] public float CameraTargetW;
    [FieldOffset(0x2C0)] public float CameraRatio1;
    [FieldOffset(0x2C4)] public float CameraRatio2;
    [FieldOffset(0x2C8)] public float CameraRatio3;
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

    [FieldOffset(0x2E4)] public short PoseClassJob;
    [FieldOffset(0x2E6)] public short Background; // BannerBg rowId
    [FieldOffset(0x2E8)] public byte BackgroundState; // 0 = do nothing, 1 = loads texture by icon from row, 2 = renders KernelTexture?, 3 = done

    [FieldOffset(0x2F0)] public AtkTexture BackgroundTexture;

    [FieldOffset(0x308)] public CharaViewCharacterData PortraitCharacterData;
    [FieldOffset(0x370)] public bool CharacterVisible;
    [FieldOffset(0x371)] public bool CharacterDataCopied;
    [FieldOffset(0x372)] public bool CharacterLoaded;

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

    [VirtualFunction(0)]
    public partial void Dtor(bool freeMemory);

    /* This is the base CharaView initializer. Use the other one below.
    [VirtualFunction(1)]
    public partial void Initialize(CharaViewCharacterData* characterData, int objectIndex, IntPtr agentCallbackReady);
    */

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 43 10 C6 80 ?? ?? ?? ?? ?? 48 8B 4B 10")]
    public partial void Initialize(int clientObjectId, CharaViewCharacterData* characterData, long a4, int a5, long a6); // a4 is set to +0x378, a5 is set to +0x380, a6 is set to +0x388

    [VirtualFunction(2)]
    public partial void Release();

    [VirtualFunction(3)]
    public partial void ResetPositions();

    // vf4?

    [VirtualFunction(5)]
    public partial void SetCameraRotation(float a2, float a3); // maybe?

    // vf6?

    // vf7? called by Render()

    // vf8?

    // vf9?

    [VirtualFunction(10)]
    public partial void Update();

    [VirtualFunction(11)]
    public partial bool IsGameObjectReady(CharaViewGameObject* obj);

    [MemberFunction("E8 ?? ?? ?? ?? 49 8B 4C 24 ?? 48 8B 01 FF 90")]
    public partial void ResetCamera(); // sets position, target, zoom etc.

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 43 10 48 8D 4C 24")]
    public partial void SetCameraPosition(CompressedVector4* cam, CompressedVector4* target);

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 93 ?? ?? ?? ?? 0F 28 D0")]
    public partial float GetAnimationTime(); // as Vector3?

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 96 ?? ?? ?? ?? 48 8B 8E ?? ?? ?? ?? E8 ?? ?? ?? ?? BB")]
    public partial void SetAmbientLightingColor(uint red, uint green, uint blue);

    [MemberFunction("E8 ?? ?? ?? ?? BB ?? ?? ?? ?? 48 8D 4D C7 8B C3 33 D2 89 11")]
    public partial void SetAmbientLightingBrightness(byte brightness);

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 96 ?? ?? ?? ?? 48 8B 8E ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F B7 86")]
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

    [MemberFunction("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 0F B7 93")]
    public partial void SetPoseTimed(short id, float time);

    [MemberFunction("E8 ?? ?? ?? ?? 84 DB 0F 84 ?? ?? ?? ?? 48 63 87 ?? ?? ?? ?? 45 33 C9")]
    public partial void SetExpression(byte id); // same as GetGameObject()->Unk8D0.SetExpression(id)

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 45 F2")]
    public partial IntPtr ExportPortraitData(ExportedPortraitData* output);

    [MemberFunction("E8 ?? ?? ?? ?? 83 BE ?? ?? ?? ?? ?? 4C 8B B4 24 ?? ?? ?? ?? 74 2D")]
    public partial IntPtr ImportPortraitData(ExportedPortraitData* input);

    /// <summary>Use this after manually setting camera positions.</summary>
    [MemberFunction("E8 ?? ?? ?? ?? F3 0F 10 53 ?? 48 8B CF")]
    public partial void ApplyCameraPositions();

    [MemberFunction("E8 ?? ?? ?? ?? 0F B7 43 24 66 85 C0 75 05 0F 28 D6 EB 35 0F B7 D0 8B CA 8B C2 C1 E9 0A 81 E2 ?? ?? ?? ?? 83 E1 1F C1 E0 10 C1 E1 17 25 ?? ?? ?? ?? 81 C1 ?? ?? ?? ?? C1 E2 0D 0B C8 0B CA 89 4C 24 40")]
    public partial void SetHeadDirection(float a2, float a3);

    [MemberFunction("E8 ?? ?? ?? ?? 44 0F B6 4B ?? 48 8B CF 44 0F B6 43 ?? 0F B6 53 26 E8 ?? ?? ?? ?? 48 8B 4F 18 0F B6 43 29 F3 0F 10 35 ?? ?? ?? ?? 88 87 ?? ?? ?? ?? 48 85 C9 74 13 0F B6 C0 66 0F 6E C8 0F 5B C9 F3 0F 5E CE E8 ?? ?? ?? ?? 0F B7 43 2C 0F B7 53 2A 48 8B 4F 18 66 89 97 ?? ?? ?? ?? 66 89 87 ?? ?? ?? ?? 48 85 C9 74 17 98 66 0F 6E D0 0F BF C2 0F 5B D2 66 0F 6E C8 0F 5B C9 E8 ?? ?? ?? ?? 44 0F B6 4B ?? 48 8B CF 44 0F B6 43 ?? 0F B6 53 2E E8 ?? ?? ?? ?? 48 8B 4F 18 0F B6 43 31 88 87 ?? ?? ?? ?? 48 85 C9 74 13 0F B6 C0 66 0F 6E C8 0F 5B C9 F3 0F 5E CE E8 ?? ?? ?? ?? 0F B7 43 32 48 8B 5C 24")]
    public partial void SetEyeDirection(float a2, float a3);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B DD BA ?? ?? ?? ?? 48 C1 E3 04 49 03 DE")]
    public partial void ToggleCharacterVisibility(bool visible);

    [MemberFunction("E8 ?? ?? ?? ?? 0F B6 45 92 C7 83")]
    public partial void ToggleGearVisibility(bool hideVisor, bool hideWeapon, bool closeVisor);
}

[StructLayout(LayoutKind.Explicit, Size = 0x8)]
public unsafe partial struct CompressedVector4
{
    [FieldOffset(0x0)] public short X;
    [FieldOffset(0x2)] public short Y;
    [FieldOffset(0x4)] public short Z;
    [FieldOffset(0x6)] public short W;

    [MemberFunction("E8 ?? ?? ?? ?? 8B 7D A8")]
    public partial void Compress(float x, float y, float z, float w = 1.0f);

    public static CompressedVector4* From(float x, float y, float z, float w = 1.0f)
    {
        var cvec = (CompressedVector4*)IMemorySpace.GetUISpace()->Malloc<CompressedVector4>();
        cvec->Compress(x, y, z, w);
        return cvec;
    }

    public static CompressedVector4* From(System.Numerics.Vector3 vec)
    {
        var cvec = (CompressedVector4*)IMemorySpace.GetUISpace()->Malloc<CompressedVector4>();
        cvec->Compress(vec.X, vec.Y, vec.Z);
        return cvec;
    }

    public static CompressedVector4* From(System.Numerics.Vector4 vec)
    {
        var cvec = (CompressedVector4*)IMemorySpace.GetUISpace()->Malloc<CompressedVector4>();
        cvec->Compress(vec.X, vec.Y, vec.Z, vec.W);
        return cvec;
    }
}

// exported by "E8 ?? ?? ?? ?? 0F B7 45 F2"
// imported by "E8 ?? ?? ?? ?? 83 BE ?? ?? ?? ?? ?? 4C 8B B4 24 ?? ?? ?? ?? 74 2D"
[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public unsafe struct ExportedPortraitData
{
    // compressed coordinates, see "E8 ?? ?? ?? ?? 0F B7 43 10 48 8D 4C 24"
    [FieldOffset(0x0)] public short CameraPhi;
    [FieldOffset(0x2)] public short CameraTheta;
    [FieldOffset(0x4)] public short CameraR;
    [FieldOffset(0x6)] public short CameraUnk2AC;
    [FieldOffset(0x8)] public short CameraTarget1;
    [FieldOffset(0xA)] public short CameraTarget2;
    [FieldOffset(0xC)] public short CameraTarget3;
    [FieldOffset(0xE)] public short CameraTarget4;
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
