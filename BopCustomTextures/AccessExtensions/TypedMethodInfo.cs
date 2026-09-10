using HarmonyLib;
using System;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;
public class TypedMethodInfo<O, T>: TypedInfo<O>
{
    public MethodInfo Method;

    public TypedMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        Method = AccessTools.Method(typeof(O), name, parameters, generics);
        if (Method == null)
        {
            Log($"Unable to find method \"{name}\" in class {typeof(O).FullName} with return type {typeof(T).FullName}");
        }
        else if (Method.ReturnType != typeof(T))
        {
            Log($"Unable to find method \"{name}\" in class {typeof(O).FullName}, but has return type {Method.ReturnType.FullName} instead of {typeof(T).FullName}");
            Method = null;
        }
    }

    public override bool Exists()
    {
        return Method != null;
    }

    public T Invoke(O obj)
    {
        return Invoke(obj, []);
    }
    public T Invoke(O obj, object[] parameters)
    {
        if (!Exists())
        {
            return default;
        }
        return (T)Method.Invoke(obj, parameters);
    }
}

public class TypedMethodInfo<O>: TypedInfo<O>
{
    public MethodInfo Method;

    public TypedMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        Method = AccessTools.Method(typeof(O), name, parameters, generics);
        if (Method == null)
        {
            Log($"Unable to find method \"{name}\" in class {typeof(O).FullName} with return type {typeof(void).FullName}");
        }
        else if (Method.ReturnType != typeof(void))
        {
            Log($"Unable to find method \"{name}\" in class {typeof(O).FullName}, but has return type {Method.ReturnType.FullName} instead of {typeof(void).FullName}");
            Method = null;
        }
    }

    public override bool Exists()
    {
        return Method != null;
    }

    public void Invoke(O obj)
    {
        Invoke(obj, []);
    }
    public void Invoke(O obj, object[] parameters)
    {
        if (!Exists())
        {
            return;
        }
        Method.Invoke(obj, parameters);
    }
}