using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod <see cref="string"/> definition.
/// </summary>
public class MString : MObject<string>, IMKey<string>
{
    public string Value;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    public bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        Value = key;
        return true;
    }

    public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out string res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            res = (string)jtoken;
            return true;
        }
        ctx.Logger.LogJsonParseError(jtoken.Path, "string", "not string");
        res = default;
        return false;
    }

    public override string Apply(string obj)
    {
        return Value;
    }
}
