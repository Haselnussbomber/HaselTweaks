using Dalamud.Game;
using Dalamud.Utility.Signatures;

namespace HaselTweaks;

public abstract class Tweak
{
    protected Plugin Plugin = null!;

    public string InternalName => GetType().Name;
    public abstract string Name { get; }

    public virtual bool CanLoad => true;
    public virtual bool ForceLoad => false;
    public virtual bool Ready { get; protected set; }
    public virtual bool Enabled { get; protected set; }

    internal virtual void SetupInternal(Plugin plugin)
    {
        this.Plugin = plugin;
        SignatureHelper.Initialise(this);
        Ready = true;
        Setup();
    }

    internal virtual void EnableInternal()
    {
        Enabled = true;
        Enable();
    }

    internal virtual void DisableInternal()
    {
        Enabled = false;
        Disable();
    }

    internal virtual void DisposeInternal()
    {
        Ready = false;
        Dispose();
    }

    public virtual void Setup() { }
    public virtual void Enable() { }
    public virtual void Disable() { }
    public virtual void Dispose() { }
    public virtual void OnFrameworkUpdate(Framework framework) { }
}
