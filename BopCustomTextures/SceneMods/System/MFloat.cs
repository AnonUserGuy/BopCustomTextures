using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BopCustomTextures.SceneMods.System;

public class MFloat : MValue<float>
{
    private static readonly Regex InfinityRegex = new Regex(@"^\s*(\+|-)?\s*inf(?:inity)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        if (TryJsonParse(ctx, jtoken, out float val))
        {
            Value = val;
            return true;
        }
        return false;
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out float res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            Match match = InfinityRegex.Match((string)jtoken);
            if (match.Success)
            {
                if (match.Groups[1].Length > 0 && match.Groups[1].Value[0] == '-')
                {
                    res = float.NegativeInfinity;
                }
                else
                {
                    res = float.PositiveInfinity;
                }
                return true;
            }
            ctx.Logger.LogJsonParseError(jtoken.Path, "mfloat", "string wasn't \"Infinity\" or \"-Infinity\"");
        }
        else if (jtoken.Type == JTokenType.Float || jtoken.Type == JTokenType.Integer)
        {
            res = (float)jtoken;
            return true;
        }
        ctx.Logger.LogJsonParseError(jtoken.Path, "mfloat", "not a float, int, \"Infinity\", or \"-Infinity\"");

        res = default;
        return false;
    }
}
