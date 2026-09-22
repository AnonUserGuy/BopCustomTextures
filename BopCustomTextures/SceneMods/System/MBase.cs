using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BopCustomTextures.SceneMods.System;

public interface IMBase
{
    public bool JsonParse(CustomJsonInitializer ctx, Type type, JToken val);

    public object Apply(object obj);

    public void ApplyReadOnly(object obj);

    public bool IsAssignable(Type type);
}

public interface IMBase<T> : IMBase
{
    public bool JsonParse(CustomJsonInitializer ctx, JToken val);

    public T Apply(T obj);

    public void ApplyReadOnly(T obj);
}

public interface IMKey<T>
{
    public bool JsonParseKey(CustomJsonInitializer ctx, string key);
}

public abstract class MBase : IMBase
{
    public abstract bool JsonParse(CustomJsonInitializer ctx, Type type, JToken val);

    public static bool TryJsonParse(CustomJsonInitializer ctx, Type type, JToken val, out IMBase res)
    {
        return MComponentParserRegistry.Instance.TryParseJson(ctx, type, val, out res);
    }
    public static bool TryJsonParse<T>(CustomJsonInitializer ctx, Type type, JToken val, out T res) where T : IMBase, new()
    {
        res = new();
        if (res.JsonParse(ctx, type, val))
        {
            return true;
        }
        return false;
    }
    public static bool TryJsonParse<T, O>(CustomJsonInitializer ctx, JToken val, out T res) where T : IMBase<O>, new()
    {
        res = new();
        if (res.JsonParse(ctx, val))
        {
            return true;
        }
        return false;
    }

    public abstract object Apply(object obj);

    public virtual void ApplyReadOnly(object obj)
    {
        Apply(obj);
    }

    public abstract bool IsAssignable(Type type);
}

public abstract class MBase<T> : MBase, IMBase<T>
{
    public override bool JsonParse(CustomJsonInitializer ctx, Type type, JToken val)
    {
        return IsAssignable(type) && JsonParse(ctx, val);
    }

    public abstract bool JsonParse(CustomJsonInitializer ctx, JToken val);

    public override object Apply(object obj)
    {
        if (!TryGetAssigned(obj, out var obj2)) return obj;
        return Apply(obj2);
    }

    public override void ApplyReadOnly(object obj)
    {
        if (!TryGetAssigned(obj, out var obj2)) return;
        ApplyReadOnly(obj2);
    }

    public abstract T Apply(T obj);

    public virtual void ApplyReadOnly(T obj)
    {
        Apply(obj);
    }

    public bool TryGetAssigned(object obj, out T obj2)
    {
        if (!IsAssignable(obj.GetType()))
        {
            obj2 = default;
            return false;
        }
        obj2 = (T)obj;
        return true;
    }

    public override bool IsAssignable(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }
}