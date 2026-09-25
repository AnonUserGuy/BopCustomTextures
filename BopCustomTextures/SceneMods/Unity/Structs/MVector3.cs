using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

/// <summary>
/// Scene mod <see cref="Vector3"/> definition.
/// </summary>
public class MVector3 : MBaseVector<Vector3>
{
    private const int width = 3;
    public override int Width => width;

    public override float this[int i]
    {
        get => Value[i];
        set => Value[i] = value;
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken val, out Vector3 vector)
    {
        var mvector = new MVector3();
        if (mvector.JsonParse(ctx, val))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jobj)
    {
        JToken jfloat;
        float mfloat;
        if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[0] = mfloat;
        if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[1] = mfloat;
        if (jobj.TryGetValue("z", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[2] = mfloat;
        return true;
    }

    public static Vector3 Apply(Vector3? src, Vector3 dest)
    {
        if (src.HasValue) dest = Apply(src.Value, dest);
        return dest;
    }
    public static Vector3 Apply(Vector3 src, Vector3 dest)
    {
        for (int i = 0; i < width; i++)
        {
            if (!float.IsNaN(src[i])) dest[i] = src[i];
        }
        return dest;
    }

    public override Vector3 Apply(Vector3 dest)
    {
        return Apply(Value, dest);
    }
}
