using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Scene mod <see cref="MonoBehaviour"/> definition.
/// </summary>
public class MBehaviour<T> : MComponent<T> where T : Behaviour
{
    public bool? enabled;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (KeyMatch(key, "Enabled") && MValue<bool>.TryJsonParse(ctx, val, out var jval)) enabled = jval;
        else base.JsonParsePair(ctx, key, val);
    }

    public override T ApplyInternal(T component)
    {
        if (enabled != null) component.enabled = (bool)enabled;
        return base.ApplyInternal(component);
    }
}