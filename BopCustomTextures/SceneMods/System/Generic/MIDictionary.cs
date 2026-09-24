using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System.Generic;

public class MIDictionary<G, MKey, TKey, MValue, TValue> : MObject<G>
    where G : class, IDictionary<TKey, TValue>, new()
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
                    ctx.Logger.LogJsonParseError(val.Path, $"{typeof(G).Name}<{typeof(TKey).Name}, {typeof(TValue).Name}>", $"{typeof(TKey).Name} isn't parseable from string");
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
        ctx.Logger.LogJsonParseError(val.Path, $"{typeof(G).Name}<{typeof(TKey).Name}, {typeof(TValue).Name}>", "not an object or array");
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

    public override G Apply(G list)
    {
        if (Values != null)
        {
            foreach (var pair in Values)
            {
                if (list.TryGetValue(pair.Key, out var val))
                {
                    list[pair.Key] = pair.Value.Apply(val);
                }
                else
                {
                    list[pair.Key] = pair.Value.Apply();
                }
            }
        }
        return list;
    }

    public override G Apply()
    {
        if (Values != null)
        {
            G list = [];
            foreach (var pair in Values)
            {
                list[pair.Key] = pair.Value.Apply();
            }
            return list;
        }
        return null;
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
            val = dummy.Apply();
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
            val = dummy.Apply();
            return true;
        }
        val = default;
        return false;
    }
}
