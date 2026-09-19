using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MVector2 : MBaseVector<Vector2>
{
    private const int width = 2;
    public override int Width { get => width; }

    public override float this[int i] {
        get => Value[i];
        set => Value[i] = value;
    }

    [SceneModParser(typeof(Vector2))]
    public static bool TryJsonParseWrapped(JToken val, out MVector2 mvector)
    {
        switch (val)
        {
            case JObject jobj2:
                mvector = new MVector2(jobj2);
                return true;
            case JArray jarray2:
                mvector = new MVector2(jarray2);
                return true;
        }
        mvector = null;
        return false;
    }

    new public static bool TryJsonParse(JToken val, out Vector2 vector)
    {
        if (TryJsonParseWrapped(val, out var mvector))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }

    public MVector2(JObject jobj)
    {
        JToken jfloat;
        float mfloat;
        if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[0] = mfloat;
        if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[1] = mfloat;
    }

    public MVector2(JArray jarray) : base(jarray) { }

    public MVector2(params float[] values) : base(values) { }

    public static Vector2 Apply(Vector2? src, Vector2 dest)
    {
        if (src.HasValue) Apply(src.Value, dest);
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
