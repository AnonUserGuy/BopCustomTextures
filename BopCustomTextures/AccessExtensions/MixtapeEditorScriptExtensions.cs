using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;
public static class MixtapeEditorScriptExtensions
{
    private static readonly MethodInfo formatMenuMethod = AccessTools.Method(typeof(MixtapeEditorScript), "FormatMenu", []);
    public static void FormatMenu(this MixtapeEditorScript obj) => formatMenuMethod.Invoke(obj, []);
}
