using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MVector3 : MBaseVector<Vector3>
{
	public const int Width = 3;

	[SceneModParser(typeof(Vector3))]
	public static bool TryJsonParse(JToken val, out MVector3 mvector)
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

	public static bool TryJsonParseEulerAngles(JToken val, out MVector3 mvector)
	{
		if (val.Type == JTokenType.Float || val.Type == JTokenType.Integer)
		{
            if (!MFloat.TryJsonParse(val, out var mfloat))
			{
				mvector = null;
				return false;
			}
			mvector = new(null, null, mfloat);
			return true;
        }
		return TryJsonParse(val, out mvector);
    }


	public MVector3(JObject jobj) : base(Width)
	{
		JToken jfloat;
		MFloat mfloat;
		if (jobj.TryGetValue("x", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[0] = mfloat;
		if (jobj.TryGetValue("y", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[1] = mfloat;
		if (jobj.TryGetValue("z", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[2] = mfloat;
	}

	public MVector3(JArray jarray) : base(Width, jarray) { }

    public MVector3(params MFloat[] values) : base(Width, values) { }

    public override Vector3 Apply(Vector3 src)
	{
		for (int i = 0; i < Width; i++)
		{
			if (Values[i] != null) src[i] = Values[i].Value;
		}
		return src;
	}
}
