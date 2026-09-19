using BopCustomTextures.SceneMods.Base;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene mod <see cref="MonoBehaviour"/> definition.
/// </summary>
public abstract class MBehaviour<T> : MComponent<T> where T : Behaviour
{
    public bool? enabled;

    public override void JsonParsePair(string key, JToken val)
    {
        if (KeyMatch(key, "Enabled") && MValue<bool>.TryJsonParse(val, out var jval)) enabled = jval;
        else base.JsonParsePair(key, val);
    }

    public override T Apply(T component)
    {
        if (enabled != null) component.enabled = (bool)enabled;
        base.Apply(component);
        return component;
    }
}