using BopCustomTextures.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// <para>Scene Mod <see cref="Component"/> definition. Can be parsed from JSON and applied to a <see cref="GameObject"/>.</para>
/// <para>Doesn't necessarily apply to a <see cref="Component"/>. (see <see cref="MActive"/> or <see cref="Scripts.MDebugSelectedScript"/>.)</para>
/// </summary>
public interface IMComponent
{
    /// <summary>
    /// Parse a scene mod component definition from a given <see cref="JToken"/>.
    /// </summary>
    /// <param name="ctx">The invoking <see cref="CustomJsonInitializer"/>, for logging and general parsing methods.</param>
    /// <param name="jcomponent"><see cref="JObject"/> containing component defintion.</param>
    /// <returns><see langword="true"/> is JSON component parsed successfully, <see langword="false"/> otherwise.</returns>
    public abstract bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent);

    /// <summary>
    /// Apply scene mod component to <see cref="GameObject"/>.
    /// </summary>
    /// <param name="obj"><see cref="GameObject"/> to apply to.</param>
    public abstract void Apply(GameObject gameObj);
}

/// <summary>
/// <para>Scene mod <see cref="Component"/> definition that, for scene mods applied using the "Apply Scene Mod" event, 
/// needs to be applied immediately rather than scheduled and needs to apply them to the <see cref="MixtapeLoaderCustom"/>.</para>
/// <para>See <see cref="Scripts.MTempoSound"/> for an example.</para>
/// </summary>
public interface IMLoaderComponent
{
    /// <summary>
    /// Apply a scene mod to the invoking <see cref="MixtapeLoaderCustom"/>.
    /// </summary>
    /// <param name="ctx">"Context" object shared between all instances of the same type implementing <see cref="IMLoaderComponent"/>.
    /// <see langword="null"/> for first instance.</param>
    /// <param name="loader">Invoking <see cref="MixtapeLoaderCustom"/>.</param>
    /// <param name="entity">"Apply Scene Mod" event <see cref="Entity"/>.</param>
    /// <param name="beat">Beat of "Apply Scene Mod" event with offset already resolved.</param>
    /// <param name="gameObj"><para>Target <see cref="GameObject"/>.</para> 
    /// <para><see cref="Component"/> will have to be resolved by method itself for <see cref="MComponent{T}"/>'s.</para></param>
    /// <returns>"Context" object to give to next instance of the same type implementing <see cref="IMLoaderComponent"/>.</returns>
    public abstract object ApplyLoader(MixtapeLoaderCustom loader, object ctx, Entity entity, float beat, GameObject gameObj);
}

/// <summary>
/// <para>A <see cref="IMLoaderComponent"/> that needs to perform an additional "finalizing" step after all 
/// "Apply Scene Mod" events are resolved.</para>
/// <para>See <see cref="Scripts.MTempoSound"/> for an example.</para>
/// </summary>
public interface IMLoaderComponentFinal : IMLoaderComponent
{
    /// <summary>
    /// Finalize scene mods applied to the invoking <see cref="MixtapeLoaderCustom"/>. Invoked by last used 
    /// <see cref="IMLoaderComponentFinal"/> instance.
    /// </summary>
    /// <param name="ctx">"Context" object shared between all instances of the same type implementing <see cref="IMLoaderComponent"/>.</param>
    public abstract void ApplyLoaderFinalize(MixtapeLoaderCustom loader, object ctx);
}

/// <summary>
/// Tuple of <see cref="IMLoaderComponent"/> "Context" object and last used <see cref="IMLoaderComponent"/> instance.
/// </summary>
/// <param name="component">Last used <see cref="IMLoaderComponent"/> instance.</param>
/// <param name="context"><see cref="IMLoaderComponent"/> "Context" object</param>
public readonly struct MLoaderComponentContext(IMLoaderComponent component, object context)
{
    public readonly IMLoaderComponent Component = component;
    public readonly object Context = context;
}

/// <summary>
/// Scene Mod <see cref="Component"/> definition.
/// </summary>
/// <typeparam name="T">Target <see cref="Component"/> type.</typeparam>
public class MComponent<T> : MUnityObject<T>, IMComponent where T : Component
{
    public void Apply(GameObject obj)
    {
        var component = obj.GetComponent<T>();
        if (component != null)
        {
            Apply(component);
        }
    }
}