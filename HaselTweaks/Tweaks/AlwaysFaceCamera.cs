using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class AlwaysFaceCamera : Tweak
{
    private readonly IFramework _framework;

    public override void OnEnable()
    {
        _framework.Update += OnUpdate;
    }

    public override void OnDisable()
    {
        _framework.Update -= OnUpdate;
        DisableFaceCamera();
    }

    private void OnUpdate(IFramework framework)
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer == null || localPlayer->InCombat || localPlayer->GetTargetId() != 0xE0000000)
        {
            DisableFaceCamera();
            return;
        }

        var cameraManager = CameraManager.Instance();
        if (cameraManager == null || cameraManager->Camera == null || cameraManager->ActiveCameraIndex != 0)
        {
            DisableFaceCamera();
            return;
        }

        var playerForwardDirection = new Vector3(MathF.Sin(localPlayer->Rotation), 0f, MathF.Cos(localPlayer->Rotation));
        var directionToCamera = Vector3.Normalize(cameraManager->Camera->SceneCamera.Position - localPlayer->Position);
        var dot = Vector3.Dot(playerForwardDirection, directionToCamera);

        if (dot <= 0.0f)
        {
            DisableFaceCamera();
            return;
        }

        EnableFaceCamera();
        localPlayer->LookAt.CameraVector = cameraManager->Camera->SceneCamera.Position;
    }

    private void EnableFaceCamera()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer != null && (localPlayer->LookAt.FaceCameraFlag & 1) == 0)
            localPlayer->LookAt.FaceCameraFlag |= 1;
    }

    private void DisableFaceCamera()
    {
        var localPlayer = Control.GetLocalPlayer();
        if (localPlayer != null && (localPlayer->LookAt.FaceCameraFlag & 1) == 1)
            localPlayer->LookAt.FaceCameraFlag &= 0xFE;
    }
}
