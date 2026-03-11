using BopCustomTextures.Json;
using BopCustomTextures.Config;
using BopCustomTextures.Customs;
using BopCustomTextures.Logging;
using BopCustomTextures.Scripts;
using BopCustomTextures.EventTemplates;
using BopCustomTextures.AccessExtensions;
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
using System.Reflection.Emit;
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
    public static readonly string LowestVersion = "0.2.1";
    /// <summary>
    /// lowest release number saved mixtapes will support
    /// </summary>
    public static readonly uint LowestRelease = 3;
    /// <summary>
    /// plugin name within logger
    /// </summary>
    public static readonly string LoggerName = "CustomTex";
    /// <summary>
    /// plugin github repo URL
    /// </summary>
    public static readonly string PluginRepoUrl = "https://github.com/AnonUserGuy/BopCustomTextures";

    public static new ManualLogSource Logger;
    public static CustomManager Manager;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private static bool couldFindMenu = false;

    private static ConfigEntry<bool> loadCustomAssets;

    private static ConfigEntry<bool> saveCustomFiles;
    private static ConfigEntry<bool> upgradeOldMixtapes;
    private static ConfigEntry<bool> uploadAppendDescription;
    private static ConfigEntry<bool> loadOutdatedPluginEditor;

    private static ConfigEntry<Display> displayCopyOptions;
    private static ConfigEntry<Display> displayReloadOptions;
    private static ConfigEntry<Display> displayEventTemplates;
    private static ConfigEntry<int> eventTemplatesIndex;

    private static ConfigEntry<LogLevel> logOutdatedPlugin;
    private static ConfigEntry<LogLevel> logUpgradeMixtape;

    private static ConfigEntry<LogLevel> logFileLoading;
    private static ConfigEntry<LogLevel> logUnloading;
    private static ConfigEntry<LogLevel> logSeperateTextureSprites;
    private static ConfigEntry<LogLevel> logAtlasTextureSprites;
    private static ConfigEntry<LogLevel> logMComponentRegistering;

    private static ConfigEntry<LogLevel> logSceneIndices;

    private static ConfigEntry<OutdatedPluginHandling> loadOutdatedPluginPlayer;

    private void Awake()
    {
        // Plugin startup logic
        BepInEx.Logging.Logger.Sources.Remove(base.Logger);
        Logger = BepInEx.Logging.Logger.CreateLogSource(LoggerName);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        LoadConfigs();

        var customlogger = new ManualLogSourceCustom(Logger,
            logFileLoading,
            logUnloading,
            logSeperateTextureSprites,
            logAtlasTextureSprites,
            logOutdatedPlugin,
            logMComponentRegistering,
            logUpgradeMixtape
        );

        Harmony.PatchAll();
        MComponentParserRegistry.Initialize(customlogger);

        Manager = new CustomManager(customlogger, GetTempPath(), 
            BopCustomTexturesEventTemplates.sceneModTemplate,
            BopCustomTexturesEventTemplates.textureVariantTemplates,
            MixtapeEventTemplates.entities);
        if (displayEventTemplates.Value == Display.Always)
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

    private void LoadConfigs()
    {
        loadCustomAssets = Config.Bind("General",
            "LoadCustomAssets",
            true,
            "When opening a modded mixtape, load the custom assets stored in it.\n" + 
            "(Note: modded mixtapes won't maintain their custom files if saved while this is disabled.)");


        loadOutdatedPluginPlayer = Config.Bind("Player",
            "LoadOutdatedPluginPlayer",
            OutdatedPluginHandling.ShowDisclaimer,
            "How to handle opening a modded mixtape in the Mixtape Player that was made for a newer version of BopCustomTextures.");


        saveCustomFiles = Config.Bind("Editor",
            "SaveCustomFiles",
            true,
            "When opening a modded mixtape in the editor, maintain its custom asset files whenever the mixtape is saved.");

        upgradeOldMixtapes = Config.Bind("Editor",
            "UpgradeOldMixtapes",
            true,
            "When opening a modded mixtape for an older version of the plugin in the editor, " +
            "upgrade the mixtape version to the current one when saving.");

        uploadAppendDescription = Config.Bind("Editor",
            "UploadAppendDescription",
            true,
            "When uploading a modded mixtape to the Steam Workshop, add a blurb to the end of the description with a link to download BopCustomTextures.");

        loadOutdatedPluginEditor = Config.Bind("Editor",
            "LoadOutdatedPluginEditor",
            true,
            "When opening a modded mixtape in the editor made for a newer version of BopCustomTextures, attempt to load custom assets.");


        displayCopyOptions = Config.Bind("Editor.Display",
            "DisplayOptionsCopy",
            Display.Always,
            $"When to display \"{CustomManager.menuCopyOptions[0]}\" and \"{CustomManager.menuCopyOptions[1]}\" in editor.");

        displayReloadOptions = Config.Bind("Editor.Display",
            "DisplayOptionsReload",
            Display.WhenActive,
            $"When to display \"{CustomManager.menuReloadOptions[0]}\" in editor.");

        displayEventTemplates = Config.Bind("Editor.Display",
            "DisplayEventTemplates",
            Display.Always,
            "When to display mixtape events category \"Bop Custom Textures\".\n" +
            "(Note: options besides \"Always\" can be buggy when attempting to work with a modded mixtape.)");

        eventTemplatesIndex = Config.Bind("Editor.Display",
            "EventTemplatesIndex",
            4,
            "Position in mixtape event categories list to display \"Bop Custom Textures\" at. " +
            "Values lower than 1 will put category at end of list.\n" +
            "(Note: position 0 unsupported as editor is hardcoded to only support category \"Global\" there.)");


        logOutdatedPlugin = Config.Bind("Logging",
            "logOutdatedPlugin",
            LogLevel.Error | LogLevel.MixtapeEditor,
            "Log level for message indicating BopCustomTextures needs to be updated to play a mixtape.");

        logUpgradeMixtape = Config.Bind("Logging",
            "LogUpgradeMixtape",
            LogLevel.Warning | LogLevel.MixtapeEditor,
            "Log level for messaage reminding user to save a mixtape to add/upgrade its BopCustomTextures.json file.");


        logFileLoading = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogFileLoading",
            LogLevel.Debug,
            "Log level for verbose file loading of custom files in .bop archives.");

        logUnloading = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogUnloading",
            LogLevel.Debug,
            "Log level for verbose custom asset unloading");

        logSeperateTextureSprites = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogSeperateTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from seperate textures.");

        logAtlasTextureSprites = UpgradeOrBind("Logging", "Logging.Debugging",
            "LogAtlasTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from atlas textures.");

        logMComponentRegistering = Config.Bind("Logging.Debugging",
            "LogMComponentRegistering",
            LogLevel.Debug,
            "Log level for registering of MComponents.");


        logSceneIndices = UpgradeOrBind("Logging", "Logging.Modding",
            "LogSceneIndices",
            LogLevel.None,
            "Log level for vanilla scene loading, including scene name + build index. (for locating level and sharedassets files)");
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "ReadDirectory")]
    private static class BopMixtapeSerializerReadDirectoryPatch
    {
        static void Postfix(string path)
        {
            if (loadCustomAssets.Value)
            {
                Manager.CheckVersionThenReadDirectory(path,
                    saveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory(),
                    upgradeOldMixtapes.Value,
                    GetOutdatedPluginHandling(),
                    displayEventTemplates.Value,
                    eventTemplatesIndex.Value);
            }
        }
    }

    [HarmonyPatch(typeof(RiqLoader), "StartMixtape")] 
    private static class RiqLoaderStartMixtapePatch
    {
        static bool Prefix(RiqLoader __instance)
        {
            if (Manager.interruptLoad)
            {
                VersionDisclaimerScript.Create(Manager, __instance);
                return false; // skip original
            }
            return true;
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
            if (!loadCustomAssets.Value || !IsProbablyCustom())
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
            if (loadCustomAssets.Value)
            {
                Manager.ResetIfNecessary(path, displayEventTemplates.Value, eventTemplatesIndex.Value);
            }
        }
    }
    
    [HarmonyPatch]
    private static class MixtapeCustomLoadRiqArchivePatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(RiqLoader), "LoadRiqArchive");
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "LoadRiqArchive");
        }
        static void Postfix(string path)
        {
            Manager.CheckVersionThenReadRiqArchive(path,
                saveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory(),
                upgradeOldMixtapes.Value,
                GetOutdatedPluginHandling(),
                displayEventTemplates.Value,
                eventTemplatesIndex.Value);
        }
    }
    
    [HarmonyPatch(typeof(MixtapeEditorScript), "SaveAsRiq")]
    private static class MixtapeEditorScriptSaveAsRiqPatch
    {
        static void Postfix(string path)
        {
            Manager.SaveAsRiq(path, upgradeOldMixtapes.Value);
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
        static void Prefix(MixtapeLoaderCustom __instance, out MixtapeLoaderCustom __state)
        {
            __state = __instance;
        }
        static IEnumerator Postfix(IEnumerator __result, MixtapeLoaderCustom __state)
        {
            bool hasInited = false;
            __state.Total() = 0;

            while (__result.MoveNext())
            {
                if (__state.Total() > 0 && !hasInited)
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

    [HarmonyPatch(typeof(MixtapeEditorScript), "UpdateInternal")]
    private static class MixtapeEditorScriptUpdateInternalPatch
    {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            if (displayCopyOptions.Value == Display.Never && displayReloadOptions.Value == Display.Never)
            {
                return instructions;
            }

            var codeMatcher = new CodeMatcher(instructions, il);

            codeMatcher.MatchForward(false, [
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Br ||
                    ci.opcode == OpCodes.Br_S),
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Ldloc ||
                    ci.opcode == OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldc_I4_7),
                new CodeMatch(ci =>
                    ci.opcode == OpCodes.Bne_Un ||
                    ci.opcode == OpCodes.Bne_Un_S),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(MixtapeEditorScript), "FileQuit"))
            ]);
            if (!codeMatcher.IsValid)
            {
                Logger.LogError("Could not find mixtape editor menu handler, so mixtape editor will not include modded menu options.");
                return instructions;
            }

            var breakInstruction = codeMatcher.Instruction;
            codeMatcher.Advance(1);
            var ldlocInstruction = codeMatcher.Instruction;
            codeMatcher.Advance(2);
            int branchPos = codeMatcher.Pos;

            codeMatcher.Advance(3);
            codeMatcher.Insert([
                breakInstruction,
                new CodeInstruction(OpCodes.Ldarg_0),
                ldlocInstruction,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MixtapeEditorScriptUpdateInternalPatch), "Internal"))
            ]);
            codeMatcher.Advance(1);
            codeMatcher.CreateLabel(out var label);

            codeMatcher.Start().Advance(branchPos);
            codeMatcher.SetOperandAndAdvance(label);

            couldFindMenu = true;
            return codeMatcher.InstructionEnumeration();
        }

        private static void Internal(MixtapeEditorScript __instance, int option)
        {
            if (couldFindMenu)
            {
                Manager.HandleMenuOption(__instance,
                    option - 8,
                    displayCopyOptions.Value, 
                    displayReloadOptions.Value,
                    saveCustomFiles.Value,
                    upgradeOldMixtapes.Value,
                    GetOutdatedPluginHandling(),
                    displayEventTemplates.Value,
                    eventTemplatesIndex.Value);
            }
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "FormatMenu")]
    private static class MixtapeEditorScriptFormatOptionsPatch
    {
        static void Postfix(MixtapeEditorScript __instance)
        {
            if (couldFindMenu)
            {
                Manager.FormatMenu(__instance,
                    displayCopyOptions.Value,
                    displayReloadOptions.Value);
            }
        }
    }

    [HarmonyPatch(typeof(SteamUploadManager), "UploadCoroutine", MethodType.Enumerator)]
    private static class SteamUploadManagerUploadCoroutinePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            if (!uploadAppendDescription.Value)
            {
                return instructions;
            }

            var codeMatcher = new CodeMatcher(instructions, il);
            codeMatcher.MatchForward(false, [
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BopMixtapeV0), "description"))
            ]);
            if (!codeMatcher.IsValid)
            {
                Logger.LogError("Could not find upload description instruction, so mixtape will not be uploaded with an appended description.");
                return instructions;
            }

            codeMatcher.Set(OpCodes.Call, AccessTools.Method(typeof(SteamUploadManagerUploadCoroutinePatch), "Internal"));

            return codeMatcher.InstructionEnumeration();
        }

        private static string Internal(BopMixtapeV0 mixtape)
        {
            if (uploadAppendDescription.Value)
            {
                return Manager.GetDescriptionAppended(mixtape.description);
            } 
            else
            {
                return mixtape.description;
            }
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
    public static OutdatedPluginHandling GetOutdatedPluginHandling()
    {
        return (TempoSceneManager.GetActiveSceneKey() == SceneKey.RiqLoader) ? loadOutdatedPluginPlayer.Value :
            loadOutdatedPluginEditor.Value ? OutdatedPluginHandling.LoadModded : OutdatedPluginHandling.LoadVanilla;
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
