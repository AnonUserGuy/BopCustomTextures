using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="RiqLoader"/> exposing private fields and methods.
/// </summary>
internal static class RiqLoaderExtensions
{
    private static readonly MethodInfo startMixtapeMethod = AccessTools.Method(typeof(RiqLoader), "StartMixtape", []);
    public static void StartMixtape(this RiqLoader obj) => startMixtapeMethod.Invoke(obj, []);
}
