using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MQuaternion : MBaseVector<Quaternion>
{
    private const int width = 4;
    public override int Width { get => width; }

    public override float this[int i]
    {
        get => Value[i];
        set => Value[i] = value;
    }
    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken val, out Quaternion vector)
    {
        var mvector = new MQuaternion();
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
        if (jobj.TryGetValue("w", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[3] = mfloat;
        return true;
    }

    public static Quaternion Apply(Quaternion? src, Quaternion dest)
    {
        if (src.HasValue) dest = Apply(src.Value, dest);
        return dest;
    }
    public static Quaternion Apply(Quaternion src, Quaternion dest)
    {
        for (int i = 0; i < width; i++)
        {
            if (!float.IsNaN(src[i])) dest[i] = src[i];
        }
        return dest;
    }

    public override Quaternion Apply(Quaternion dest)
    {
        return Apply(Value, dest);
    }
}
