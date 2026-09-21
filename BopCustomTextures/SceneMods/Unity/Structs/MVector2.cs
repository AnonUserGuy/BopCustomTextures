using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MVector2 : MBaseVector<Vector2>
{
    private const int width = 2;
    public override int Width { get => width; }

    public override float this[int i]
    {
        get => Value[i];
        set => Value[i] = value;
    }
    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken val, out Vector2 vector)
    {
        var mvector = new MVector2();
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
        return true;
    }

    public static Vector2 Apply(Vector2? src, Vector2 dest)
    {
        if (src.HasValue) dest = Apply(src.Value, dest);
        return dest;
    }
    public static Vector2 Apply(Vector2 src, Vector2 dest)
    {
        for (int i = 0; i < width; i++)
        {
            if (!float.IsNaN(src[i])) dest[i] = src[i];
        }
        return dest;
    }

    public override Vector2 Apply(Vector2 dest)
    {
        return Apply(Value, dest);
    }
}
