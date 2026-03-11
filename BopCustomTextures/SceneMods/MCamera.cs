using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="Camera"/> definition.
/// </summary>
public class MCamera : MComponent<Camera>
{
    public bool? orthographic;
    public float? orthographicSize;
    public float? aspect;
    public Color? backgroundColor;

    public static void Register()
    {
        MComponentParserRegistry.Register("Camera", JsonParse);
    }

    public static MCamera JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        var mcomponent = new MCamera();
        if (ctx.TryGetJValue(jcomponent, "Orthographic", JTokenType.Boolean, out var jval)) mcomponent.orthographic = (bool)jval;
        if (ctx.TryGetJFloat(jcomponent, "OrthographicSize", out var jfloat)) mcomponent.orthographicSize = jfloat;
        if (ctx.TryGetJFloat(jcomponent, "Aspect", out jfloat)) mcomponent.aspect = jfloat;
        if (ctx.TryGetJColor(jcomponent, "BackgroundColor", out var color)) mcomponent.backgroundColor = color;
        return mcomponent;
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
