using BopCustomTextures.Json;
using BopCustomTextures.Config;
using BopCustomTextures.Customs;
using BopCustomTextures.Logging;
using BopCustomTextures.EventTemplates;
using BepInEx;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Diagnostics;
using LogLevel = BopCustomTextures.Logging.LogLevel;

namespace BopCustomTextures;

/// <summary>
/// Plugin class. Manages configuration, executes all harmony patches and other hooks, and otherwise uses CustomManager to realize functionality.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BopCustomTexturesPlugin : BaseUnityPlugin
{
    /// <summary>
    /// lowest version string saved mixtapes will support
    /// </summary>
    public const string LowestVersion = "0.3.0";
    /// <summary>
    /// lowest release number saved mixtapes will support
    /// </summary>
    public const uint LowestRelease = 4;
    /// <summary>
    /// plugin name within logger
    /// </summary>
    public const string LoggerName = "CustomTex";
    /// <summary>
    /// plugin github repo URL
    /// </summary>
    public const string PluginRepoUrl = "https://github.com/AnonUserGuy/BopCustomTextures";

    public static BopCustomTexturesPlugin Instance;

    public new ManualLogSourceCustom Logger;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public ConfigManager ConfigManager;
    public CustomMenuManager MenuManager;
    public CustomManager Manager;

    private void Awake()
    {
        // Plugin startup logic

        if (Instance != null)
        {
            Instance.Logger.LogWarning("Tried to initialize BopCustomTextures twice in one session!");
            return;
        }
        Instance = this;

        ConfigManager = new ConfigManager(Config);
        BepInEx.Logging.Logger.Sources.Remove(base.Logger);
        var vanillaLogger = BepInEx.Logging.Logger.CreateLogSource(LoggerName);
        Logger = new ManualLogSourceCustom(vanillaLogger, ConfigManager);

        try
        {
            Harmony.PatchAll();
        }
        catch (Exception e)
        {
            Logger.LogError($"A method failed to be patched. You should probably update your game or remove {MyPluginInfo.PLUGIN_GUID}.");
            Logger.LogError(e);
        }
        
        MComponentParserRegistry.Initialize(Logger);

        Manager = new(Logger, ConfigManager, GetTempPath(),
            BopCustomTexturesEventTemplates.SceneModTemplates,
            BopCustomTexturesEventTemplates.TextureVariantTemplates);

        MenuManager = new(Logger, ConfigManager, Manager,
            BopCustomTexturesEventTemplates.EditorPropertiesTemplate,
            BopCustomTexturesEventTemplates.MixtapePropertiesTemplate,
            MixtapeEventTemplates.entities);

        // Apply hooks to make sure temp files are deleted on program exit
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // If previous program exit didn't properly clean up temp files, clean them up now
        CustomFileManager.CleanUpTempDirectories(GetTempParentPath());

        if (ConfigManager.LogSceneIndices.Value != LogLevel.None)
        {
            // Apply hook to log scene loading if enabled in config
            SceneManager.sceneLoaded += delegate (Scene scene, LoadSceneMode mode)
            {
                Logger.Log(ConfigManager.LogSceneIndices.Value, $"{scene.buildIndex} - {scene.name}");
            };
        }

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public void Update()
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.E))
        {
            Logger.LogWarning(Manager.LastPath);
            Logger.LogWarning(Manager.LastModified);
        }
    }

    /// <summary>
    /// Logging for static BopCustomTextures classes.
    /// </summary>
    /// <param name="level">Message log level.</param>
    /// <param name="data">Message to be logged.</param>
    /// <returns><see langword="true"/> if logger exists, <see langword="false"/> otherwise.</returns>
    public static bool Log(LogLevel level, object data)
    {
        if (Instance?.Logger != null)
        {
            Instance.Logger.Log(level, data);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Warning logging for static BopCustomTextures classes.
    /// </summary>
    /// <param name="data">Message to be logged.</param>
    /// <returns><see langword="true"/> if logger exists, <see langword="false"/> otherwise.</returns>
    public static bool LogWarning(object data) => Log(LogLevel.Warning, data);

    private void OnProcessExit(object sender, EventArgs e)
    {
        if (Manager != null)
        {
            Manager.DeleteTempDirectory();
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (Manager != null)
        {
            Manager.DeleteTempDirectory();
        }
    }

    private void OnApplicationQuit()
    {
        if (Manager != null)
        {
            Manager.DeleteTempDirectory();
        }
    }

    public static string GetTempParentPath() => Path.Combine(Path.GetTempPath(), "BepInEx", MyPluginInfo.PLUGIN_GUID);

    public static string GetTempPath() => Path.Combine(GetTempParentPath(), $"{Process.GetCurrentProcess().Id}");

    public static bool IsProbablyCustom()
    {
        SceneKey activeSceneKey = TempoSceneManager.GetActiveSceneKey();
        return activeSceneKey == SceneKey.MixtapeEditor || activeSceneKey == SceneKey.MixtapeCustom;
    }
}
