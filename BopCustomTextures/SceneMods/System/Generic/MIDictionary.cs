using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

public class MIDictionary<MKey, TKey, MValue, TValue> : MObject<IDictionary<TKey, TValue>> 
    where MKey : IMBase<TKey>, new() // also optionally IMKey<TKey>
    where MValue : IMBase<TValue>, new()
{
    public Dictionary<TKey, MValue> Values;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        switch (val)
        {
            case JArray jarray:
                Values = [];
                foreach (var jel in jarray)
                {
                    JsonParsePair(ctx, jel);
                }
                return true;

            case JObject jobj:
                if (!IsKeyParseableFromString())
                {
                    ctx.Logger.LogJsonParseError(val.Path, $"dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>", $"{typeof(TKey).Name} isn't parseable from string");
                    return false;
                }

                Values = [];
                foreach (var pair in jobj)
                {
                    if (!TryParseKey(ctx, pair.Key, out TKey key))
                    {
                        ctx.Logger.LogJsonParseError(val.Path, $"\"{pair.Key}\"", $"couldn't be parsed as a {typeof(TKey).Name}");
                        continue;
                    }
                    MValue mel = new();
                    if (mel.JsonParse(ctx, pair.Value))
                    {
                        Values[key] = mel;
                    }
                }
                return true;
        }
        ctx.Logger.LogJsonParseError(val.Path, $"dictionary<{typeof(TKey).Name}, {typeof(TValue).Name}>", "not an object or array");
        return false;
    }

    public bool JsonParsePair(CustomJsonInitializer ctx, JToken val)
    {
        JToken jkey;
        JToken jval;

        switch (val)
        {
            case JArray jarray:
                if (jarray.Count < 2)
                {
                    ctx.Logger.LogJsonParseError(val.Path, $"pair<{typeof(TKey).Name}, {typeof(TValue).Name}>", "insufficient array elements (should be 2)");
                    return false;
                }
                jkey = jarray[0];
                jval = jarray[1];
                break;
            case JObject jobject:
                if (!(jobject.TryGetValue("Key", out jkey) || jobject.TryGetValue("key", out jkey)))
                {
                    ctx.Logger.LogJsonParseError(val.Path, $"pair<{typeof(TKey).Name}, {typeof(TValue).Name}>", "missing key \"key\"");
                    return false;
                }
                if (!(jobject.TryGetValue("Value", out jval) || jobject.TryGetValue("value", out jval)))
                {
                    ctx.Logger.LogJsonParseError(val.Path, $"pair<{typeof(TKey).Name}, {typeof(TValue).Name}>", "missing key \"value\"");
                    return false;
                }
                break;
            default:
                ctx.Logger.LogJsonParseError(val.Path, $"pair<{typeof(TKey).Name}, {typeof(TValue).Name}>", "not an object or array");
                return false;
        }

        if (!TryParseKey(ctx, jkey, out TKey key))
        {
            ctx.Logger.LogJsonParseError(jkey.Path, $"pair<{typeof(TKey).Name}, {typeof(TValue).Name}> key", $"couldn't be parsed as a {typeof(TKey).Name}");
            return false;
        }
        MValue mval = new();
        if (!mval.JsonParse(ctx, jval))
        {
            ctx.Logger.LogJsonParseError(jval.Path, $"pair<{typeof(TKey).Name}, {typeof(TValue).Name}> value", $"couldn't be parsed as a {typeof(TValue).Name}");
        }
        Values[key] = mval;
        return true;
    }

    public override IDictionary<TKey, TValue> Apply(IDictionary<TKey, TValue> list)
    {
        if (Values != null)
        {
            foreach (var pair in Values)
            {
                list[pair.Key] = pair.Value.Apply(list[pair.Key]);
            }
        }
        return list;
    }

    public override bool IsAssignable(Type type)
    {
        if (!(type.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()))) return false;
        var args = type.GetGenericArguments();
        return IsKeyAssignable(args[0])
            && IsValueAssignable(args[1]);
    }

    public bool IsKeyAssignable(Type innerType)
    {
        MKey dummy = new();
        return dummy.IsAssignable(innerType);
    }

    public bool IsValueAssignable(Type innerType)
    {
        MValue dummy = new();
        return dummy.IsAssignable(innerType);
    }

    public bool IsKeyParseableFromString()
    {
        return typeof(IMKey<TKey>).IsAssignableFrom(typeof(MKey));
    }

    public bool TryParseKey(CustomJsonInitializer ctx, JToken jtoken, out TKey val)
    {
        MKey dummy = new();
        if (dummy.JsonParse(ctx, jtoken))
        {
            val = dummy.Apply(default);
            return true;
        }
        val = default;
        return false;
    }

    public bool TryParseKey(CustomJsonInitializer ctx, string key, out TKey val)
    {
        MKey dummy = new();
        if (((IMKey<TKey>)dummy).JsonParseKey(ctx, key)) 
        {
            val = dummy.Apply(default);
            return true;
        }
        val = default;
        return false;
    }
}
