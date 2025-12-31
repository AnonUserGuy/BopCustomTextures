using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using System.IO.Compression;

namespace BopCustomTextures;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public Harmony Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.PatchAll();
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "Read")]
    private static class BopMixtapeSerializerReadPatch
    {
        static void Postfix(string path)
        {
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
            
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                if (!CustomSceneManagement.CheckIsCustomScene(entry))
                {
                    CustomTextureManagement.CheckIsCustomTexture(entry);
                }
            }
            Logger.LogInfo("Checked all files");
        }
    }

    [HarmonyPatch(typeof(BopMixtapeSerializerV0), "Write")]
    private static class BopMixtapeSerializerWritePatch
    {
        static void Postfix(string path)
        {
            if (CustomManagement.HasFiles())
            {
                Logger.LogInfo("Saving with custom files");
                using FileStream stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Update);
                CustomManagement.WriteFiles(zipArchive);
            }
        }
    }

    [HarmonyPatch(typeof(MixtapeEditorScript), "ResetAll")]
    private static class MixtapeEditorScriptResetAllPatch
    {
        static void Postfix()
        {
            CustomSceneManagement.UnloadCustomScenes();
            CustomTextureManagement.UnloadCustomTextures();
            CustomManagement.UnloadFiles();
        }
    }

    [HarmonyPatch(typeof(MixtapeLoaderCustom), "InitScene")]
    private static class MixtapeLoaderCustomGetOrLoadScenePatch
    {
        static void Postfix(MixtapeLoaderCustom __instance, SceneKey sceneKey)
        {

            CustomSceneManagement.InitCustomScene(__instance, sceneKey);
            CustomTextureManagement.InitCustomTextures(__instance, sceneKey);
        }
    }
}
