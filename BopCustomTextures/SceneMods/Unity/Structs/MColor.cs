using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
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

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        if (val.Type == JTokenType.String)
        {
            InitValue();
            return JsonParse(ctx, val, (string)val);
        }
        return base.JsonParse(ctx, val);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken val, out Color vector)
    {
        var mvector = new MColor();
        if (mvector.JsonParse(ctx, val))
        {
            vector = mvector.Value;
            return true;
        }
        vector = default;
        return false;
    }

    public bool JsonParse(CustomJsonInitializer ctx, JToken val, string str)
    {
        str = str.TrimStart('#');
        if (str.Length > 8)
        {
            str = str.Substring(0, 8);
        }
        if (!int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            ctx.Logger.LogJsonParseError(val.Path, "Color", $"couldn't parse string as color \"{str}\"");
            return false;
        }
        for (int i = str.Length / 2 - 1; i >= 0; i--)
        {
            this[i] = (rgb & 0xFF) / 255.0f;
            rgb >>= 8;
        }
        return true;
    }

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jobj)
    {
        JToken jfloat;
        float mfloat;
        if (jobj.TryGetValue("r", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[0] = mfloat;
        if (jobj.TryGetValue("g", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[1] = mfloat;
        if (jobj.TryGetValue("b", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[2] = mfloat;
        if (jobj.TryGetValue("a", out jfloat) && MFloat.TryJsonParse(ctx, jfloat, out mfloat)) this[3] = mfloat;
        return true;
    }

    public static Color Apply(Color? src, Color dest)
    {
        if (src.HasValue) dest = Apply(src.Value, dest);
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
