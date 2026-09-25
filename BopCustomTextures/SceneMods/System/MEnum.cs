#if false
// I only added enum functionality so Animators could be made to play animations from their generated "AnimationState" enums,
// but you can also play animations by their string animation names. Because MEnum is a bit buggy currently, I'm removing it 
// until I see good enough reason to re-add it and fix it in the future.

using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BopCustomTextures.SceneMods.System;

public class MEnum<T> : MValue<T>, IMKey<T> where T : struct, Enum
{
    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    public bool JsonParseKey(CustomJsonInitializer ctx, string key)
    {
        return TryJsonParseKey(ctx, key, out Value);
    }

    new public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out T res)
    {
        if (jtoken.Type == JTokenType.String)
        {
            if (Enum.TryParse((string)jtoken, out res))
            {
                return true;
            }
            ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T), "couldn't parse string as enum member");
            res = default;
            return false;
        }
        return TryJsonParse(ctx, jtoken, out res);
    }

    public static bool TryJsonParseKey(CustomJsonInitializer ctx, string key, out T res)
    {
        if (Enum.TryParse(key, out res)) return true;
        else if (int.TryParse(key, out int intRes))
        {
            res = (T)(object)intRes; // ugh
            return true;
        }
        ctx.Logger.LogJsonParseError(key, typeof(T), "couldn't parse as enum member or int");
        return false;
    }
}
#endif
