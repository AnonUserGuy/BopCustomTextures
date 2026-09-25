using BopCustomTextures.Json;
using BopCustomTextures.AccessExtensions.TypedInfo;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod definition for class/reference types.
/// </summary>
/// <typeparam name="T">Target class/reference type</typeparam>
public class MObject<T> : MBase<T> where T : class
{
    public T Ref;
    public bool HasRef = false;

    public Dictionary<TypedMemberInfo<T, object>, IMBase> DynamicMembers;

    public Dictionary<TypedMethodInfo<T>, object[]> DynamicMethods;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        if (val.Type == JTokenType.Null)
        {
            Ref = null;
            HasRef = true;
            return true;
        }

        if (val is not JObject jobj)
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

    public virtual void JsonParsePair(CustomJsonInitializer ctx, string key, JToken jtoken)
    {
        var member = TypedMemberInfo<T, object>.Create(key);
        if (member != null)
        {
            if (TryJsonParse(ctx, member.Type, jtoken, out var mod))
            {
                DynamicMembers ??= [];
                DynamicMembers[member] = mod;
            }
            return;
        }

        object[] args;
        TypedMethodInfo<T> method;
        if (jtoken is JArray jarray)
        {
            args = new object[jarray.Count];
            method = TypedMethodInfo<T>.CreateWithArgumentPredicate(key, delegate (Type[] types) {
                if (types.Length != args.Length)
                {
                    return false;
                }
                for (int i = 0; i < types.Length; i++)
                {
                    if (!TryJsonParse(ctx, types[i], jarray[i], out var res))
                    {
                        return false;
                    }
                    args[i] = res.ApplyNone();
                }
                return true;
            });
        } 
        else
        {
            args = new object[1];
            method = TypedMethodInfo<T>.CreateWithArgumentPredicate(key, delegate (Type[] types) {
                if (types.Length != args.Length)
                {
                    return false;
                }
                for (int i = 0; i < types.Length; i++)
                {
                    if (!TryJsonParse(ctx, types[i], jtoken, out var res))
                    {
                        return false;
                    }
                    args[i] = res.ApplyNone();
                }
                return true;
            });
        }

        if (method != null)
        {
            DynamicMethods ??= [];
            DynamicMethods[method] = args;
            return;
        }

        ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T).Name, $"key \"{key}\" couldn't be found in class {typeof(T).Name}");
    }

    new public virtual bool TryJsonParse(CustomJsonInitializer ctx, Type type, JToken jtoken, out IMBase res)
    {
        return MBase.TryJsonParse(ctx, type, jtoken, out res);
    }

    public override T Apply(T obj)
    {
        if (HasRef)
        {
            obj = Ref;
        }
        if (obj != null)
        {
            obj = ApplyInternal(obj);
        }
        return obj;
    }

    public virtual T ApplyInternal(T obj)
    {
        if (DynamicMembers != null)
        {
            foreach (var pair in DynamicMembers)
            {
                var member = pair.Key;
                var mod = pair.Value;

                if (member.IsReadOnly)
                {
                    mod.ApplyReadOnly(member.GetValue(obj));
                }
                else
                {
                    member.SetValue(obj, mod.Apply(member.GetValue(obj)));
                }
            }
        }
        if (DynamicMethods != null)
        {
            foreach (var pair in DynamicMethods)
            {
                var method = pair.Key;
                var args = pair.Value;

                method.Invoke(obj, args);
            }
        }
        return obj;
    }

    public override T Apply()
    {
        return Apply(null);
    }
}
