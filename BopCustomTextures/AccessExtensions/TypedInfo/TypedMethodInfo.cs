using System;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions.TypedInfo;

public class TypedMethodInfo<O> : TypedInfo<O>
{
    public MethodInfo Method;

    public override Type Type => Method?.ReturnType;

    public TypedMethodInfo() { }

    public TypedMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        Find(name, parameters, generics);
    }

    public virtual bool Find(string name, Type[] parameters = null, Type[] generics = null)
    {
        Method = GetMethodInfo(name, parameters, generics);
        if (Method == null)
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find method \"{name}\" in class {typeof(O).Name} with return type {typeof(void).Name}");
            return false;
        }
        else if (Method.ReturnType != typeof(void))
        {
            BopCustomTexturesPlugin.LogWarning($"Found method \"{name}\" in class {typeof(O).Name}, but has return type {Method.ReturnType.Name} instead of {typeof(void).Name}");
            Method = null;
            return false;
        }
        return true;
    }

    public override bool Exists()
    {
        return Method != null;
    }

    public void Invoke(O obj, object[] parameters = null)
    {
        if (!Exists())
        {
            return;
        }
        Method.Invoke(obj, parameters);
    }
}

public class TypedMethodInfo<O, T> : TypedMethodInfo<O>
{
    public TypedMethodInfo() { }

    public TypedMethodInfo(string name, Type[] parameters = null, Type[] generics = null)
    {
        Find(name, parameters, generics);
    }

    public override bool Find(string name, Type[] parameters = null, Type[] generics = null)
    {
        Method = GetMethodInfo(name, parameters, generics);
        if (Method == null)
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find method \"{name}\" in class {typeof(O).Name} with return type {typeof(T).Name}");
            return false;
        }
        else if (!typeof(T).IsAssignableFrom(Method.ReturnType))
        {
            BopCustomTexturesPlugin.LogWarning($"Found method \"{name}\" in class {typeof(O).Name}, but has return type {Method.ReturnType.Name} instead of {typeof(T).Name}");
            Method = null;
            return false;
        }
        return true;
    }

    new public T Invoke(O obj, object[] parameters = null)
    {
        if (!Exists())
        {
            return default;
        }
        return (T)Method.Invoke(obj, parameters);
    }
}