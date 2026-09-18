using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MQuaternion : MBaseVector<Quaternion>
{
    public const int Width = 4;

    [SceneModParser(typeof(Quaternion))]
    public static bool TryJsonParse(JToken val, out MQuaternion mvector)
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

    public MQuaternion(JObject jobj) : base(Width)
    {
        JToken jfloat;
        MFloat mfloat;
        if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[0] = mfloat;
        if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[1] = mfloat;
        if (jobj.TryGetValue("z", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[2] = mfloat;
        if (jobj.TryGetValue("w", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[3] = mfloat;
    }

    public MQuaternion(JArray jarray) : base(Width, jarray) { }

    public MQuaternion(params MFloat[] values) : base(Width, values) { }

    public override Quaternion Apply(Quaternion src)
    {
        for (int i = 0; i < Width; i++)
        {
            if (Values[i] != null) src[i] = Values[i].Value;
        }
        return src;
    }
}
