using HarmonyLib;

namespace BopCustomTextures.AccessExtensions;
public static class MixtapeLoaderCustomExtensions
{
    private static readonly AccessTools.FieldRef<MixtapeLoaderCustom, Entity[]> entitiesRef =
        AccessTools.FieldRefAccess<MixtapeLoaderCustom, Entity[]>("entities");

    public static ref Entity[] Entities(this MixtapeLoaderCustom instance) => ref entitiesRef(instance);

    private static readonly AccessTools.FieldRef<MixtapeLoaderCustom, int> totalRef =
        AccessTools.FieldRefAccess<MixtapeLoaderCustom, int>("total");

    public static ref int Total(this MixtapeLoaderCustom instance) => ref totalRef(instance);
}
