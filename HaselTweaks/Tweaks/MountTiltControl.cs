using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;
using HaselTweaks.Structs;

namespace HaselTweaks.Tweaks;

public unsafe partial class MountTiltControl(IGameInteropProvider GameInteropProvider) : ITweak
{
    public string InternalName => nameof(MountTiltControl);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    private Hook<HaselEffectContainer.Delegates.Setup>? EffectContainerSetupHook;

    public void OnInitialize()
    {
        EffectContainerSetupHook = GameInteropProvider.HookFromAddress<HaselEffectContainer.Delegates.Setup>(
            HaselEffectContainer.MemberFunctionPointers.Setup,
            EffectContainerSetupDetour);
    }

    public void OnEnable()
    {
        EffectContainerSetupHook?.Enable();
    }

    public void OnDisable()
    {
        EffectContainerSetupHook?.Disable();
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();
        EffectContainerSetupHook?.Dispose();

        Status = TweakStatus.Disposed;
        GC.SuppressFinalize(this);
    }

    private void EffectContainerSetupDetour(HaselEffectContainer* thisPtr)
    {
        EffectContainerSetupHook!.Original(thisPtr);

        if (thisPtr->ContainerInterface.OwnerObject->GetObjectKind() != ObjectKind.Mount)
            return;

        thisPtr->UnkMountTiltField34 = 0;
        thisPtr->UnkMountTiltField38 = 0;
    }
}
