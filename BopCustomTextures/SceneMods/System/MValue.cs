using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod defintion for value types, including structs and primatives.
/// </summary>
/// <typeparam name="T">Target value type, such as a struct or primative.</typeparam>
public class MValue<T> : MBase<T> where T : struct
{
    public T Value;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        return TryJsonParse(ctx, jtoken, out Value);
    }

    public static bool TryJsonParse(CustomJsonInitializer ctx, JToken jtoken, out T res)
    {
        if (jtoken is not JValue jvalue)
        {
            ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T).Name, "not value");
            res = default;
            return false;
        }

        try
        {
            res = (T)Convert.ChangeType(jvalue.Value, typeof(T));
                
        }
        catch (InvalidCastException)
        {
            ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T).Name, $"\"{jvalue.Value}\" not castable to {typeof(T).Name}");
            res = default;
            return false;
        }

        return true;
    }

    public override T Apply(T obj)
    {
        return Value;
    }

    public override T Apply()
    {
        return Value;
    }
}
