using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class BiggerItemDyeingPreview : ITweak
{
    private readonly IAddonLifecycle _addonLifecycle;

    public string InternalName => nameof(BiggerItemDyeingPreview);
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize() { }

    public void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "ColorantColoring", OnPostSetup);
    }

    public void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ColorantColoring", OnPostSetup);
    }

    void IDisposable.Dispose()
    {
        if (Status is TweakStatus.Disposed or TweakStatus.Outdated)
            return;

        OnDisable();

        Status = TweakStatus.Disposed;
    }

    private void OnPostSetup(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonSetupArgs setupArgs)
            return;

        var addon = (AtkUnitBase*)setupArgs.Addon;
        var rootNode = addon->RootNode;
        if (rootNode == null)
            return;

        var windowNode = addon->WindowNode;
        if (windowNode == null)
            return;

        var leftDyeBox = addon->GetNodeById(81);
        var itemNameNode = addon->GetNodeById(9);
        var itemNameWrappedNode = addon->GetNodeById(10);
        var textBackgroundNode = addon->GetNodeById(17);
        var previewParent = addon->GetNodeById(70);
        var preview = addon->GetNodeById(71);
        if (leftDyeBox == null || itemNameNode == null || itemNameWrappedNode == null || textBackgroundNode == null || previewParent == null || preview == null)
            return;

        var previewComponent = preview->GetComponent();
        if (previewComponent == null)
            return;

        var border = previewComponent->UldManager.SearchNodeById(3);
        var image = (AtkImageNode*)previewComponent->GetImageNodeById(4);
        if (border == null || image == null)
            return;

        itemNameNode->ToggleVisibility(false);
        itemNameWrappedNode->ToggleVisibility(true);

        rootNode->SetWidth(700);

        SetWindowSize(windowNode, 760, null);
        textBackgroundNode->SetWidth(leftDyeBox->Width);

        var scale = 0.56f;
        var width = (int)(576 * scale);
        var height = (int)(960 * scale);

        // preview
        previewParent->SetPositionFloat(previewParent->X - 3, previewParent->Y - 156);
        preview->SetPositionFloat(0, 0);
        SetSize(preview, width, height);
        SetSize(previewParent, width, height);

        // border
        SetSize(border, width + 8, height + 10);

        // image
        SetSize((AtkResNode*)image, width, height);

        // image sharpness
        image->Flags = 128;

        for (var i = 0u; i < 8; i++)
        {
            var checkbox = addon->GetNodeById(72 + i);
            if (checkbox != null)
                checkbox->SetPositionFloat(checkbox->X + 64, checkbox->Y + 224);
        }
    }
}
