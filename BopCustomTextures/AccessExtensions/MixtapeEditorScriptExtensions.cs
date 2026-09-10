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


    private static readonly AccessTools.FieldRef<MixtapeEditorScript, Dictionary<string, MixtapeEventScript>> singletonEventsRef =
        AccessTools.FieldRefAccess<MixtapeEditorScript, Dictionary<string, MixtapeEventScript>>("singletonEvents");
    public static ref Dictionary<string, MixtapeEventScript> SingletonEvents(this MixtapeEditorScript instance) => ref singletonEventsRef(instance);


    private static readonly AccessTools.FieldRef<MixtapeEditorScript, HashList<MixtapeEventScript>> selectedEventsRef =
        AccessTools.FieldRefAccess<MixtapeEditorScript, HashList<MixtapeEventScript>>("selectedEvents");
    public static ref HashList<MixtapeEventScript> SelectedEvents(this MixtapeEditorScript instance) => ref selectedEventsRef(instance);


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
}
