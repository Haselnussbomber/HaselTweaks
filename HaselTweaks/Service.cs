using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselTweaks.Services;

namespace HaselTweaks;

public class Service
{
    public static AddonObserver AddonObserver { get; private set; } = null!;
    public static TranslationManager TranslationManager { get; private set; } = null!;
    public static StringManager StringManager { get; private set; } = null!;
    public static TextureManager TextureManager { get; private set; } = null!;
    public static WindowManager WindowManager { get; private set; } = null!;

    public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] public static ChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static ClientState ClientState { get; private set; } = null!;
    [PluginService] public static Condition Condition { get; private set; } = null!;
    [PluginService] public static Framework Framework { get; private set; } = null!;
    [PluginService] public static KeyState KeyState { get; private set; } = null!;

    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IGamepadState GamepadState { get; private set; } = null!;
    [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;

    public static void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
        PluginInterface.Create<Service>();
        AddonObserver = new();
        TranslationManager = new(PluginInterface, ClientState);
        StringManager = new();
        TextureManager = new();
        WindowManager = new(pluginInterface);
    }

    public static void Dispose()
    {
        AddonObserver.Dispose();
        TranslationManager.Dispose();
        TextureManager.Dispose();
        WindowManager.Dispose();
    }
}
