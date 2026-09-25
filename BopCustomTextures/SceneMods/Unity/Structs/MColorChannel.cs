using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace BopCustomTextures.SceneMods.Unity.Structs;

/// <summary>
/// Scene mod <see cref="float"/> definition for floats that represent color values. Used by <see cref="MColor"/>.
/// </summary>
public class MColorChannel : MFloat
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    public override bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        return TryJsonParseKey(ctx, key, out Value);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out float res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            if (TryParseString((string)jtoken, out res)) return true;
            ctx.Logger.LogJsonParseError(jtoken.Path, "color channel", "not parseable as hex int from 00-FF, \"Infinity\", or \"-Infinity\"");
            return false;
        }
        else if (jtoken.Type == JTokenType.Float)
        {
            res = (float)jtoken;
            return true;
        }
        else if (jtoken.Type == JTokenType.Integer)
        {
            res = (float)jtoken / 255.0f;
            return true;
        }
        ctx.Logger.LogJsonParseError(jtoken.Path, "color channel", "not a float, int, \"Infinity\", or \"-Infinity\"");

        res = default;
        return false;
    }

    new public static bool TryJsonParseKey(CustomJsonInitializer ctx, string key, out float res)
    {
        if (TryParseString(key, out res) || float.TryParse(key, out res)) return true;
        ctx.Logger.LogJsonParseError(key, "color channel", "not parseable as hex int from 00-FF, float, \"Infinity\", or \"-Infinity\"");
        return false;
    }

    public static bool TryParseString(string str, out float res)
    {
        if (str.Length == 2 && int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var intres)) 
        {
            res = intres / 255.0f;
            return true;
        }
        return TryParseInfinity(str, out res);
    }
}
