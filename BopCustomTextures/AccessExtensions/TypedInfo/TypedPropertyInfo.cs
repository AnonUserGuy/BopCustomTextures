using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions.TypedInfo;
public class TypedPropertyInfo<O, T> : TypedMemberInfo<O, T>
{
    public PropertyInfo Property;

    public TypedPropertyInfo() { }

    public TypedPropertyInfo(string name)
    {
        Find(name);
    }

    public override bool Find(string name)
    {
        Property = AccessTools.Property(typeof(O), name);
        if (Property == null)
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find property \"{name}\" in class {typeof(O).Name} of type {typeof(T).Name}");
            return false;
        }
        else if (Property.PropertyType != typeof(T))
        {
            BopCustomTexturesPlugin.LogWarning($"Found property \"{name}\" in class {typeof(O).Name}, but is of type {Property.PropertyType.Name} instead of {typeof(T).Name}");
            Property = null;
            return false;
        }
        return true;
    }

    public override bool Exists()
    {
        return Property != null;
    }

    public override T GetValue(O obj)
    {
        if (!Exists())
        {
            return default;
        }
        return (T)Property.GetValue(obj);
    }

    public override bool SetValue(O obj, T value)
    {
        if (!Exists())
        {
            return false;
        }
        Property.SetValue(obj, value);
        return true;
    }
}
