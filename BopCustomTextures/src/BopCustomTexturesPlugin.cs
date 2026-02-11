using BopCustomTextures.Customs;
using BopCustomTextures.Logging;
using BopCustomTextures.EventTemplates;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using LogLevel = BopCustomTextures.Logging.LogLevel;

namespace BopCustomTextures;

/// <summary>
/// Plugin class. Executes all harmony patches and other hooks, and otherwise uses Customs/CustomManager to realize functionality.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BopCustomTexturesPlugin : BaseUnityPlugin
{
    /// <summary>
    /// lowest version string saved mixtapes will support
    /// </summary>
    public static readonly string LowestVersion = "0.2.0";
    /// <summary>
    /// lowest release number saved mixtapes will support
    /// </summary>
    public static readonly uint LowestRelease = 2;
    /// <summary>
    /// plugin name within logger
    /// </summary>
    public static readonly string LoggerName = "CustomTex";

    public static new ManualLogSource Logger;
    public static CustomManager Manager;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private static ConfigEntry<bool> saveCustomFiles;
    private static ConfigEntry<bool> upgradeOldMixtapes;
    private static ConfigEntry<DisplayEventTemplates> displayEventTemplates;
    private static ConfigEntry<int> eventTemplatesIndex;

    private static ConfigEntry<LogLevel> logOutdatedPlugin;
    private static ConfigEntry<LogLevel> logUpgradeMixtape;

    private static ConfigEntry<LogLevel> logFileLoading;
    private static ConfigEntry<LogLevel> logUnloading;
    private static ConfigEntry<LogLevel> logSeperateTextureSprites;
    private static ConfigEntry<LogLevel> logAtlasTextureSprites;
    private static ConfigEntry<LogLevel> logSceneIndices;

    private void Awake()
    {
        // Plugin startup logic
        BepInEx.Logging.Logger.Sources.Remove(base.Logger);
        Logger = BepInEx.Logging.Logger.CreateLogSource(LoggerName);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Config loading
        saveCustomFiles = Config.Bind("Editor",
            "SaveCustomFiles",
            true,
            "When opening a modded mixtape in the editor, save these files whenever the mixtape is saved");

        upgradeOldMixtapes = Config.Bind("Editor",
            "UpgradeOldMixtapes",
            true,
            "When opening a modded mixtape for an older version of the plugin in the editor, " +
            "upgrade the mixtape version to the current one when saving.");

        displayEventTemplates = Config.Bind("Editor",
            "DisplayEventTemplates",
            DisplayEventTemplates.WhenActive,
            "When to display mixtape events catagory \"Bop Custom Textures\".");

        eventTemplatesIndex = Config.Bind("Editor",
            "EventTemplatesIndex",
            4,
            "Position in mixtape event catagories list to display \"Bop Custom Textures\" at. " +
            "Values lower than 1 will put catagory at end of list.");


        logOutdatedPlugin = Config.Bind("Logging",
            "logOutdatedPlugin",
            LogLevel.Error | LogLevel.MixtapeEditor,
            "Log level for message indicating BopCustomTextures needs to be updated to play a mixtape");

        logUpgradeMixtape = Config.Bind("Logging",
            "LogUpgradeMixtape",
            LogLevel.Warning | LogLevel.MixtapeEditor,
            "Log level for messaage reminding user to save a mixtape to add/upgrade its BopCustomTextures.json file");


        logFileLoading = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogFileLoading",
            LogLevel.Debug,
            "Log level for verbose file loading of custom files in .bop archives");

        logUnloading = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogUnloading",
            LogLevel.Debug,
            "Log level for verbose custom asset unloading");

        logSeperateTextureSprites = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogSeperateTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from seperate textures");

        logAtlasTextureSprites = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogAtlasTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from atlas textures");


        logSceneIndices = UpgradeOrBind("Logging", "Logging.Modding",
            "LogSceneIndices",
            LogLevel.None,
            "Log level for vanilla scene loading, including scene name + build index (for locating level and sharedassets files)");

        var customlogger = new ManualLogSourceCustom(Logger,
            MyPluginInfo.PLUGIN_NAME,
            logFileLoading,
            logUnloading,
            logSeperateTextureSprites,
            logAtlasTextureSprites,
            logOutdatedPlugin,
            logUpgradeMixtape
        );

        Harmony.PatchAll();

        Manager = new CustomManager(customlogger, GetTempPath(), 
            BopCustomTexturesEventTemplates.sceneModTemplate,
            BopCustomTexturesEventTemplates.textureVariantTemplates,
            MixtapeEventTemplates.entities);
        if (displayEventTemplates.Value == DisplayEventTemplates.Always)
        {
            Manager.AddEventTemplates(eventTemplatesIndex.Value);
        }

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
                customlogger.Log(logSceneIndices.Value, $"{scene.buildIndex} - {scene.name}");
            };
        }
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "ReadDirectory")]
    private static class BopMixtapeSerializerReadDirectoryPatch
    {
        static void Postfix(string path)
        {
            Manager.ReadDirectory(path,
                saveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory(),
                upgradeOldMixtapes.Value,
                displayEventTemplates.Value,
                eventTemplatesIndex.Value);
        }
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "WriteDirectory")]
    private static class BopMixtapeSerializerWriteDirectoryPatch
    {
        static void Postfix(string path)
        {
            Manager.WriteDirectory(path, upgradeOldMixtapes.Value);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "ResetAllAndReformat")]
    private static class MixtapeEditorScriptResetAllAndReformatPatch
    {
        static void Postfix()
        {
            Manager.ResetAll(displayEventTemplates.Value, eventTemplatesIndex.Value);
        }
    }
    [HarmonyPatch(typeof(MixtapeLoaderCustom), "Awake")]
    private static class MixtapeLoaderCustomAwakePatch
    {
        static void Prefix()
        {
            if (!IsProbablyCustom())
            {
                Manager.ResetAll(displayEventTemplates.Value, eventTemplatesIndex.Value);
            }
        }
    }
    [HarmonyPatch]
    private static class MixtapeCustomLoadPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RiqLoader), "Load");
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "Open", [typeof(string)]);
        }
        static void Prefix(string path)
        {
            Manager.ResetIfNecessary(path, displayEventTemplates.Value, eventTemplatesIndex.Value);
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

    [HarmonyPatch(typeof(MixtapeLoaderCustom), "Start")]
    private static class MixtapeLoaderCustomStartPatch
    {
        public static readonly AccessTools.FieldRef<MixtapeLoaderCustom, int> totalRef =
            AccessTools.FieldRefAccess<MixtapeLoaderCustom, int>("total");
        static void Prefix(MixtapeLoaderCustom __instance, out MixtapeLoaderCustom __state)
        {
            __state = __instance;
        }
        static IEnumerator Postfix(IEnumerator __result, MixtapeLoaderCustom __state)
        {
            bool hasInited = false;
            totalRef(__state) = 0;

            while (__result.MoveNext())
            {
                if (totalRef(__state) > 0 && !hasInited)
                {
                    // after BeginInternal for all games, before jukebox is ready
                    Manager.Prepare(__state);
                    hasInited = true;
                }
                yield return __result.Current;
            } 
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "GameNameToDisplay")]
    private static class MixtapeEditorScriptGameNameToDisplayPatch
    {
        static bool Prefix(string name, ref string __result)
        {
            if (name == MyPluginInfo.PLUGIN_GUID)
            {
                __result = MyPluginInfo.PLUGIN_NAME;
                return false; // skip original
            }
            return true; // don't skip original
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

    private ConfigEntry<T> UpgradeOrBind<T>(string oldSection, string newSection, string key, T defaultValue, string description)
    {
        var oldEntry = Config.Bind(
            oldSection,
            key,
            defaultValue,
            description
        );
        Config.Remove(new ConfigDefinition(oldSection, key));
        return Config.Bind(
            newSection,
            key,
            oldEntry.Value,
            description
        );
    }
}
