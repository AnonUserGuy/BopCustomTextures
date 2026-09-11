using BopCustomTextures.Json;
using BopCustomTextures.Config;
using BopCustomTextures.Customs;
using BopCustomTextures.Logging;
using BopCustomTextures.Scripts;
using BopCustomTextures.EventTemplates;
using BopCustomTextures.AccessExtensions;
using BepInEx;
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

    public static new ManualLogSourceCustom Logger;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public static CustomManager Manager;
    public static ConfigManager ConfigManager;

    private void Awake()
    {
        // Plugin startup logic
        ConfigManager = new ConfigManager(Config);
        InitLogger();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.PatchAll();
        MComponentParserRegistry.Initialize(Logger);

        Manager = new CustomManager(Logger, ConfigManager, GetTempPath(),
            BopCustomTexturesEventTemplates.SceneModTemplate,
            BopCustomTexturesEventTemplates.TextureVariantTemplates,
            BopCustomTexturesEventTemplates.EditorPropertiesTemplate,
            BopCustomTexturesEventTemplates.MixtapePropertiesTemplate,
            MixtapeEventTemplates.entities);
        Manager.UpdateEventCategoryPosition();

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
    }

    private void InitLogger()
    {
        if (Logger != null)
        {
            Logger.LogWarning("Tried to initialize logger twice in one session!");
            return;
        }
        BepInEx.Logging.Logger.Sources.Remove(base.Logger);
        var vanillaLogger = BepInEx.Logging.Logger.CreateLogSource(LoggerName);
        Logger = new ManualLogSourceCustom(vanillaLogger, ConfigManager);
    }

    /// <summary>
    /// Logging for static BopCustomTextures classes.
    /// </summary>
    /// <param name="level">Message log level.</param>
    /// <param name="data">Message to be logged.</param>
    /// <returns><see langword="true"/> if logger exists, <see langword="false"/> otherwise.</returns>
    public static bool Log(LogLevel level, object data)
    {
        if (Logger != null)
        {
            Logger.Log(level, data);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Warning logging for static BopCustomTextures classes.
    /// </summary>
    /// <param name="data">Message to be logged.</param>
    /// <returns><see langword="true"/> if logger exists, <see langword="false"/> otherwise.</returns>
    public static bool LogWarning(object data)
    {
        if (Logger != null)
        {
            Logger.LogWarning(data);
            return true;
        }
        return false;
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "ReadDirectory")]
    private static class BopMixtapeSerializerReadDirectoryPatch
    {
        static void Postfix(string path)
        {
            if (ConfigManager.LoadCustomAssets.Value)
            {
                Manager.CheckVersionThenReadDirectory(path);
            }
        }
    }

    [HarmonyPatch(typeof(RiqLoader), "StartMixtape")]
    private static class RiqLoaderStartMixtapePatch
    {
        static bool Prefix(RiqLoader __instance)
        {
            if (Manager.InterruptLoad)
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
            Manager.WriteDirectory(path);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "ResetAllAndReformat")]
    private static class MixtapeEditorScriptResetAllAndReformatPatch
    {
        static void Postfix(MixtapeEditorScript __instance)
        {
            Manager.ResetAll();
            Manager.UpdateSingletonEvents(__instance);
        }
    }
    [HarmonyPatch(typeof(MixtapeLoaderCustom), "Awake")]
    private static class MixtapeLoaderCustomAwakePatch
    {
        static void Prefix()
        {
            if (!ConfigManager.LoadCustomAssets.Value || !IsProbablyCustom())
            {
                Manager.ResetAll();
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
            if (ConfigManager.LoadCustomAssets.Value)
            {
                Manager.ResetIfNecessary(path);
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
            Manager.CheckVersionThenReadRiqArchive(path);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "SaveAsRiq")]
    private static class MixtapeEditorScriptSaveAsRiqPatch
    {
        static void Postfix(string path)
        {
            Manager.SaveAsRiq(path);
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
            __state.SetTotal(0);

            while (__result.MoveNext())
            {
                if (__state.GetTotal() > 0 && !hasInited)
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
        // this method can't be patched with a transpiler
        // https://github.com/AnonUserGuy/BopCustomTextures/issues/9

        static void Postfix(MixtapeEditorScript __instance)
        {
            Manager.HandleKeybind(__instance);
        }
    }

    [HarmonyPatch]
    private static class MixtapeEditorScriptUpdateSingletonEventsPatch
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectMinigame");
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectEvent");
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectPropertyOrValue");
        }

        static void Prefix(MixtapeEditorScript __instance)
        {
            Manager.UpdateSingletonEvents(__instance);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "OnSelectCategory")]
    private static class MixtapeEditorScriptOnSelectMinigamePatch
    {
        static void Prefix(MixtapeEditorScript __instance, ref string category)
        {
            Manager.CycleModdedCategory(__instance, ref category);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "CycleProperty")]
    private static class MixtapeEditorScriptCyclePropertyPatch
    {
        static void Postfix(MixtapeEditorScript __instance, int option)
        {
            Manager.CycleProperty(__instance, option);
        }
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    [HarmonyPatch]
    private static class MixtapeEditorScriptFormatIfSingletonSelectedPatch
    {
        static bool Prepare() => !MixtapeEditorScriptExtensions.SingletonEventsField.Exists();
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectMinigame");
            yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectEvent");
        }
        static void Postfix(MixtapeEditorScript __instance)
        {
            Manager.FormatIfSingletonSelected(__instance);
        }
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    [HarmonyPatch(typeof(MixtapeEditorScript), "SpawnEventFromTemplate")]
    private static class MixtapeEditorScriptSpawnEventFromTemplatePatch
    {
        static bool Prepare() => !MixtapeEditorScriptExtensions.SingletonEventsField.Exists();
        static bool Prefix(MixtapeEditorScript __instance, MixtapeEventTemplate templateEvent)
        {
            if (Manager.CheckSingletonEventSpawning(__instance, templateEvent))
            {
                return false; // skip original
            }
            return true; // don't skip original
        }
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    [HarmonyPatch(typeof(MixtapeEditorScript), "SelectedEventIsSingleton")]
    private static class MixtapeEditorScriptSelectedEventIsSingletonPatch
    {
        static bool Prepare() => !MixtapeEditorScriptExtensions.SingletonEventsField.Exists();
        static bool Prefix(MixtapeEditorScript __instance, ref bool __result)
        {
            if (Manager.SelectedEventIsSingleton(__instance))
            {
                __result = true;
                return false; // skip original
            }
            return true; // don't skip original
        }
    }

    [HarmonyPatch(typeof(SteamUploadManager), "UploadCoroutine", MethodType.Enumerator)]
    private static class SteamUploadManagerUploadCoroutinePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            if (!ConfigManager.UploadAppendDescription.Value)
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
            if (ConfigManager.UploadAppendDescription.Value)
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

}
