using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.Extensions;
internal static class RiqLoaderExtensions
{
    private static readonly MethodInfo startMixtapeMethod = AccessTools.Method(typeof(RiqLoader), "StartMixtape", []);
    public static void StartMixtape(this RiqLoader obj) => startMixtapeMethod.Invoke(obj, []);
}
