using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BopCustomTextures.SceneMods.System;

public class MIntEnum<T> : MInt where T : struct, Enum
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    public override bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        return TryJsonParseKey(ctx, key, out Value);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out int res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            if (Enum.TryParse((string)jtoken, out T enumRes))
            {
                res = (int)(object)enumRes; // ugh
                return true;
            }
            ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T), "couldn't parse string as enum member");
            res = default;
            return false;
        }
        return TryJsonParse(ctx, jtoken, out res);
    }

    public static bool TryJsonParseKey(CustomJsonInitializer ctx, string key, out int res)
    {
        if (Enum.TryParse(key, out T enumRes))
        {
            res = (int)(object)enumRes; // ugh
            return true;
        }
        else if (int.TryParse(key, out res)) return true;
        ctx.Logger.LogJsonParseError(key, typeof(T), "couldn't parse as enum member or int");
        return false;
    }
}
