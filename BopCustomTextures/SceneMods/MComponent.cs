using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene Mod generic <see cref="Component"/> interface. Can be parsed from JSON and applied to a <see cref="GameObject"/>.
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
    public abstract void Apply(GameObject obj);
}

/// <summary>
/// Scene Mod generic <see cref="Component"/> definition. Can be applied to a component of type T.
/// </summary>
public abstract class MComponent<T>: MObject<T>, IMComponent where T: Component
{

    /// <summary>
    /// Parse a scene mod component definition from a given <see cref="JToken"/> if it corresponds to a JSON object.
    /// </summary>
    /// <param name="ctx">The invoking <see cref="CustomJsonInitializer"/>, for logging and general parsing methods.</param>
    /// <param name="jcomponent"><see cref="JToken"/> for JSON object with component definition.</param>
    /// <returns><see langword="true"/> is JSON component parsed successfully, <see langword="false"/> otherwise.</returns>
    public virtual bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent)
    {
        if (jcomponent.Type != JTokenType.Object)
        {
            ctx.logger.LogWarning($"JSON Component is a {jcomponent.Type} when it should be a Object");
            return false;
        }
        JsonParse(ctx, (JObject)jcomponent);
        return true;
    }

    /// <summary>
    /// Parse a scene mod component definition from a given <see cref="JObject"/>.
    /// </summary>
    /// <param name="ctx">The invoking <see cref="CustomJsonInitializer"/>, for logging and general parsing methods.</param>
    /// <param name="jcomponent"><see cref="JObject"/> containing component defintion.</param>
    /// <returns><see langword="true"/> is JSON component parsed successfully, <see langword="false"/> otherwise.</returns>
    public abstract bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent);

    /// <summary>
    /// Apply scene mod to <see cref="Component"/> on <see cref="GameObject"/>, if it has it.
    /// </summary>
    /// <param name="obj"><see cref="GameObject"/> to find <see cref="Component"/> in.</param>
    public virtual void Apply(GameObject obj)
    {
        T component = obj.GetComponent<T>();
        if (component)
        {
            Apply(component);
        }
    }
}