using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using BopCustomTextures.SceneMods.Unity.Structs;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene mod <see cref="Camera"/> definition.
/// </summary>
[MComponent("Camera")]
public class MCamera : MComponent<Camera>
{
    public bool? orthographic;
    public float? orthographicSize;
    public float? aspect;
    public Color? backgroundColor;

    public override void JsonParsePair(string key, JToken val)
    {
        if      (KeyMatch(key, "Orthographic") && MValue<bool>.TryJsonParse(val, out var jval)) orthographic = jval;
        else if (KeyMatch(key, "OrthographicSize") && MFloat.TryJsonParse(val, out var jfloat)) orthographicSize = jfloat;
        else if (KeyMatch(key, "Aspect") && MFloat.TryJsonParse(val, out jfloat)) aspect = jfloat;
        else if (KeyMatch(key, "BackgroundColor") && MColor.TryJsonParse(val, out var color)) backgroundColor = color;
        else base.JsonParsePair(key, val);
    }

    public override Camera Apply(Camera component)
    {
        if (orthographic != null) component.orthographic = (bool)orthographic;
        if (orthographicSize != null) component.orthographicSize = (float)orthographicSize;
        if (aspect != null) component.aspect = (float)aspect;
        if (backgroundColor != null) component.backgroundColor = MColor.Apply(backgroundColor, component.backgroundColor);
        base.Apply(component);
        return component;
    }
}
