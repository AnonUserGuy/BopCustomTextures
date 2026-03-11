using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="MonoBehaviour"/> definition.
/// </summary>
public abstract class MBehaviour<T> : MComponent<T> where T: Behaviour
{
    public bool? enabled;

    public static MBehaviour<T> JsonParse<T>(CustomJsonInitializer ctx, JObject jcomponent, MBehaviour<T> mcomponent) where T : Behaviour
    {
        if (ctx.TryGetJValue(jcomponent, "Enabled", JTokenType.Boolean, out var jbool)) mcomponent.enabled = (bool)jbool;
        return mcomponent;
    }
    public override T Apply(T component)
    {
        if (enabled != null) component.enabled = (bool)enabled;
        return component;
    }
}