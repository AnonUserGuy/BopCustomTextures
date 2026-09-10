using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;
public class TypedPropertyInfo<O, T>: TypedMemberInfo<O, T>
{
    public PropertyInfo Property;

    public TypedPropertyInfo(string name)
    {
        Property = AccessTools.Property(typeof(O), name);
        if (Property == null)
        {
            Log($"Unable to find property \"{name}\" in class {typeof(O).FullName} of type {typeof(T).FullName}");
        }
        else if (Property.PropertyType != typeof(T))
        {
            Log($"Found property \"{name}\" in class {typeof(O).FullName}, but is of type {Property.PropertyType.FullName} instead of {typeof(T).FullName}");
            Property = null;
        }
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
