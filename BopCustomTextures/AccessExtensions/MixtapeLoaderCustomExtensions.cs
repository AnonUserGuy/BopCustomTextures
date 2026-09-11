using BopCustomTextures.AccessExtensions.TypedInfo;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="MixtapeLoaderCustom"/> exposing private fields and methods.
/// </summary>
public static class MixtapeLoaderCustomExtensions
{
    public static readonly TypedFieldInfo<MixtapeLoaderCustom, Entity[]> EntitiesField = new("entities");
    public static Entity[] GetEntities(this MixtapeLoaderCustom instance) =>
        EntitiesField.GetValue(instance);
    public static bool SetEntities(this MixtapeLoaderCustom instance, Entity[] value) =>
        EntitiesField.SetValue(instance, value);


    public static readonly TypedFieldInfo<MixtapeLoaderCustom, int> TotalField = new("total");
    public static int GetTotal(this MixtapeLoaderCustom instance) =>
        TotalField.GetValue(instance);
    public static bool SetTotal(this MixtapeLoaderCustom instance, int value) =>
        TotalField.SetValue(instance, value);

}
