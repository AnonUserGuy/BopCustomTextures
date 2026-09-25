using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod <see cref="float"/> definition.
/// </summary>
public class MFloat : MValue<float>, IMKey
{
    private static readonly Regex InfinityRegex = new Regex(@"^\s*(\+|-)?\s*inf(?:inity)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    public virtual bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        return TryJsonParseKey(ctx, key, out Value);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out float res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            if (TryParseInfinity((string)jtoken, out res)) return true;
            ctx.Logger.LogJsonParseError(jtoken.Path, "float", "string wasn't \"Infinity\" or \"-Infinity\"");
            return false;
        }
        else if (jtoken.Type == JTokenType.Float || jtoken.Type == JTokenType.Integer)
        {
            res = (float)jtoken;
            return true;
        }
        ctx.Logger.LogJsonParseError(jtoken.Path, "float", "not a float, int, \"Infinity\", or \"-Infinity\"");

        res = default;
        return false;
    }

    public static bool TryJsonParseKey(CustomJsonInitializer ctx, string key, out float res)
    {
        if (TryParseInfinity(key, out res) || float.TryParse(key, out res)) return true;
        ctx.Logger.LogJsonParseError(key, "float", "not parseable as float, \"Infinity\", or \"-Infinity\"");
        return false;
    }

    public static bool TryParseInfinity(string str, out float inf)
    {
        Match match = InfinityRegex.Match(str);
        if (match.Success)
        {
            if (match.Groups[1].Length > 0 && match.Groups[1].Value[0] == '-')
            {
                inf = float.NegativeInfinity;
            }
            else
            {
                inf = float.PositiveInfinity;
            }
            return true;
        }
        inf = float.NaN;
        return false;
    }
}
