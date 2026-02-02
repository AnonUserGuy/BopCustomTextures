using BopCustomTextures.Customs;
using BopCustomTextures.Logging;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Diagnostics;

namespace BopCustomTextures;

/// <summary>
/// Plugin class. Executes all harmony patches and other hooks, and otherwise uses Customs/CustomManager to realize functionality.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BopCustomTexturesPlugin : BaseUnityPlugin
{

    public static new ManualLogSource Logger;
    public static CustomManager Manager;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private static ConfigEntry<bool> saveCustomFiles;

    private static ConfigEntry<LogLevel> logFileLoading;
    private static ConfigEntry<LogLevel> logUnloading;
    private static ConfigEntry<LogLevel> logSeperateTextureSprites;
    private static ConfigEntry<LogLevel> logAtlasTextureSprites;
    private static ConfigEntry<LogLevel> logSceneIndices;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Config loading
        saveCustomFiles = Config.Bind("Editor",
            "SaveCustomFiles",
            true,
            "When opening a mixtape in the editor with custom files, save these files whenever the mixtape is saved");

        logFileLoading = Config.Bind("Logging",
            "LogFileLoading",
            LogLevel.Debug,
            "Log level for verbose file loading of custom files in .bop archives");

        logUnloading = Config.Bind("Logging",
            "LogUnloading",
            LogLevel.Debug,
            "Log level for verbose custom asset unloading");

        logSeperateTextureSprites = Config.Bind("Logging",
            "LogSeperateTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from seperate textures");

        logAtlasTextureSprites = Config.Bind("Logging",
            "LogAtlasTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from atlas textures");

        logSceneIndices = Config.Bind("Logging",
            "LogSceneIndices",
            LogLevel.None,
            "Log level for vanilla scene loading, including scene name + build index (for locating level and sharedassets files)");

        var customlogger = new ManualLogSourceCustom(Logger,
            logFileLoading.Value,
            logUnloading.Value,
            logSeperateTextureSprites.Value,
            logAtlasTextureSprites.Value
        );
        Manager = new CustomManager(customlogger, GetTempPath());

        Harmony.PatchAll();

        // Apply hooks to make sure temp files are deleted on program exit
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // If previous program exit didn't properly clean up temp files, clean them up now
        CustomFileManager.CleanUpTempDirectories(GetTempParentPath());

        if (logSceneIndices.Value != LogLevel.None)
        {
            // Apply hook to log scene loading if enabled in config
            SceneManager.sceneLoaded += delegate (Scene scene, LoadSceneMode mode)
            {
                Logger.Log(logSceneIndices.Value, $"{scene.buildIndex} - {scene.name}");
            };
        }
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "ReadDirectory")]
    private static class BopMixtapeSerializerReadDirectoryPatch
    {
        static void Postfix(string path)
        {
            Manager.ReadDirectory(path, saveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory());
        }
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "WriteDirectory")]
    private static class BopMixtapeSerializerWriteDirectoryPatch
    {
        static void Postfix(string path)
        {
            Manager.WriteDirectory(path);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "ResetAllAndReformat")]
    private static class MixtapeEditorScriptResetAllAndReformatPatch
    {
        static void Postfix()
        {
            Manager.ResetAll();
        }
    }
    [HarmonyPatch(typeof(MixtapeLoaderCustom), "Awake")]
    private static class MixtapeLoaderCustomAwakePatch
    {
        static void Prefix()
        {
            if (!IsProbablyCustom())
            {
                Manager.ResetAll();
            }
        }
    }
    [HarmonyPatch(typeof(RiqLoader), "Load")]
    private static class RiqLoaderLoadPatch
    {
        static void Prefix(string path)
        {
            Manager.ResetIfNecessary(path);
        }
    }
    [HarmonyPatch(typeof(MixtapeEditorScript), "Open", [typeof(string)] )]
    private static class MixtapeEditorScriptOpenPatch
    {
        static void Prefix(string path)
        {
            Manager.ResetIfNecessary(path);
        }
    }

    [HarmonyPatch(typeof(MixtapeLoaderCustom), "InitScene")]
    private static class MixtapeLoaderCustomGetOrLoadScenePatch
    {
        static void Postfix(MixtapeLoaderCustom __instance, SceneKey sceneKey)
        {
            Manager.InitScene(__instance, sceneKey);
        }
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        Manager.DeleteTempDirectory();
    }
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Manager.DeleteTempDirectory();
    }
    private void OnApplicationQuit()
    {
        Manager.DeleteTempDirectory();
    }

    public static string GetTempParentPath()
    {
        return Path.Combine(Path.GetTempPath(), "BepInEx", MyPluginInfo.PLUGIN_GUID);
    }
    public static string GetTempPath()
    {
        return Path.Combine(Path.GetTempPath(), "BepInEx", MyPluginInfo.PLUGIN_GUID, $"{Process.GetCurrentProcess().Id}");
    }
    public static bool IsProbablyCustom()
    {
        SceneKey activeSceneKey = TempoSceneManager.GetActiveSceneKey();
        return activeSceneKey == SceneKey.MixtapeEditor || activeSceneKey == SceneKey.MixtapeCustom;
    }
}
