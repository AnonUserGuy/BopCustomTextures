using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Unity.Structs;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Scene mod <see cref="Transform"/> definition
/// </summary>
[MComponent("Transform")]
public class MTransform : MComponent<Transform>
{
    public Vector3? localPosition;
    public Quaternion? localRotation;
    public Vector3? localEulerAngles;
    public Vector3? localScale;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if      (KeyMatch(key, "LocalPosition") && MVector3.TryJsonParse(ctx, val, out var vector3)) localPosition = vector3;
        else if (KeyMatch(key, "LocalRotation") && MQuaternion.TryJsonParse(ctx, val, out var quaternion)) localRotation = quaternion;
        else if (KeyMatch(key, "LocalEulerAngles") && MEulerAngles.TryJsonParse(ctx, val, out vector3)) localEulerAngles = vector3;
        else if (KeyMatch(key, "LocalScale") && MVector3.TryJsonParse(ctx, val, out vector3)) localScale = vector3;
        else base.JsonParsePair(ctx, key, val);
    }
    public override Transform ApplyInternal(Transform component)
    {
        if (localPosition != null) component.localPosition = MVector3.Apply(localPosition, component.localPosition);
        if (localRotation != null) component.localRotation = MQuaternion.Apply(localRotation, component.localRotation);
        else if (localEulerAngles != null) component.localEulerAngles = MVector3.Apply(localEulerAngles, component.localEulerAngles);
        if (localScale != null) component.localScale = MVector3.Apply(localScale, component.localScale);
        return base.ApplyInternal(component);
    }
}
