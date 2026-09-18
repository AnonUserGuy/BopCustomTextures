using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BopCustomTextures.SceneMods.Base;

public abstract class MBase
{
    public static bool TryJsonParse(Type type, JToken val, out MBase res)
    {
        return SceneModParserRegistry.TryJsonParseStatic(type, val, out res);
    }

    public abstract object Apply(object obj);
}

public abstract class MBase<T> : MBase
{
    public static bool TryGetAssigned(object obj, out T obj2)
    {
        if (!IsAssignable(obj.GetType()))
        {
            obj2 = default;
            return false;
        }
        obj2 = (T)obj;
        return true;
    }

    public static bool IsAssignable(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    public override object Apply(object obj)
    {
        if (!TryGetAssigned(obj, out var obj2)) return obj;
        return Apply(obj2);
    }

    public abstract T Apply(T obj);
}
