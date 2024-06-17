using System.Linq;
using System.Reflection;
using Dalamud.Hooking;
using HaselTweaks.Enums;
using HaselTweaks.Interfaces;

namespace HaselTweaks;

public abstract class Tweak : ITweak, IDisposable
{
    public string InternalName { get; set; }
    public TweakStatus Status { get; set; }

    protected Tweak()
    {
        InternalName = GetType().Name;
    }

    public virtual void OnInitialize() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnDispose() { }

    void IDisposable.Dispose()
    {
        if (Status == TweakStatus.Disposed)
            return;

        OnDisable();
        DisposeHooks();
        OnDispose();

        Status = TweakStatus.Disposed;

        GC.SuppressFinalize(this);
    }

    private void CallHooks(string methodName)
    {
        foreach (var field in GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field =>
                field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(Hook<>)))
        {
            var hook = field.GetValue(this);
            if (hook == null) continue;

            var type = methodName == "Dispose"
                ? typeof(IDisposable)
                : typeof(Hook<>).MakeGenericType(field.FieldType.GetGenericArguments().First());

            type.GetMethod(methodName)?.Invoke(hook, null);
        }
    }

    public void DisposeHooks() => CallHooks("Dispose");
}
