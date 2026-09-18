using BopCustomTextures.AccessExtensions.TypedInfo;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.Base;
public class MObject<T>: MBase<T>
{
    public Dictionary<TypedMemberInfo<T, object>, MBase> DynamicMembers;

    public static bool TryJsonParse(JToken val, out MObject<T> res)
    {
        JObject jobj = (JObject)val;
        if (jobj == null)
        {
            res = null;
            return false;
        }

        res = new();
        foreach (var pair in jobj)
        {
            res.JsonParsePair(pair.Key, pair.Value);
        }
        return true;
    }

    public virtual void JsonParsePair(string key, JToken val)
    {
        var member = TypedMemberInfo<T, object>.Create(key);
        if (member == null || !MBase.TryJsonParse(member.Type, val, out var mod))
        {
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
        foreach (var pair in DynamicMembers)
        {
            var member = pair.Key;
            var mod = pair.Value;
            member.SetValue(obj, mod.Apply(member.GetValue(obj)));
        }
        return obj;
    }
}
