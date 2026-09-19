using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace BopCustomTextures.SceneMods.Unity.Structs;

public class MColor : MBaseVector<Color>
{
    private const int width = 4;
    public override int Width { get => width; }

    public override float this[int i]
    {
        get => Value[i];
        set => Value[i] = value;
    }

    [SceneModParser(typeof(Color))]
    public static bool TryJsonParseWrapped(JToken val, out MColor mvector)
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
                return TryJsonParseWrapped((string)val, out mvector);
        }
        mvector = null;
        return false;
    }

    public static bool TryJsonParseWrapped(string str, out MColor mvector)
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

    new public static bool TryJsonParse(JToken val, out Color vector)
    {
        if (TryJsonParseWrapped(val, out var mvector))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }


    private MColor(int width, int rgb)
    {
        for (int i = width - 1; i >= 0; i--)
        {
            this[i] = (rgb & 0xFF) / 255.0f;
            rgb >>= 8;
        }
    }

    public MColor(JObject jobj)
    {
        JToken jfloat;
        float mfloat;
        if (jobj.TryGetValue("r", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[0] = mfloat;
        if (jobj.TryGetValue("g", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[1] = mfloat;
        if (jobj.TryGetValue("b", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[2] = mfloat;
        if (jobj.TryGetValue("a", out jfloat) && MFloat.TryJsonParse(jfloat, out mfloat)) this[3] = mfloat;
    }

    public MColor(JArray jarray) : base(jarray) { }

    public MColor(params float[] values) : base(values) { }

    public static Color Apply(Color? src, Color dest)
    {
        if (src.HasValue) Apply(src.Value, dest);
        return dest;
    }
    public static Color Apply(Color src, Color dest)
    {
        for (int i = 0; i < width; i++)
        {
            if (!float.IsNaN(src[i])) dest[i] = src[i];
        }
        return dest;
    }

    public override Color Apply(Color dest)
    {
        return Apply(Value, dest);
    }
}
