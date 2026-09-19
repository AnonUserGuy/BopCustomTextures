using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BopCustomTextures.SceneMods.Base;

public class MFloat : MValue<float>
{
    private static readonly Regex InfinityRegex = new Regex(@"^\s*(\+|-)?\s*inf(?:inity)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [SceneModParser(typeof(float))]
    public static bool TryJsonParseWrapped(JToken jtoken, out MFloat res)
    {
        if (TryJsonParse(jtoken, out float val))
        {
            res = new(val);
            return true;
        }
        res = null;
        return false;
    }

    new public static bool TryJsonParse(JToken jtoken, out float res)
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
        }
        else if (jtoken.Type == JTokenType.Float || jtoken.Type == JTokenType.Integer)
        {
            res = (float)jtoken;
            return true;
        }
        res = default;
        return false;
    }

    public MFloat()
    {

    }

    public MFloat(float val)
    {
        Value = val;
    }
}
