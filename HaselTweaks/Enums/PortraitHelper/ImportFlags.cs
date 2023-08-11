namespace HaselTweaks.Enums.PortraitHelper;

[Flags]
public enum ImportFlags
{
    None = 0,
    BannerBg = 1 << 0,
    BannerFrame = 1 << 1,
    BannerDecoration = 1 << 2,
    BannerTimeline = 1 << 3,
    Expression = 1 << 4,
    AmbientLightingBrightness = 1 << 5,
    AmbientLightingColor = 1 << 6,
    DirectionalLightingBrightness = 1 << 7,
    DirectionalLightingColor = 1 << 8,
    DirectionalLightingVerticalAngle = 1 << 9,
    DirectionalLightingHorizontalAngle = 1 << 10,
    AnimationProgress = 1 << 11,
    CameraPosition = 1 << 12,
    CameraTarget = 1 << 13,
    HeadDirection = 1 << 14,
    EyeDirection = 1 << 15,
    CameraZoom = 1 << 16,
    ImageRotation = 1 << 17,
    All =
        BannerBg | BannerFrame | BannerDecoration |
        BannerTimeline | Expression | AmbientLightingBrightness |
        AmbientLightingColor | DirectionalLightingBrightness | DirectionalLightingColor |
        DirectionalLightingVerticalAngle | DirectionalLightingHorizontalAngle | AnimationProgress |
        CameraPosition | CameraTarget | HeadDirection |
        EyeDirection | CameraZoom | ImageRotation
}
