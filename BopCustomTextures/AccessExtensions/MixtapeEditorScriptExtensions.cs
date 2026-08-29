using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="MixtapeEditorScript"/> exposing private fields and methods.
/// </summary>
public static class MixtapeEditorScriptExtensions
{
    private static readonly MethodInfo formatMenuMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatMenu", []);
    public static void FormatMenu(this MixtapeEditorScript obj) => formatMenuMethod.Invoke(obj, []);

    // These only exist pre editor UI update, last: -app 1929290 -depot 1929291 -manifest 2700963706022908388 -beta beta
    public static readonly FieldInfo menuField = AccessTools.Field(typeof(MixtapeEditorScript), "menu");

    public static readonly FieldInfo menuTextField = AccessTools.Field(typeof(MixtapeEditorScript), "menuText");

    // These only exist post editor UI update, first: -app 1929290 -depot 1929291 -manifest 2259048567631053773 -beta beta
    public static readonly MethodInfo OnSelectCategoryMethod = AccessTools.Method(typeof(MixtapeEditorScript), "OnSelectCategory", [typeof(string)]);
}
