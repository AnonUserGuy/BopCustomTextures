using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MColor : MBaseVector<Color>
{
    public const int Width = 4;

    [SceneModParser(typeof(Color))]
    public static bool TryJsonParse(JToken val, out MColor mvector)
    {
        switch (val.Type)
        {
            case JTokenType.Object:
                mvector = new((JObject)val);
                return true;
            case JTokenType.Array:
                mvector = new((JArray)val);
                return true;
            case JTokenType.String:
                return TryJsonParse((string)val, out mvector);
        }
        mvector = null;
        return false;
    }

    public static bool TryJsonParse(string str, out MColor mvector)
    {
        str = str.TrimStart('#');
        if (str.Length > 8)
        {
            str = str.Substring(0, 8);
        }
        if (!int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            BopCustomTexturesPlugin.LogWarning($"JSON color string \"{str}\" couldn't be parsed as as color");
            mvector = null;
            return false;
        }
        mvector = new(str.Length / 2, rgb);
        return true;
    }

    private MColor(int width, int rgb): base(Width)
    {
        for (int i = width - 1; i >= 0; i--)
        {
            Values[i] = new((rgb & 0xFF) / 255.0f);
            rgb >>= 8;
        }
    }

    public MColor(JObject jobj) : base(Width)
    {
        JToken jfloat;
        MFloat mfloat;
        if (jobj.TryGetValue("r", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[0] = mfloat;
        if (jobj.TryGetValue("g", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[1] = mfloat;
        if (jobj.TryGetValue("b", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[2] = mfloat;
        if (jobj.TryGetValue("a", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) Values[3] = mfloat;
    }

    public MColor(JArray jarray) : base(Width, jarray) { }

    public MColor(params MFloat[] values) : base(Width, values) { }

    public override Color Apply(Color src)
    {
        for (int i = 0; i < Width; i++)
        {
            if (Values[i] != null) src[i] = Values[i].Value;
        }
        return src;
    }
}
