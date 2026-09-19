using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Base;

public class MValue<T> : MBase<T> where T : struct
{
    public T Value;

    [SceneModParser(typeof(int))]
    [SceneModParser(typeof(bool))]
    public static bool TryJsonParseWrapped(JToken jtoken, out MValue<T> res)
    {
        if (TryJsonParse(jtoken, out T val))
        {
            res = new(val);
            return true;
        }
        res = null;
        return false;
    }

    public static bool TryJsonParse(JToken jtoken, out T res)
    {
        if (jtoken is JValue jvalue && jvalue.Value is T val)
        {
            res = val;
            return true;
        }
        res = default;
        return false;
    }

    public override T Apply(T obj)
    {
        return Value;
    }

    public MValue()
    {

    }

    public MValue(T val)
    {
        Value = val;
    }
}
