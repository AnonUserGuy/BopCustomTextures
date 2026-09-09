using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="MixtapeEditorScript"/> exposing private fields and methods.
/// </summary>
public static class MixtapeEditorScriptExtensions
{
    private static readonly MethodInfo formatMenuMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatMenu", []);
    public static void FormatMenu(this MixtapeEditorScript obj) => formatMenuMethod.Invoke(obj, []);


/*    private static readonly AccessTools.FieldRef<MixtapeEditorScript, List<string>> gamesRef =
        AccessTools.FieldRefAccess<MixtapeEditorScript, List<string>>("games");
    public static ref List<string> Games(this MixtapeEditorScript instance) => ref gamesRef(instance);

    private static readonly AccessTools.FieldRef<MixtapeEditorScript, Dictionary<string, int>> minigameToOffsetRef =
    AccessTools.FieldRefAccess<MixtapeEditorScript, Dictionary<string, int>>("minigameToOffset");
    public static ref Dictionary<string, int> MinigameToOffset(this MixtapeEditorScript instance) => ref minigameToOffsetRef(instance);*/


    private static readonly AccessTools.FieldRef<MixtapeEditorScript, int> levelIndexRef =
        AccessTools.FieldRefAccess<MixtapeEditorScript, int>("_levelIndex");
    public static ref int LevelIndex(this MixtapeEditorScript instance) => ref levelIndexRef(instance);

    private static readonly AccessTools.FieldRef<MixtapeEditorScript, int> eventIndexRef =
        AccessTools.FieldRefAccess<MixtapeEditorScript, int>("_eventIndex");
    public static ref int EventIndex(this MixtapeEditorScript instance) => ref eventIndexRef(instance);


    private static readonly MethodInfo setSelectedEventMethod = AccessTools.Method(typeof(MixtapeEditorScript), "SetSelectedEvent", [typeof(MixtapeEventScript), typeof(bool), typeof(bool)]);
    public static void SetSelectedEvent(this MixtapeEditorScript obj, MixtapeEventScript mixtapeEvent, bool forceUpdate = false, bool forceClear = false) => setSelectedEventMethod.Invoke(obj, [mixtapeEvent, forceUpdate, forceClear]);

    private static readonly AccessTools.FieldRef<MixtapeEditorScript, List<MixtapeEventScript>> selectedEventsRef =
        AccessTools.FieldRefAccess<MixtapeEditorScript, List<MixtapeEventScript>>("selectedEvents");
    public static ref List<MixtapeEventScript> SelectedEvents(this MixtapeEditorScript instance) => ref selectedEventsRef(instance);


    private static readonly MethodInfo ZSortEventsMethod = AccessTools.Method(typeof(MixtapeEditorScript), "ZSortEvents", []);
    public static void ZSortEvents(this MixtapeEditorScript obj) => ZSortEventsMethod.Invoke(obj, []);

    private static readonly MethodInfo FormatLevelsMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatLevels", []);
    public static void FormatLevels(this MixtapeEditorScript obj) => FormatLevelsMethod.Invoke(obj, []);

    private static readonly MethodInfo FormatEventsMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatEvents", []);
    public static void FormatEvents(this MixtapeEditorScript obj) => FormatEventsMethod.Invoke(obj, []);

    private static readonly MethodInfo FormatPropertiesMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatProperties", []);
    public static void FormatProperties(this MixtapeEditorScript obj) => FormatPropertiesMethod.Invoke(obj, []);

    private static readonly MethodInfo FormatValuesMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatValues", []);
    public static void FormatValues(this MixtapeEditorScript obj) => FormatValuesMethod.Invoke(obj, []);


#if BNB_OLD_EDITOR
    // These only exist pre editor UI update, last: -app 1929290 -depot 1929291 -manifest 2700963706022908388 -beta beta
    public static readonly FieldInfo menuField = AccessTools.Field(typeof(MixtapeEditorScript), "menu");
    public static readonly FieldInfo menuTextField = AccessTools.Field(typeof(MixtapeEditorScript), "menuText");

    public static readonly MethodInfo OnSelectCategoryMethod = null;
#else
    public static readonly FieldInfo menuField = null;
    public static readonly FieldInfo menuTextField = null;

    // These only exist post editor UI update, first: -app 1929290 -depot 1929291 -manifest 2259048567631053773 -beta beta
    public static readonly MethodInfo OnSelectCategoryMethod = AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectCategory", [typeof(string)]);
#endif

/*    public static int CatagoryToMinigame(this MixtapeEditorScript __instance, string category)
    {
        var games = __instance.Games();
        var minigameToOffset = __instance.MinigameToOffset();
        var levelIndex = __instance.LevelIndex();

        int gamesIndex = games.IndexOf(category);
        int minigameOffset = minigameToOffset.TryGetValue(category, out var res) ? res : 0;
        int index = gamesIndex + minigameOffset;
        if (games[levelIndex].StartsWith(games[gamesIndex]))
        {
            minigameOffset = (index + 1 < games.Count && games[index + 1].StartsWith(games[gamesIndex])) ? (minigameOffset + 1) : 0;
            index = gamesIndex + minigameOffset;
            minigameToOffset[category] = minigameOffset;
        }
        return index;
    }*/
}
