namespace HaselTweaks.Records;

public readonly record struct IterationArgs(int Index, int Count)
{
    public bool IsFirst => Index == 0;
    public bool IsLast => Index == Count - 1;
}
