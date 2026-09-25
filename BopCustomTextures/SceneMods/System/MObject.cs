using BopCustomTextures.Json;
using BopCustomTextures.AccessExtensions.TypedInfo;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.System;

/// <summary>
/// Scene mod definition for class/reference types.
/// </summary>
/// <typeparam name="T">Target class/reference type</typeparam>
public class MObject<T> : MBase<T> where T : class
{
    private interface IDynamicMember
    {
        public void Apply(T obj);
    }

    private class DynamicField(FieldInfo field, IMBase mod) : IDynamicMember
    {
        private readonly FieldInfo Field = field;
        private readonly IMBase Mod = mod;

        public void Apply(T obj) => Field.SetValue(obj, Mod.Apply(Field.GetValue(obj)));
    }

    private class DynamicProperty(PropertyInfo property, IMBase mod) : IDynamicMember
    {
        private readonly PropertyInfo Property = property;
        private readonly IMBase Mod = mod;

        public void Apply(T obj) => Property.SetValue(obj, Mod.Apply(Property.GetValue(obj)));
    }

    private class DynamicFieldReadOnly(FieldInfo field, IMBase mod) : IDynamicMember
    {
        private readonly FieldInfo Field = field;
        private readonly IMBase Mod = mod;

        public void Apply(T obj) => Mod.Apply(Field.GetValue(obj));
    }

    private class DynamicPropertyReadOnly(PropertyInfo property, IMBase mod) : IDynamicMember
    {
        private readonly PropertyInfo Property = property;
        private readonly IMBase Mod = mod;

        public void Apply(T obj) => Mod.Apply(Property.GetValue(obj));
    }

    private class DynamicMethod(MethodInfo method, object[] args = null) : IDynamicMember
    {
        private readonly MethodInfo Member = method;
        private readonly object[] Args = args;

        public void Apply(T obj) => Member.Invoke(obj, Args);
    }


    public T Ref;
    public bool HasRef = false;

    private bool FindDynamicMembers;
    private List<IDynamicMember> DynamicMembers;

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        FindDynamicMembers = ctx.Mixtape.Unsafe;

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

        if (FindDynamicMembers && (jobj.TryGetValue("members", out var jfields) || jobj.TryGetValue("Members", out jfields)))
        {
            FindDynamicMembers = false;
            JsonParseDynamicMembers(ctx, jfields);
        }

        foreach (var pair in jobj)
        {
            JsonParsePair(ctx, pair.Key, pair.Value);
        }
        return true;
    }

    private void JsonParseDynamicMembers(CustomJsonInitializer ctx, JToken jfields)
    {
        if (jfields is JArray jfieldsArray)
        {
            foreach (var jfield in jfieldsArray)
            {
                if (jfield is JArray jfieldArray)
                {
                    if (jfieldArray.Count < 1 || jfieldArray[0].Type != JTokenType.String)
                    {
                        ctx.Logger.LogJsonParseError(jfield.Path, typeof(T).Name, "needs at least first element, which is string name of member");
                    }
                    else if (jfieldArray.Count == 2)
                    {
                        JsonParseDynamicMember(ctx, (string)jfieldArray[0], jfieldArray[1]);
                    }
                    else
                    {
                        JsonParseDynamicMethod(ctx, (string)jfieldArray[0], jfieldArray, 1);
                    }
                }
                else if (jfield is JObject jfieldObj)
                {
                    foreach (var pair in jfieldObj)
                    {
                        JsonParseDynamicMember(ctx, pair.Key, pair.Value);
                    }
                }
                else if (jfield.Type == JTokenType.String)
                {
                    JsonParseDynamicMethod(ctx, (string)jfield);

                }
                else
                {
                    ctx.Logger.LogJsonParseError(jfield.Path, typeof(T).Name, "not an object, array, or string");
                }
            }
        }
        else if (jfields is JObject jfieldsObj)
        {
            foreach (var pair in jfieldsObj)
            {
                JsonParseDynamicMember(ctx, pair.Key, pair.Value);
            }
        }
        else if (jfields.Type != JTokenType.Null)
        {
            ctx.Logger.LogJsonParseError(jfields.Path, typeof(T).Name, "not an object, array, or null");
        }
    }

    private void JsonParseDynamicMember(CustomJsonInitializer ctx, string key, JToken jtoken)
    {
        var field = TypedInfo<T>.GetFieldInfo(key);
        if (field != null)
        {
            if (TryJsonParse(ctx, field.FieldType, jtoken, out var mod))
            {
                DynamicMembers ??= [];
                if (field.IsInitOnly)
                {
                    DynamicMembers.Add(new DynamicFieldReadOnly(field, mod));
                } 
                else
                {
                    DynamicMembers.Add(new DynamicField(field, mod));
                }
            }
            return;
        }

        var property = TypedInfo<T>.GetPropertyInfo(key);
        if (property != null)
        {
            if (TryJsonParse(ctx, property.PropertyType, jtoken, out var mod))
            {
                DynamicMembers ??= [];
                if (!property.CanWrite)
                {
                    DynamicMembers.Add(new DynamicPropertyReadOnly(property, mod));
                }
                else
                {
                    DynamicMembers.Add(new DynamicProperty(property, mod));
                }
            }
            return;
        }

        JsonParseDynamicMethod(ctx, key, jtoken);
    }

    private void JsonParseDynamicMethod(CustomJsonInitializer ctx, string key)
    {
        var method = GetMethodInfo(key);

        if (method != null)
        {
            DynamicMembers ??= [];
            DynamicMembers.Add(new DynamicMethod(method));
            return;
        }

        ctx.Logger.LogJsonParseError(key, typeof(T).Name, $"key \"{key}\" couldn't be found in class {typeof(T).Name}");
    }

    private void JsonParseDynamicMethod(CustomJsonInitializer ctx, string key, JArray jarray, int offset = 0)
    {
        if (jarray.Count <= offset)
        {
            JsonParseDynamicMethod(ctx, key);
            return;
        }
        else if (IsArrayOfArrays(jarray, offset))
        {
            var success = false;
            for (var i = offset; i < jarray.Count; i++)
            {
                var jarray0 = (JArray)jarray[i];

                object[] args = new object[jarray0.Count];
                var method = GetMethodInfo(key, args, (Type type, int i, out IMBase res)
                    => TryJsonParse(ctx, type, jarray0[i], out res));

                if (method != null)
                {
                    DynamicMembers ??= [];
                    DynamicMembers.Add(new DynamicMethod(method, args));
                    success = true;
                }
            }
            if (success) return;
        }
        else
        {
            object[] args = new object[jarray.Count - offset];
            var method = GetMethodInfo(key, args, (Type type, int i, out IMBase res)
                => TryJsonParse(ctx, type, jarray[i + offset], out res));

            if (method != null)
            {
                DynamicMembers ??= [];
                DynamicMembers.Add(new DynamicMethod(method, args));
                return;
            }
        }
        ctx.Logger.LogJsonParseError(jarray.Path, typeof(T).Name, $"key \"{key}\" couldn't be found in class {typeof(T).Name}");
    }

    private void JsonParseDynamicMethod(CustomJsonInitializer ctx, string key, JObject jobj)
    {
        bool success = false;
        foreach (var pair in jobj)
        {
            object[] args;
            MethodInfo method;

            var keyArg = pair.Key;
            if (pair.Value is JArray jarray)
            {
                args = new object[jarray.Count + 1];
                method = GetMethodInfo(key, args, (Type type, int i, out IMBase res)
                    => i == 0 ? TryJsonParseKey(ctx, type, keyArg, out res) : TryJsonParse(ctx, type, jarray[i - 1], out res));
            }
            else
            {
                var jtoken = pair.Value;
                args = new object[2];
                method = GetMethodInfo(key, args, (Type type, int i, out IMBase res)
                    => i == 0 ? TryJsonParseKey(ctx, type, keyArg, out res) : TryJsonParse(ctx, type, jtoken, out res));
            }

            if (method != null)
            {
                DynamicMembers ??= [];
                DynamicMembers.Add(new DynamicMethod(method, args));
                success = true;
            }
        }
        if (success) return;

        ctx.Logger.LogJsonParseError(jobj.Path, typeof(T).Name, $"key \"{key}\" couldn't be found in class {typeof(T).Name}");
    }

    private void JsonParseDynamicMethod(CustomJsonInitializer ctx, string key, JToken jtoken)
    {
        if (jtoken == null)
        {
            JsonParseDynamicMethod(ctx, key);
            return;
        }
        else if (jtoken is JArray jarray)
        {
            JsonParseDynamicMethod(ctx, key, jarray);
            return;
        }
        else if (jtoken is JObject jobj)
        {
            JsonParseDynamicMethod(ctx, key, jobj);
            return;
        }

        object[] args = new object[1];
        var method = GetMethodInfo(key, args, (Type type, int i, out IMBase res)
            => TryJsonParse(ctx, type, jtoken, out res));

        if (method != null)
        {
            DynamicMembers ??= [];
            DynamicMembers.Add(new DynamicMethod(method, args));
            return;
        }

        ctx.Logger.LogJsonParseError(jtoken.Path, typeof(T).Name, $"key \"{key}\" couldn't be found in class {typeof(T).Name}");
    }

    private delegate bool TryJsonParseDelegate(Type type, int index, out IMBase val);

    private static MethodInfo GetMethodInfo(string key)
    {
        var methodInfos = TypedInfo<T>.GetMethodInfos(key);
        foreach (var methodInfo in methodInfos)
        {
            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != 0)
            {
                continue;
            }
            return methodInfo;
        }
        return null;
    }

    private static MethodInfo GetMethodInfo(string key, object[] args, TryJsonParseDelegate tryJsonParse)
    {
        var methodInfos = TypedInfo<T>.GetMethodInfos(key);
        foreach (var methodInfo in methodInfos)
        {
            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != args.Length)
            {
                continue;
            }
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                if (!tryJsonParse(parameterInfos[i].ParameterType, i, out var mod))
                {
                    continue;
                }
                args[i] = mod.ApplyNone();
            }
            return methodInfo;
        }
        return null;
    }

    private static bool IsArrayOfArrays(JArray jarray, int offset = 0)
    {
        for (var i = offset; i < jarray.Count; i++)
        {
            var jtoken = jarray[i];
            if (jtoken.Type != JTokenType.Array) return false;
        }
        return true;
    }

    public virtual void JsonParsePair(CustomJsonInitializer ctx, string key, JToken jtoken)
    {
        if (FindDynamicMembers && !KeyMatch("Members", key))
        {
            JsonParseDynamicMember(ctx, key, jtoken);
        }
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
            foreach (var dynamicMember in DynamicMembers)
            {
                dynamicMember.Apply(obj);
            }
        }
        return obj;
    }

    public override T Apply()
    {
        return Apply(null);
    }

    /// <summary>
    /// Compare two strings. Case-insensitive for the first character, case-sensitive for the rest.
    /// </summary>
    /// <returns><see langword="true"/> if the strings are equal per these metrics, <see langword="false"/> otherwise.</returns>
    public static bool KeyMatch(string key1, string key2)
    {
        if (string.IsNullOrEmpty(key1) || string.IsNullOrEmpty(key2))
            return key1 == key2;

        if (key1.Length != key2.Length)
            return false;

        if (char.ToLowerInvariant(key1[0]) != char.ToLowerInvariant(key2[0]))
            return false;

        return string.Compare(key1, 1, key2, 1, key1.Length - 1, StringComparison.Ordinal) == 0;
    }
}
