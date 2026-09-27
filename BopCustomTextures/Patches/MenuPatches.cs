using BopCustomTextures.Scripts;
using BopCustomTextures.AccessExtensions;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using static BopCustomTextures.BopCustomTexturesPlugin;

namespace BopCustomTextures.Patches;


[HarmonyPatch(typeof(RiqLoader), "StartMixtape")]
public static class RiqLoaderStartMixtapePatch
{
    static bool Prefix(RiqLoader __instance)
    {
        if (Instance.Manager.InterruptLoad)
        {
            VersionDisclaimerScript.Create(Instance.Manager, __instance);
            return false; // skip original
        }
        return true;
    }
}

[HarmonyPatch(typeof(MixtapeEditorScript), "GameNameToDisplay")]
public static class MixtapeEditorScriptGameNameToDisplayPatch
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
public static class MixtapeEditorScriptUpdateInternalPatch
{
    // this method can't be patched with a transpiler
    // https://github.com/AnonUserGuy/BopCustomTextures/issues/9

    static void Postfix(MixtapeEditorScript __instance)
    {
        Instance.MenuManager.HandleOldMenu(__instance);
        Instance.MenuManager.HandleKeybind(__instance);
    }
}

[HarmonyPatch]
public static class MixtapeEditorScriptFormatMenuPatch
{
    private static readonly MethodInfo TargetMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatMenu");
    static bool Prepare() => TargetMethod != null;
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return TargetMethod;
    }
    static void Postfix(MixtapeEditorScript __instance)
    {
        Instance.MenuManager.FormatOldMenu(__instance);
    }
}

[HarmonyPatch]
public static class MixtapeEditorScriptUpdateSingletonEventsPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectMinigame");
        yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectEvent");
        yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectPropertyOrValue");
    }

    static void Prefix(MixtapeEditorScript __instance)
    {
        Instance.MenuManager.UpdateSingletonEvents(__instance);
    }
}

[HarmonyPatch(typeof(MixtapeEditorScript), "OnSelectCategory")]
public static class MixtapeEditorScriptOnSelectMinigamePatch
{
    static void Prefix(MixtapeEditorScript __instance, ref string category)
    {
        Instance.MenuManager.CycleModdedCategory(__instance, ref category);
    }
}

[HarmonyPatch(typeof(MixtapeEditorScript), "CycleProperty")]
public static class MixtapeEditorScriptCyclePropertyPatch
{
    static void Postfix(MixtapeEditorScript __instance, int option)
    {
        Instance.MenuManager.CycleProperty(__instance, option);
    }
}

// For versions without MixtapeEditorScript.singletonEvents
[HarmonyPatch]
public static class MixtapeEditorScriptFormatIfSingletonSelectedPatch
{
    static bool Prepare() => !MixtapeEditorScriptExtensions.SingletonEventsField.Exists();
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectMinigame");
        yield return AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectEvent");
    }
    static void Postfix(MixtapeEditorScript __instance)
    {
        Instance.MenuManager.FormatIfSingletonSelected(__instance);
    }
}

// For versions without MixtapeEditorScript.singletonEvents
[HarmonyPatch(typeof(MixtapeEditorScript), "SpawnEventFromTemplate")]
public static class MixtapeEditorScriptSpawnEventFromTemplatePatch
{
    static bool Prepare() => !MixtapeEditorScriptExtensions.SingletonEventsField.Exists();
    static bool Prefix(MixtapeEditorScript __instance, MixtapeEventTemplate templateEvent)
    {
        if (Instance.MenuManager.CheckSingletonEventSpawning(__instance, templateEvent))
        {
            return false; // skip original
        }
        return true; // don't skip original
    }
}

// For versions without MixtapeEditorScript.singletonEvents
[HarmonyPatch(typeof(MixtapeEditorScript), "SelectedEventIsSingleton")]
public static class MixtapeEditorScriptSelectedEventIsSingletonPatch
{
    static bool Prepare() => !MixtapeEditorScriptExtensions.SingletonEventsField.Exists();
    static bool Prefix(MixtapeEditorScript __instance, ref bool __result)
    {
        if (Instance.MenuManager.SelectedEventIsSingleton(__instance))
        {
            __result = true;
            return false; // skip original
        }
        return true; // don't skip original
    }
}