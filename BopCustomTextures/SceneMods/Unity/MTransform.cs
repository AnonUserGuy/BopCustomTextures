using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Unity.Structs;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene mod <see cref="Transform"/> definition
/// </summary>
[MComponent("Transform")]
public class MTransform : MComponent<Transform>
{
    public MVector3 localPosition;
    public MQuaternion localRotation;
    public MVector3 localEulerAngles;
    public MVector3 localScale;

    public override void JsonParsePair(string key, JToken val)
    {
        if      (KeyMatch(key, "LocalPosition") && MVector3.TryJsonParse(val, out var vector3)) localPosition = vector3;
        else if (KeyMatch(key, "LocalRotation") && MQuaternion.TryJsonParse(val, out var quaternion)) localRotation = quaternion;
        else if (KeyMatch(key, "LocalEulerAngles") && MVector3.TryJsonParseEulerAngles(val, out vector3)) localEulerAngles = vector3;
        else if (KeyMatch(key, "LocalScale") && MVector3.TryJsonParse(val, out vector3)) localScale = vector3;
        else base.JsonParsePair(key, val);
    }
    public override Transform Apply(Transform component)
    {
        if (localPosition != null) component.localPosition = localPosition.Apply(component.localPosition);
        if (localRotation != null) component.localRotation = localRotation.Apply(component.localRotation);
        else if (localEulerAngles != null) component.localEulerAngles = localEulerAngles.Apply(component.localEulerAngles);
        if (localScale != null) component.localScale = localScale.Apply(component.localScale);
        base.Apply(component);
        return component;
    }
}
