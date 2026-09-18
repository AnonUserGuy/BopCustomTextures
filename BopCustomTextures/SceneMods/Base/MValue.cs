using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Base;

public class MValue<T>(T value): MBase<T>
{
    public T Value = value;

    [SceneModParser(typeof(int))]
    [SceneModParser(typeof(bool))]
    [SceneModParser(typeof(string))]
    public static bool TryJsonParse(JToken jtoken, out MValue<T> res)
    {
        if (jtoken is JValue jvalue && jvalue.Value is T val)
        {
            res = new(val);
            return true;
        }
        res = null;
        return false;
    }

    public override T Apply(T obj)
    {
        return Value;
    }
}
