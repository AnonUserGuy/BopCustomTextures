using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace BopCustomTextures.SceneMods.Base;

public class MFloat(float value): MValue<float>(value)
{
    private static readonly Regex InfinityRegex = new Regex(@"^\s*(\+|-)?\s*inf(?:inity)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [SceneModParser(typeof(float))]
    public static bool TryJsonParse(JToken jtoken, out MFloat res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            Match match = InfinityRegex.Match((string)jtoken);
            if (match.Success)
            {
                if (match.Groups[1].Length > 0 && match.Groups[1].Value[0] == '-')
                {
                    res = new(float.NegativeInfinity);
                }
                else
                {
                    res = new(float.PositiveInfinity);
                }
                return true;
            }
        }
        else if (jtoken.Type == JTokenType.Float || jtoken.Type == JTokenType.Integer)
        {
            res = new((float)jtoken);
            return true;
        }
        res = default;
        return false;
    }
}
