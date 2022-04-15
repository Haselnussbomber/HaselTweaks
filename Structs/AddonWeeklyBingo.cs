using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// Component::GUI::AddonWeeklyBingo
//   Component::GUI::AtkUnitBase
//     Component::GUI::AtkEventListener
[StructLayout(LayoutKind.Explicit, Size = 0x23C8)]
public unsafe struct AddonWeeklyBingo
{
    [FieldOffset(0x0)] public AtkUnitBase AtkUnitBase;
    [FieldOffset(0x220)] public DutySlotList DutySlots;
    [FieldOffset(0x18E8)] public StateDisplayManager StateDisplay;
    [FieldOffset(0x1938)] public StickerList Stickers;
    [FieldOffset(0x1EC8)] public fixed byte Unk1EC8[24];
    [FieldOffset(0x1EE0)] public AtkResNode* CrossLine1ResNode;
    [FieldOffset(0x1EE8)] public AtkResNode* CrossLine2ResNode;
    [FieldOffset(0x1EF0)] public AtkResNode* CrossLine3ResNode;
    [FieldOffset(0x1EF8)] public AtkComponentBase* CrossLine1BaseComponentNode;
    [FieldOffset(0x1F00)] public AtkComponentBase* CrossLine2BaseComponentNode;
    [FieldOffset(0x1F18)] public AtkComponentBase* CrossLine3BaseComponentNode;
    [FieldOffset(0x1F10)] public fixed byte Unk1F10[8]; // 2x uint? first could be NumCrossLinesShown
    [FieldOffset(0x1F18)] public AtkTextNode* NumStickersPlacedTextNode;
    [FieldOffset(0x1F20)] public uint NumStickersPlaced;
    [FieldOffset(0x1F24)] public fixed byte Unk1F24[4];
    [FieldOffset(0x1F28)] public RewardsList Rewards;
    [FieldOffset(0x2380)] public AtkResNode* ResNode;
    [FieldOffset(0x2380)] public AtkCollisionNode* CollisionNode;
    [FieldOffset(0x2390)] public fixed byte Unk2390[4];
    [FieldOffset(0x2394)] public bool InDutySlotResetMode; // actually allocated as uint32
    [FieldOffset(0x2398)] public void* Unk2398; // FunctionPtr
    [FieldOffset(0x23A0)] public void* Unk23A0; // FunctionPtr
    [FieldOffset(0x23A8)] public void* Unk23A8; // FunctionPtr
    [FieldOffset(0x23B0)] public void* Unk23B0; // FunctionPtr
    [FieldOffset(0x23B8)] public void* Unk23B8; // FunctionPtr
    [FieldOffset(0x23C0)] public void* Unk23C0; // FunctionPtr

    public enum DutySlotStatus : byte
    {
        Open = 0,
        Claimable = 1,
        Claimed = 2,
    }

    // 48 8B C2 48 89 51 08 48 8B D9
    [StructLayout(LayoutKind.Explicit, Size = 0x168)]
    public unsafe struct DutySlot
    {
        [FieldOffset(0)] public AtkEventListener AtkEventListener;
        [FieldOffset(0x08)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x10)] public uint Index;
        [FieldOffset(0x19)] public DutySlotStatus Status; // initialized as ushort at 0x18?
        [FieldOffset(0x32)] public fixed byte TooltipText[100];
        [FieldOffset(0x138)] public AtkComponentButton* ComponentBase;
        [FieldOffset(0x140)] public AtkImageNode* ImageNode;
        [FieldOffset(0x148)] public AtkResNode* ResNode;
        [FieldOffset(0x150)] public AtkResNode* CloverleafResNode;
        [FieldOffset(0x158)] public AtkTextNode* CloverleafTextNode;
        [FieldOffset(0x160)] public AtkResNode* StickerResNode;
    }

    // E8 ?? ?? ?? ?? BA ?? ?? ?? ?? 48 89 9B ?? ?? ?? ??
    [StructLayout(LayoutKind.Explicit, Size = 0x16C8)]
    public unsafe struct DutySlotList
    {
        public const int Length = 16;

        [FieldOffset(0x0)] public void* vtbl; // ?
        [FieldOffset(0x08)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x10)] public fixed byte Unk10[20];
        [FieldOffset(0x24)] public uint NumSecondChances;
        [FieldOffset(0x28)] private fixed byte data[Length * 0x168];
        [FieldOffset(0x16A8)] public AtkComponentButton* SecondChanceButton;
        [FieldOffset(0x16B0)] public AtkComponentButton* SecondChanceCancelButton;
        [FieldOffset(0x16B8)] public AtkTextNode* SecondChancePointsTextNode;
        [FieldOffset(0x16C0)] public AtkResNode* DutySlotsResNode;

        public DutySlot* this[int i]
        {
            get
            {
                if (i < 0 || i > Length) return null;
                fixed (byte* p = data)
                {
                    return (DutySlot*)(p + sizeof(DutySlot) * i);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x168)]
    public unsafe struct Sticker
    {
        [FieldOffset(0x0)] public void* vtbl; // AtkEventListener*?
        [FieldOffset(0x08)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x10)] public uint Index;
        [FieldOffset(0x14)] public bool IsStickerSet; // uint16?
        [FieldOffset(0x20)] public AtkComponentButton* ButtonNode;
        [FieldOffset(0x28)] public AtkComponentBase* IconBaseNode;
        [FieldOffset(0x30)] public AtkComponentBase* IconShadowBaseNode;
        [FieldOffset(0x38)] public AtkResNode* IconResNode;
        [FieldOffset(0x40)] public AtkResNode* IconShadowResNode;
        [FieldOffset(0x48)] public void* Unk48;
        [FieldOffset(0x50)] public void* Unk50;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x590)]
    public unsafe struct StickerList
    {
        public const int Length = 16;

        [FieldOffset(0x0)] public void* vtbl; // ?
        [FieldOffset(0x08)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x10)] private fixed byte data[Length * 0x58];

        public Sticker* this[int i]
        {
            get
            {
                if (i < 0 || i > Length) return null;
                fixed (byte* p = data)
                {
                    return (Sticker*)(p + sizeof(Sticker) * i);
                }
            }
        }
    }

    // E8 ?? ?? ?? ?? 39 47 3C
    [StructLayout(LayoutKind.Explicit, Size = 0x50)]
    public unsafe struct StateDisplayManager
    {
        public const int Length = 6;

        [FieldOffset(0x0)] public void* vtbl; // ?

        [FieldOffset(0x08)] private fixed byte data[Length * 8];

        // 1) No more seals can be applied. Deliver the journal to Khloe Aliapoh to receive your reward.
        [FieldOffset(0x08)] public IntPtr FullSealsText;

        // 2) One or more lines of seals have been completed. Deliver the journal to Khloe Aliapoh to receive your reward or continue adventuring to add more seals.
        [FieldOffset(0x10)] public IntPtr OneOrMoreLinesText;

        // 3) Second Chance points can be used to increase your chances of completing lines.
        [FieldOffset(0x18)] public IntPtr SecondChancePointsText;

        // 4) Select a completed duty to receive a seal.
        [FieldOffset(0x20)] public IntPtr ReceiveSealCompleteText;

        // 5) Complete a task to receive a seal.
        [FieldOffset(0x28)] public IntPtr ReceiveSealIncompleteText;

        // 6) Select a completed duty to be rendered incomplete.
        [FieldOffset(0x30)] public IntPtr SecondChanceRetryText;

        [FieldOffset(0x38)] public fixed byte Unk38[4]; // bool as uint?
        [FieldOffset(0x3C)] public uint CurrentTextIndex;
        [FieldOffset(0x40)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x48)] public AtkTextNode* TextNode;

        public IntPtr* this[int i]
        {
            get
            {
                if (i < 0 || i > Length) return null;
                fixed (byte* p = data)
                {
                    return (IntPtr*)(p + sizeof(IntPtr) * i);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xD8)]
    public unsafe struct Reward
    {
        [FieldOffset(0x0)] public uint Index;
        [FieldOffset(0x04)] public uint ItemID;
        [FieldOffset(0x08)] public uint Amount;
        [FieldOffset(0x0C)] public fixed byte UnkC[4];
        [FieldOffset(0x10)] public void* ItemPtr; // exd row pointer?
        [FieldOffset(0x18)] public AtkComponentButton* ButtonNode;
        [FieldOffset(0x20)] public AtkImageNode* SelectedOverlayImageNode; // maybe
        [FieldOffset(0x28)] public AtkComponentTextNineGrid* TextNineGrid; // no clue
        [FieldOffset(0x30)] public AtkComponentIcon* IconComponentNode;
        [FieldOffset(0x38)] public AtkTextNode* AmountTextNode;
        [FieldOffset(0x40)] public AtkCollisionNode* CollisionNode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x40)]
    public unsafe struct ExperienceReward
    {
        [FieldOffset(0x0)] public AtkResNode* RewardsLine3ResNode;
        [FieldOffset(0x08)] public void* Unk8;
        [FieldOffset(0x10)] public void* Unk10;
        [FieldOffset(0x18)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x20)] public uint Experience;
        [FieldOffset(0x24)] public fixed byte Unk24[4];
        [FieldOffset(0x28)] public char* TooltipText;
        [FieldOffset(0x30)] public AtkComponentButton* ButtonNode;
        [FieldOffset(0x38)] public AtkImageNode* ImageNode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x100)]
    public unsafe struct RewardsLine
    {
        public const int Length = 3;

        [FieldOffset(0x0)] public void* vtbl; // ?
        [FieldOffset(0x08)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x10)] private fixed byte data[Length * 0xD8];
        [FieldOffset(0x10)] public Reward* Reward1;
        [FieldOffset(0x58)] public Reward* Reward2;
        [FieldOffset(0xA0)] public Reward* Reward3;
        [FieldOffset(0xE8)] public fixed byte UnkE8[8]; // first byte might be PickedRewardIndex?
        [FieldOffset(0xF0)] public AtkResNode* WrapperResNode;
        [FieldOffset(0xF8)] public AtkResNode* ResNode;

        public Reward* this[int i]
        {
            get
            {
                if (i < 0 || i > Length) return null;
                fixed (byte* p = data)
                {
                    return (Reward*)(p + sizeof(Reward) * i);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x458)]
    public unsafe struct RewardsList
    {
        public const int Length = 4;

        [FieldOffset(0x0)] public void* vtbl; // ?
        [FieldOffset(0x08)] public void* Addon; // AddonWeeklyBingo*
        [FieldOffset(0x10)] public AtkResNode* ResNode;
        [FieldOffset(0x18)] private fixed byte data[Length * 0x100];
        [FieldOffset(0x18)] public RewardsLine* RewardsLine1;
        [FieldOffset(0x118)] public RewardsLine* RewardsLine2;
        [FieldOffset(0x218)] public RewardsLine* RewardsLine3;
        [FieldOffset(0x318)] public RewardsLine* RewardsLine4;
        [FieldOffset(0x418)] public ExperienceReward* ExperienceReward;

        public RewardsLine* this[int i]
        {
            get
            {
                if (i < 0 || i > Length) return null;
                fixed (byte* p = data)
                {
                    return (RewardsLine*)(p + sizeof(RewardsLine) * i);
                }
            }
        }
    }
}
