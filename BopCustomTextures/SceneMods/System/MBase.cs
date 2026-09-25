using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod interface without specific target type.
/// </summary>
public interface IMBase
{
    public bool JsonParse(CustomJsonInitializer ctx, Type type, JToken val);

    public object Apply(object obj);

    public object ApplyNone();

    public void ApplyReadOnly(object obj);

    public bool IsAssignable(Type type);
}

/// <summary>
/// <para>Scene mod interface with specific target type.</para> 
/// 
/// <para>Instead of implementing this, instead classes should descend from <see cref="MBase{T}"/> so automatic
/// type registration for scene mod definition resolution can occur.</para> 
/// </summary>
/// <typeparam name="T">Type that scene mod modifies the members of.</typeparam>
public interface IMBase<T> : IMBase
{
    public bool JsonParse(CustomJsonInitializer ctx, JToken val);

    public T Apply(T obj);

    public T Apply();

    public void ApplyReadOnly(T obj);
}

/// <summary>
/// Scene mod interface for types that can be parsed from a JSON string key. Used by 
/// <see cref="Generic.MIDictionary{G, MKey, TKey, MValue, TValue}"/>.
/// </summary>
/// <typeparam name="T">Class that can be parsed from a string.</typeparam>
public interface IMKey<T>
{
    public bool JsonParseKey(CustomJsonInitializer ctx, string key);
}

/// <summary>
/// Base class of all scene mods, including those that don't target a specific type.
/// </summary>
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

    public abstract object ApplyNone();
}

/// <summary>
/// Root scene mod definition which targets a specific type, <see cref="T"/>. Descendents are automatically registered for
/// scene mod definition resolution if <see cref="MComponentParserRegistry.RegisterAssembly"/> is called on the containing assembly.
/// </summary>
/// <typeparam name="T">Type that scene mod modifies the members of.</typeparam>
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

    public override object ApplyNone()
    {
        return Apply();
    }

    public abstract T Apply();
}