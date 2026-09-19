using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MVector3 : MBaseVector<Vector3>
{
    private const int width = 3;
    public override int Width { get => width; }

    public override float this[int i]
    {
        get => Value[i];
        set => Value[i] = value;
    }

    [SceneModParser(typeof(Vector3))]
	public static bool TryJsonParseWrapped(JToken val, out MVector3 mvector)
	{
		switch (val)
		{
			case JObject jobj2:
				mvector = new MVector3(jobj2);
				return true;
			case JArray jarray2:
				mvector = new MVector3(jarray2);
				return true;
		}
		mvector = null;
		return false;
	}

    new public static bool TryJsonParse(JToken val, out Vector3 vector)
    {
        if (TryJsonParseWrapped(val, out var mvector))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }

    public static bool TryJsonParseEulerAngles(JToken val, out Vector3 mvector)
	{
		if (val.Type == JTokenType.Float || val.Type == JTokenType.Integer)
		{
            if (!MFloat.TryJsonParse(val, out float mfloat))
			{
				mvector = default;
				return false;
			}
			mvector = new(float.NaN, float.NaN, mfloat);
			return true;
        }
		return TryJsonParse(val, out mvector);
    }

    public MVector3(JObject jobj)
	{
		JToken jfloat;
		float mfloat;
		if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[0] = mfloat;
		if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[1] = mfloat;
		if (jobj.TryGetValue("z", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[2] = mfloat;
	}

	public MVector3(JArray jarray) : base(jarray) { }

    public MVector3(params float[] values) : base(values) { }

    public static Vector3 Apply(Vector3? src, Vector3 dest)
    {
        if (src.HasValue) Apply(src.Value, dest);
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
