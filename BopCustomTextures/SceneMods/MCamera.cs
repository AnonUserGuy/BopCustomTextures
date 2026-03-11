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

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        if (ctx.TryGetJValue(jcomponent, "Orthographic", JTokenType.Boolean, out var jval)) orthographic = (bool)jval;
        if (ctx.TryGetJFloat(jcomponent, "OrthographicSize", out var jfloat)) orthographicSize = jfloat;
        if (ctx.TryGetJFloat(jcomponent, "Aspect", out jfloat)) aspect = jfloat;
        if (ctx.TryGetJColor(jcomponent, "BackgroundColor", out var color)) backgroundColor = color;
        return true;
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
