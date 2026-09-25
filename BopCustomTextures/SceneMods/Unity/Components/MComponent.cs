using UnityEngine;
using BopCustomTextures.Json;
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
    public abstract void Apply(GameObject obj);
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