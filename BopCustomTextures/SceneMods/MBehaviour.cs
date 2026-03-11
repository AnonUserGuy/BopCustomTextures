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

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        if (ctx.TryGetJValue(jcomponent, "Enabled", JTokenType.Boolean, out var jbool)) enabled = (bool)jbool;
        return true;
    }

    public override T Apply(T component)
    {
        if (enabled != null) component.enabled = (bool)enabled;
        return component;
    }
}