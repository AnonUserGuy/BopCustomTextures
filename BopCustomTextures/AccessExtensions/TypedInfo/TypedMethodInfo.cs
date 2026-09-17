using HarmonyLib;
using System;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions.TypedInfo;
public class TypedMethodInfo<O, T> : TypedInfo<O>
{
    public MethodInfo Method;

    public TypedMethodInfo() { }

    public TypedMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        Find(name, parameters, generics);
    }

    public bool Find(string name, Type[] parameters = null, Type[] generics = null)
    {
        Method = AccessTools.Method(typeof(O), name, parameters, generics);
        if (Method == null)
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find method \"{name}\" in class {typeof(O).Name} with return type {typeof(T).Name}");
            return false;
        }
        else if (Method.ReturnType != typeof(T))
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find method \"{name}\" in class {typeof(O).Name}, but has return type {Method.ReturnType.Name} instead of {typeof(T).Name}");
            Method = null;
            return false;
        }
        return true;
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

public class TypedMethodInfo<O> : TypedInfo<O>
{
    public MethodInfo Method;

    public TypedMethodInfo() { }

    public TypedMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        Find(name, parameters, generics);
    }

    public bool Find(string name, Type[] parameters = null, Type[] generics = null)
    {
        Method = AccessTools.Method(typeof(O), name, parameters, generics);
        if (Method == null)
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find method \"{name}\" in class {typeof(O).Name} with return type {typeof(void).Name}");
            return false;
        }
        else if (Method.ReturnType != typeof(void))
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find method \"{name}\" in class {typeof(O).Name}, but has return type {Method.ReturnType.Name} instead of {typeof(void).Name}");
            Method = null;
            return false;
        }
        return true;
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