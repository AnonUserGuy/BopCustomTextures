using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.System;

public class MValue<T> : MBase<T> where T : struct
{
    public T Value;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        if (TryJsonParse(ctx, jtoken, out T val))
        {
            Value = val;
            return true;
        }
        return false;
    }

    public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out T res)
    {
        if (jtoken is JValue jvalue && jvalue.Value is T val)
        {
            res = val;
            return true;
        }
        ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T).Name, "not value");
        res = default;
        return false;
    }

    public override T Apply(T obj)
    {
        return Value;
    }
}
