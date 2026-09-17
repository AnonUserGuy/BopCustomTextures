using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

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

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (ctx.TryGetJValue(key, val, "orthographic", JTokenType.Boolean, out var jval)) orthographic = (bool)jval;
        else if (ctx.TryGetJFloat(key, val, "orthographicSize", out var jfloat)) orthographicSize = jfloat;
        else if (ctx.TryGetJFloat(key, val, "aspect", out jfloat)) aspect = jfloat;
        else if (ctx.TryGetJColor(key, val, "backgroundColor", out var color)) backgroundColor = color;
        else base.JsonParsePair(ctx, key, val);
    }

    public override Camera Apply(Camera component)
    {
        if (orthographic != null) component.orthographic = (bool)orthographic;
        if (orthographicSize != null) component.orthographicSize = (float)orthographicSize;
        if (aspect != null) component.aspect = (float)aspect;
        if (backgroundColor != null) component.backgroundColor = ApplyColor((Color)backgroundColor, component.backgroundColor);
        return component;
    }
}
