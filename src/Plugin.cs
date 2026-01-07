using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace BopCustomTextures;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

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

        logFileLoading = Config.Bind("Logging",
            "LogFileLoading",
            LogLevel.Debug,
            "Log level for verbose file loading of custom assets in .bop/.riq archives");

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


        Harmony.PatchAll();

        if (logSceneIndices.Value != LogLevel.None)
        {
            SceneManager.sceneLoaded += delegate (Scene scene, LoadSceneMode mode)
            {
                Logger.Log(logSceneIndices.Value, $"{scene.buildIndex} - {scene.name}");
            };
        }
    }

    public static void LogFileLoading(object data)
    {
        Logger.Log(logFileLoading.Value, data);
    }
    public static void LogUnloading(object data)
    {
        Logger.Log(logUnloading.Value, data);
    }
    public static void LogSeperateTextureSprites(object data)
    {
        Logger.Log(logSeperateTextureSprites.Value, data);
    }
    public static void LogAtlasTextureSprites(object data)
    {
        Logger.Log(logAtlasTextureSprites.Value, data);
    }


    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "ReadDirectory")]
    private static class BopMixtapeSerializerReadDirectoryPatch
    {
        static void Postfix(string path)
        {
            CustomManagement.ReadDirectory(path);
        }
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "WriteDirectory")]
    private static class BopMixtapeSerializerWriteDirectoryPatch
    {
        static void Postfix(string path)
        {
            CustomManagement.WriteDirectory(path);
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "ResetAll")]
    private static class MixtapeEditorScriptResetAllPatch
    {
        static void Postfix()
        {
            CustomManagement.ResetAll();
        }
    }

    [HarmonyPatch(typeof(MixtapeLoaderCustom), "InitScene")]
    private static class MixtapeLoaderCustomGetOrLoadScenePatch
    {
        static void Postfix(MixtapeLoaderCustom __instance, SceneKey sceneKey)
        {
            CustomManagement.InitScene(__instance, sceneKey);
        }
    }
}
