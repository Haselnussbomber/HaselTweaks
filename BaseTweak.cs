using Dalamud.Game;
using Dalamud.Utility.Signatures;

namespace HaselTweaks;

public abstract class BaseTweak
{
    protected HaselTweaks Plugin;

    public abstract string Name { get; }

    public virtual bool CanLoad => true;
    public virtual bool Ready { get; protected set; }
    public virtual bool Enabled { get; protected set; }

    public virtual void Setup(HaselTweaks plugin)
    {
        this.Plugin = plugin;
        SignatureHelper.Initialise(this);
        Ready = true;
    }

    public virtual void Enable()
    {
        Enabled = true;
    }

    public virtual void Disable()
    {
        Enabled = false;
    }

    public virtual void Dispose()
    {
        Ready = false;
    }

    public virtual void OnFrameworkUpdate(Framework framework) { }
}
