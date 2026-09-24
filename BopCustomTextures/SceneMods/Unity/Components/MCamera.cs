using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using BopCustomTextures.SceneMods.Unity.Structs;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Components;

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
        if      (KeyMatch(key, "Orthographic") && MValue<bool>.TryJsonParse(ctx, val, out var jval)) orthographic = jval;
        else if (KeyMatch(key, "OrthographicSize") && MFloat.TryJsonParse(ctx, val, out var jfloat)) orthographicSize = jfloat;
        else if (KeyMatch(key, "Aspect") && MFloat.TryJsonParse(ctx, val, out jfloat)) aspect = jfloat;
        else if (KeyMatch(key, "BackgroundColor") && MColor.TryJsonParse(ctx, val, out var color)) backgroundColor = color;
        else base.JsonParsePair(ctx, key, val);
    }

    public override Camera ApplyInternal(Camera component)
    {
        if (orthographic != null) component.orthographic = (bool)orthographic;
        if (orthographicSize != null) component.orthographicSize = (float)orthographicSize;
        if (aspect != null) component.aspect = (float)aspect;
        if (backgroundColor != null) component.backgroundColor = MColor.Apply(backgroundColor, component.backgroundColor);
        return base.ApplyInternal(component);
    }
}
