using BopCustomTextures.AccessExtensions.TypedInfo;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="RiqLoader"/> exposing private fields and methods.
/// </summary>
public static class RiqLoaderExtensions
{
    private static readonly TypedMethodInfo<RiqLoader> StartMixtapeMethod = new("StartMixtape", []);
    public static void StartMixtape(this RiqLoader obj) => StartMixtapeMethod.Invoke(obj);
}
