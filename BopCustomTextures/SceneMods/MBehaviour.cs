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

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (ctx.TryGetJValue(key, val, "enabled", JTokenType.Boolean, out var jbool)) enabled = (bool)jbool;
        else base.JsonParsePair(ctx, key, val);
    }

    public override T Apply(T component)
    {
        if (enabled != null) component.enabled = (bool)enabled;
        return component;
    }
}