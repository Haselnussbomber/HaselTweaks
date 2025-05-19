using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class BiggerItemDyeingPreview : ITweak
{
    private readonly IAddonLifecycle _addonLifecycle;

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

        var previewComponent = preview->GetAsAtkComponentPreview();
        if (previewComponent == null)
            return;

        var border = previewComponent->GetNodeById(3);
        var image = (AtkImageNode*)previewComponent->GetImageNodeById(4);
        if (border == null || image == null)
            return;

        itemNameNode->ToggleVisibility(false);
        itemNameWrappedNode->ToggleVisibility(true);

        rootNode->SetWidth(700);

        addon->SetSize(760, addon->WindowNode->Height);
        textBackgroundNode->SetWidth(leftDyeBox->Width);

        var scale = 0.56f;
        var width = (ushort)(576 * scale);
        var height = (ushort)(960 * scale);

        // preview
        previewParent->SetPositionFloat(previewParent->X - 3, previewParent->Y - 128);
        preview->SetPositionFloat(0, 0);
        preview->SetWidth(width);
        preview->SetHeight(height);
        previewParent->SetWidth(width);
        previewParent->SetHeight(height);

        // border
        border->SetWidth((ushort)(width + 8));
        border->SetHeight((ushort)(height + 10));

        // image
        image->SetWidth(width);
        image->SetHeight(height);

        for (var i = 0u; i < 9; i++)
        {
            var checkbox = addon->GetNodeById(72 + i);
            if (checkbox != null)
                checkbox->SetPositionFloat(checkbox->X + 50, checkbox->Y + 228);
        }
    }
}
