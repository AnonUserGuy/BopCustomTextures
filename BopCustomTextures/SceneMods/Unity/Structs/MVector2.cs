using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MVector2 : MBaseVector<Vector2>
{
    public const int Width = 2;

    [SceneModParser(typeof(Vector2))]
    public static bool TryJsonParse(JToken val, out MVector2 mvector)
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

    public MVector2(JObject jobj) : base(Width)
    {
        JToken jfloat;
        MFloat mfloat;
        if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[0] = mfloat;
        if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[1] = mfloat;
    }

    public MVector2(JArray jarray) : base(Width, jarray) { }

    public MVector2(params MFloat[] values) : base(Width, values) { }


    public override Vector2 Apply(Vector2 src)
    {
        for (int i = 0; i < Width; i++)
        {
            if (Values[i] != null) src[i] = Values[i].Value;
        }
        return src;
    }
}
