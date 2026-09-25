using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BopCustomTextures.SceneMods.Unity.Structs;

/// <summary>
/// Scene mod <see cref="Color"/> definition.
/// </summary>
public class MColor : MBaseVector<Color>, IMKey<Color>
{
    public static readonly Regex ColorRegex = new Regex(@"^(?:#|0x)?([\da-f]{2})([\da-f]{2})?([\da-f]{2})?([\da-f]{2})?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected override string LogInvalidTypeMsg => "not string, object, or array";

    private const int width = 4;
    public override int Width => width;

    public override float this[int i]
    {
        get => Value[i];
        set => Value[i] = value;
    }

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        if (jtoken.Type == JTokenType.String)
        {
            return JsonParseKey(ctx, (string)jtoken);
        }
        return base.JsonParse(ctx, jtoken);
    }

    public bool JsonParseKey(CustomJsonInitializer ctx, string str)
    {
        Match match = ColorRegex.Match(str);
        if (!match.Success)
        {
            ctx.Logger.LogJsonParseError(str, "color", "not parseable as color string");
            return false;
        }
        InitValue();

        int i = 1;
        for (Group group = match.Groups[i]; group.Success && i < match.Groups.Count; group = match.Groups[++i])
        {
            if (MColorChannel.TryJsonParseKey(ctx, group.Value, out var res)) this[i - 1] = res;
        }
        return true;
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

    public static bool TryJsonParseKey(CustomJsonInitializer ctx, string key, out Color vector)
    {
        var mvector = new MColor();
        if (mvector.JsonParseKey(ctx, key))
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
        if (jobj.TryGetValue("r", out jfloat) && MColorChannel.TryJsonParse(ctx, jfloat, out mfloat)) this[0] = mfloat;
        if (jobj.TryGetValue("g", out jfloat) && MColorChannel.TryJsonParse(ctx, jfloat, out mfloat)) this[1] = mfloat;
        if (jobj.TryGetValue("b", out jfloat) && MColorChannel.TryJsonParse(ctx, jfloat, out mfloat)) this[2] = mfloat;
        if (jobj.TryGetValue("a", out jfloat) && MColorChannel.TryJsonParse(ctx, jfloat, out mfloat)) this[3] = mfloat;
        return true;
    }

    public override bool JsonParse(CustomJsonInitializer ctx, JArray jvector)
    {
        float mfloat;
        if (MColorChannel.TryJsonParse(ctx, jvector[0], out mfloat)) this[0] = mfloat;
        if (MColorChannel.TryJsonParse(ctx, jvector[1], out mfloat)) this[1] = mfloat;
        if (MColorChannel.TryJsonParse(ctx, jvector[2], out mfloat)) this[2] = mfloat;
        if (MColorChannel.TryJsonParse(ctx, jvector[3], out mfloat)) this[3] = mfloat;
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
