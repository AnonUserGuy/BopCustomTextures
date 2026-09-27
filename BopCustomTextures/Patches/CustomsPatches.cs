using BopCustomTextures.AccessExtensions;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using static BopCustomTextures.BopCustomTexturesPlugin;

namespace BopCustomTextures.Patches;

[HarmonyPatch(typeof(MixtapeEditorScript), "ResetAllAndReformat")]
public static class MixtapeEditorScriptResetAllAndReformatPatch
{
    static void Postfix(MixtapeEditorScript __instance)
    {
        try
        {
            if (MixtapeEditorScriptExtensions.ResetAllAndReformatMethod.Exists() 
                && Instance.MenuManager.UpdateMixtapeCategoryButton(__instance))
            {
                // BopVisualEffects compatibility thing
                __instance.ResetAllAndReformat();
                Instance.MenuManager.UpdateMixtapeCategoryButton(__instance);
                return;
            }
            Instance.Manager.ResetAll();
            Instance.MenuManager.UpdateSingletonEvents(__instance);
        }
        catch (Exception e)
        {
            Instance.Logger.LogError($"An unexpected exception occured on ResetAllAndReformat, likely from starting the mixtape editor. You should probably update your game or remove {MyPluginInfo.PLUGIN_GUID}.");
            Instance.Logger.LogError(e);
        }
    }
}

[HarmonyPatch(typeof(MixtapeLoaderCustom), "Awake")]
public static class MixtapeLoaderCustomAwakePatch
{
    static void Prefix()
    {
        if (!Instance.ConfigManager.LoadCustomAssets.Value || !IsProbablyCustom())
        {
            Instance.Manager.ResetAll();
        }
    }
}

[HarmonyPatch(typeof(RiqLoader), "Load")]
public static class RiqLoaderLoadPatch
{
    static void Prefix(string path)
    {
        if (Instance.ConfigManager.LoadCustomAssets.Value)
        {
            Instance.Manager.ResetIfNecessary(path, false);
        }
    }
}

[HarmonyPatch(typeof(MixtapeEditorScript), "Open", [typeof(string)])]
public static class MixtapeEditorScriptOpenPatch
{
    static void Prefix(string path)
    {
        if (Instance.ConfigManager.LoadCustomAssets.Value)
        {
            Instance.Manager.ResetIfNecessary(path);
        }
    }
}

[HarmonyPatch(typeof(BopMixtapeSerializerV0), "ReadDirectory")]
public static class BopMixtapeSerializerReadDirectoryPatch
{
    static void Postfix(string path)
    {
        if (Instance.ConfigManager.LoadCustomAssets.Value)
        {
            Instance.Manager.CheckVersionThenReadDirectory(path);
        }
    }
}

[HarmonyPatch(typeof(BopMixtapeSerializerV0), "WriteDirectory")]
public static class BopMixtapeSerializerWriteDirectoryPatch
{
    static void Postfix(string path)
    {
        Instance.Manager.WriteDirectory(path);
    }
}

[HarmonyPatch]
public static class MixtapeCustomLoadRiqArchivePatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(RiqLoader), "LoadRiqArchive");
        yield return AccessTools.Method(typeof(MixtapeEditorScript), "LoadRiqArchive");
    }
    static void Postfix(string path)
    {
        Instance.Manager.CheckVersionThenReadArchive(path);
    }
}

[HarmonyPatch(typeof(MixtapeEditorScript), "SaveAsRiq")]
public static class MixtapeEditorScriptSaveAsRiqPatch
{
    static void Postfix(string path)
    {
        Instance.Manager.WriteArchive(path);
    }
}

[HarmonyPatch(typeof(MixtapeLoaderCustom), "InitScene")]
public static class MixtapeLoaderCustomGetOrLoadScenePatch
{
    static void Postfix(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        Instance.Manager.InitScene(__instance, sceneKey);
    }
}

[HarmonyPatch(typeof(MixtapeLoaderCustom), "Start")]
public static class MixtapeLoaderCustomStartPatch
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
                Instance.Manager.Prepare(__state);
                hasInited = true;
            }
            yield return __result.Current;
        }
    }
}

[HarmonyPatch(typeof(SteamUploadManager), "UploadCoroutine", MethodType.Enumerator)]
public static class SteamUploadManagerUploadCoroutinePatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        if (!Instance.ConfigManager.UploadAppendDescription.Value)
        {
            return instructions;
        }

        var codeMatcher = new CodeMatcher(instructions, il);
        codeMatcher.MatchForward(false, [
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(BopMixtapeV0), "description"))
        ]);
        if (!codeMatcher.IsValid)
        {
            Instance.Logger.LogError("Could not find upload description instruction, so mixtape will not be uploaded with an appended description.");
            return instructions;
        }

        codeMatcher.Set(OpCodes.Call, AccessTools.Method(typeof(SteamUploadManagerUploadCoroutinePatch), "Internal"));

        return codeMatcher.InstructionEnumeration();
    }

    private static string Internal(BopMixtapeV0 mixtape)
    {
        if (Instance.ConfigManager.UploadAppendDescription.Value)
        {
            return Instance.Manager.GetDescriptionAppended(mixtape.description);
        }
        else
        {
            return mixtape.description;
        }
    }
}
