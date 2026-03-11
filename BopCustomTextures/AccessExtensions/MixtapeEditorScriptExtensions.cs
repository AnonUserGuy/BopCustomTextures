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
}
