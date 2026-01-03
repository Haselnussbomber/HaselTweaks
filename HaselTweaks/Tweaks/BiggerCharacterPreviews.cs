using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class BiggerCharacterPreviews : ConfigurableTweak<BiggerCharacterPreviewsConfiguration>
{
    private static readonly string[] AddonNames = ["Character", "CharacterInspect", "ColorantColoring", "Tryon"];
    private static readonly Vector2 CharaCardSize = new(576, 960); // native texture size

    private readonly IAddonLifecycle _addonLifecycle;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "Character", OnCharacterPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "CharacterInspect", OnCharacterInspectPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PostDraw, "CharacterInspect", OnCharacterInspectPostDraw);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "ColorantColoring", OnColorantColoringPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "Tryon", OnTryonPostSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, AddonNames, OnPostSetupOrRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PostRefresh, AddonNames, OnPostSetupOrRefresh);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Character", OnCharacterPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "CharacterInspect", OnCharacterInspectPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PostDraw, "CharacterInspect", OnCharacterInspectPostDraw);
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "ColorantColoring", OnColorantColoringPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Tryon", OnTryonPostSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, AddonNames, OnPostSetupOrRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PostRefresh, AddonNames, OnPostSetupOrRefresh);
    }

    private void OnCharacterPostSetup(AddonEvent type, AddonArgs args)
    {
        if (!_config.EnableCharacter)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;

        var previewNode = addon->GetNodeById(72);
        if (previewNode == null) return;

        var previewComponent = previewNode->GetAsAtkComponentPreview();
        if (previewComponent == null) return;

        var windowNode = addon->WindowNode;
        if (windowNode == null)
            return;

        // resize addon
        var extraWidth = new Vector2(36, 0);
        addon->Size += extraWidth;

        // resize window
        var windowComponent = windowNode->GetAsAtkComponentWindow();
        IncreaseNodeSize(&windowComponent->OwnerNode->AtkResNode, extraWidth.XOnly());
        IncreaseNodeSize(windowComponent->GetNodeById(2), extraWidth);
        for (var i = 0u; i < 5; i++)
            IncreaseNodeSize(windowComponent->GetNodeById(9 + i), extraWidth);

        // window header buttons
        for (var i = 0u; i < 4; i++)
            MoveNode(windowComponent->GetNodeById(5 + i), extraWidth);

        // move tabs
        for (var i = 0u; i < 4; i++)
            MoveNode(addon->GetNodeById(4 + i), extraWidth / 2f);

        // move name
        IncreaseNodeSize(addon->GetNodeById(2), extraWidth);

        // move gear set update button
        MoveNode(addon->GetNodeById(18), extraWidth);

        // resize Class/Job background
        IncreaseNodeSize(addon->GetNodeById(69), extraWidth.XOnly());

        // resize preview
        previewComponent->Size = CharaCardSize * 0.40f;

        // move slots
        for (var i = 0u; i < 6; i++)
        {
            MoveNode(addon->GetNodeById(56 + i), extraWidth); // component node
            MoveNode(addon->GetNodeById(41 + i), extraWidth); // background node
        }

        // shield has to be special
        MoveNode(addon->GetNodeById(50), extraWidth); // component node
        MoveNode(addon->GetNodeById(35), extraWidth); // background node

        // move average item level
        MoveNode(addon->GetNodeById(70), extraWidth); // text
        MoveNode(addon->GetNodeById(71), extraWidth); // icon

        // move error text
        MoveNode(addon->GetNodeById(8), new Vector2(20, 0)); // text
        MoveNode(addon->GetNodeById(9), new Vector2(20, 0)); // background

        // move buttons
        for (var i = 0u; i < 6; i++)
            MoveNode(addon->GetNodeById(73 + i), new Vector2(20, 70));
    }

    private void OnCharacterInspectPostSetup(AddonEvent type, AddonArgs args)
    {
        if (!_config.EnableCharacterInspect)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;

        var previewNode = addon->GetNodeById(41);
        if (previewNode == null) return;

        var previewComponent = previewNode->GetAsAtkComponentPreview();
        if (previewComponent == null) return;

        var extraSize = new Vector2(86, 174);
        var extraWidth = extraSize.XOnly();

        // resize addon
        addon->Size += extraSize;

        // resize Class/Job background
        IncreaseNodeSize(addon->GetNodeById(26), extraWidth);

        // resize separator
        IncreaseNodeSize(addon->GetNodeById(40), extraWidth);

        // resize preview
        previewComponent->Size = CharaCardSize * 0.5f;
        MoveNode((AtkResNode*)previewComponent->OwnerNode, -new Vector2(16, 0));
        MoveNode(previewComponent->GetNodeById(2), new Vector2(59, 0)); // error text

        // move slots
        for (var i = 0u; i < 6; i++)
        {
            MoveNode(addon->GetNodeById(49 + i), extraWidth); // component node
            MoveNode(addon->GetNodeById(63 + i), extraWidth); // background node
        }

        // shield has to be special
        MoveNode(addon->GetNodeById(43), extraWidth); // component node
        MoveNode(addon->GetNodeById(57), extraWidth); // background node

        // move buttons
        var buttonOffset = new Vector2(40, 197);
        MoveNode(addon->GetNodeById(6), buttonOffset);
        MoveNode(addon->GetNodeById(72), buttonOffset);
        MoveNode(addon->GetNodeById(73), buttonOffset);
    }

    // support for Simple Tweaks "Item Level in Examine" tweak
    private void OnCharacterInspectPostDraw(AddonEvent type, AddonArgs args)
    {
        if (!_config.EnableCharacterInspect)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;

        var previewNode = addon->GetNodeById(41);
        if (previewNode == null) return;

        var previewComponent = previewNode->GetAsAtkComponentPreview();
        if (previewComponent == null) return;

        var textNode = previewComponent->GetTextNodeById(2);
        if (textNode == null)
            return;

        var itemLevelTextNode = (AtkTextNode*)textNode->PrevSiblingNode;
        if (itemLevelTextNode == null || itemLevelTextNode->GetNodeType() != NodeType.Text)
            return;

        var itemLevelTextImage = (AtkImageNode*)itemLevelTextNode->PrevSiblingNode;
        if (itemLevelTextImage == null || itemLevelTextImage->GetNodeType() != NodeType.Image)
            return;

        var newTextPos = new Vector2(previewNode->Width - itemLevelTextNode->Width, 0);
        if (itemLevelTextNode->AtkResNode.Position == newTextPos)
            return;

        itemLevelTextNode->AtkResNode.Position = newTextPos;
        itemLevelTextImage->AtkResNode.Position = new Vector2(newTextPos.X, itemLevelTextImage->Y);
    }

    private void OnColorantColoringPostSetup(AddonEvent type, AddonArgs args)
    {
        if (!_config.EnableColorantColoring)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;

        var previewNode = addon->GetNodeById(71);
        if (previewNode == null) return;

        var previewComponent = previewNode->GetAsAtkComponentPreview();
        if (previewComponent == null) return;

        // resize addon
        addon->Size += new Vector2(106, 0);

        // resize item name background
        var leftDyeBox = addon->GetNodeById(81);
        var textBackgroundNode = addon->GetNodeById(17);
        if (leftDyeBox != null && textBackgroundNode != null)
            textBackgroundNode->SetWidth(leftDyeBox->Width);

        // resize preview
        previewNode->Position = Vector2.Zero;
        previewComponent->Size = CharaCardSize * 0.56f;
        MoveNode(addon->GetNodeById(70), -new Vector2(3, 128));

        // move buttons
        for (var i = 0u; i < 9; i++)
            MoveNode(addon->GetNodeById(72 + i), new Vector2(50, 228));
    }

    private void OnTryonPostSetup(AddonEvent type, AddonArgs args)
    {
        if (!_config.EnableTryon)
            return;

        var addon = (AtkUnitBase*)args.Addon.Address;

        var previewNode = addon->GetNodeById(31);
        if (previewNode == null) return;

        var previewComponent = previewNode->GetAsAtkComponentPreview();
        if (previewComponent == null) return;

        // resize addon
        addon->Size += new Vector2(94, 160);

        // resize preview
        previewComponent->Size = CharaCardSize * 0.50f;

        // move slots
        var offset = new Vector2(94, 0);
        for (var i = 0u; i < 5; i++)
        {
            MoveNode(addon->GetNodeById(11 + i), offset); // component node
            MoveNode(addon->GetNodeById(26 + i), offset); // background node
        }

        // shield has to be special
        MoveNode(addon->GetNodeById(5), offset); // component node
        MoveNode(addon->GetNodeById(19), offset); // background node

        // move buttons
        for (var i = 0u; i < 7; i++)
            MoveNode(addon->GetNodeById(32 + i), new Vector2(59, 164));
    }

    private void IncreaseNodeSize(AtkResNode* node, Vector2 size)
    {
        if (node != null)
            node->Size += size;
    }

    private void MoveNode(AtkResNode* node, Vector2 size)
    {
        if (node != null)
            node->Position += size;
    }

    private void OnPostSetupOrRefresh(AddonEvent type, AddonArgs args)
    {
        UpdatePreviewSharpening((AtkUnitBase*)args.Addon.Address);
    }

    private void UpdatePreviewSharpening(AtkUnitBase* addon)
    {
        if (addon == null)
            return;

        var nodeId = addon->NameString switch
        {
            "Character" => 72u,
            "CharacterInspect" => 41u,
            "ColorantColoring" => 71u,
            "Tryon" => 31u,
            _ => 0u
        };

        var sharpen = addon->NameString switch
        {
            "Character" => _config.SharpenCharacter,
            "CharacterInspect" => _config.SharpenCharacterInspect,
            "ColorantColoring" => _config.SharpenColorantColoring,
            "Tryon" => _config.SharpenTryon,
            _ => false
        };

        if (nodeId == 0)
            return;

        var previewNode = addon->GetNodeById(nodeId);
        if (previewNode == null)
            return;

        var previewComponent = previewNode->GetAsAtkComponentPreview();
        if (previewComponent == null)
            return;

        var imageNode = previewComponent->ImageNode;
        if (imageNode == null)
            return;

        imageNode->Flags.SetFlag((ImageNodeFlags)4, !sharpen);
        imageNode->Flags.SetFlag((ImageNodeFlags)8, !sharpen);
    }
}
