using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace BopCustomTextures.AccessExtensions.TypedInfo;

public abstract class TypedInfo<T>
{
    public const BindingFlags BindingFlagsAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.GetProperty | BindingFlags.SetProperty;

    public abstract bool Exists();

    public abstract Type Type { get; }

    public static FieldInfo GetFieldInfo(string name)
    {
        return AccessTools.FindIncludingBaseTypes(typeof(T), t => t.GetField(name, BindingFlagsAny));
    }

    public static PropertyInfo GetPropertyInfo(string name)
    {
        return AccessTools.FindIncludingBaseTypes(typeof(T), t => t.GetProperty(name, BindingFlagsAny));
    }

    public static IEnumerable<MethodInfo> GetMethodInfos(string name)
    {
        for (Type type = typeof(T); type != null; type = type.BaseType)
        {
            var memberInfos = type.GetMember(name, MemberTypes.Method, BindingFlagsAny);

            foreach (var memberInfo in memberInfos)
            {
                yield return (MethodInfo)memberInfo;
            }
        }
    }

    public static MethodInfo GetMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        ParameterModifier[] modifiers = [];
        MethodInfo methodInfo = null;
        if (parameters == null)
        {
            try
            {
                methodInfo = AccessTools.FindIncludingBaseTypes(typeof(T), t => t.GetMethod(name, BindingFlagsAny));
            }
            catch (AmbiguousMatchException inner)
            {
                methodInfo = AccessTools.FindIncludingBaseTypes(typeof(T), t => t.GetMethod(name, BindingFlagsAny, null, [], modifiers));
                if (methodInfo == null)
                {
                    throw inner;
                }
            }
        }
        else
        {
            methodInfo = AccessTools.FindIncludingBaseTypes(typeof(T), t => t.GetMethod(name, BindingFlagsAny, null, parameters, modifiers));
        }

        if (methodInfo == null) return null;

        if (generics != null)
        {
            methodInfo = methodInfo.MakeGenericMethod(generics);
        }

        return methodInfo;
    }
}
