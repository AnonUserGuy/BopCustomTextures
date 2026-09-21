using BopCustomTextures.Json;
using BopCustomTextures.AccessExtensions.TypedInfo;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System;

public class MObject<T>: MBase<T> where T: class
{
    public Dictionary<TypedMemberInfo<T, object>, IMBase> DynamicMembers;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        JObject jobj = (JObject)val;
        if (jobj == null)
        {
            ctx.Logger.LogJsonParseError(val.Path, typeof(T).Name, "not an object");
            return false;
        }

        foreach (var pair in jobj)
        {
            JsonParsePair(ctx, pair.Key, pair.Value);
        }
        return true;
    }

    public virtual void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        var member = TypedMemberInfo<T, object>.Create(key);
        if (member == null || !TryJsonParse(ctx, member.Type, val, out var mod))
        {
            ctx.Logger.LogJsonParseError(val.Path, typeof(T).Name, $"key \"{key}\" couldn't be found in class {typeof(T).Name}");
            return;
        }
        if (DynamicMembers == null)
        {
            DynamicMembers = [];
        }
        DynamicMembers[member] = mod;
    }

    public override T Apply(T obj)
    {
        if (DynamicMembers != null)
        {
            foreach (var pair in DynamicMembers)
            {
                var member = pair.Key;
                var mod = pair.Value;
                member.SetValue(obj, mod.Apply(member.GetValue(obj)));
            }
        }
        return obj;
    }
}
