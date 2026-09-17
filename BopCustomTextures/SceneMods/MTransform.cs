using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="Transform"/> definition
/// </summary>
[MComponent("Transform")]
public class MTransform: MComponent<Transform>
{
    public Vector3? localPosition;
    public Quaternion? localRotation;
    public Vector3? localEulerAngles;
    public Vector3? localScale;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (ctx.TryGetJVector3(key, val, "LocalPosition", out var vector3)) localPosition = vector3;
        else if (ctx.TryGetJQuaternion(key, val, "LocalRotation", out var quaternion)) localRotation = quaternion;
        else if (ctx.TryGetJEulerAngles(key, val, "LocalEulerAngles", out vector3)) localEulerAngles = vector3;
        else if (ctx.TryGetJVector3(key, val, "LocalScale", out vector3)) localScale = vector3;
    }
    public override Transform Apply(Transform component)
    {
        if (localPosition != null) component.localPosition = ApplyVector3((Vector3)localPosition, component.localPosition);
        if (localRotation != null) component.localRotation = ApplyQuaternion((Quaternion)localRotation, component.localRotation);
        else if (localEulerAngles != null) component.localEulerAngles = ApplyVector3((Vector3)localEulerAngles, component.localEulerAngles);
        if (localScale != null) component.localScale = ApplyVector3((Vector3)localScale, component.localScale);
        return component;
    }
}
