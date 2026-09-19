using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
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

    [SceneModParser(typeof(Quaternion))]
    public static bool TryJsonParseWrapped(JToken val, out MQuaternion mvector)
    {
        switch (val)
        {
            case JObject jobj2:
                mvector = new MQuaternion(jobj2);
                return true;
            case JArray jarray2:
                mvector = new MQuaternion(jarray2);
                return true;
        }
        mvector = null;
        return false;
    }

    new public static bool TryJsonParse(JToken val, out Quaternion vector)
    {
        if (TryJsonParseWrapped(val, out var mvector))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }

    public MQuaternion(JObject jobj)
    {
        JToken jfloat;
        float mfloat;
        if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[0] = mfloat;
        if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[1] = mfloat;
        if (jobj.TryGetValue("z", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[2] = mfloat;
        if (jobj.TryGetValue("w", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[3] = mfloat;
    }

    public MQuaternion(JArray jarray) : base(jarray) { }

    public MQuaternion(params float[] values) : base(values) { }

    public static Quaternion Apply(Quaternion? src, Quaternion dest)
    {
        if (src.HasValue) Apply(src.Value, dest);
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
