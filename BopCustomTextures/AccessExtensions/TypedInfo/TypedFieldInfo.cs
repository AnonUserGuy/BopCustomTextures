using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions.TypedInfo;
public class TypedFieldInfo<O, T> : TypedMemberInfo<O, T>
{
    public FieldInfo Field;

    public TypedFieldInfo() { }

    public TypedFieldInfo(string name)
    {
        Find(name);
    }

    public override bool Find(string name)
    {
        Field = AccessTools.Field(typeof(O), name);
        if (Field == null)
        {
            BopCustomTexturesPlugin.LogWarning($"Unable to find field \"{name}\" in class {typeof(O).Name} of type {typeof(T).Name}");
            return false;
        }
        else if (Field.FieldType != typeof(T))
        {
            BopCustomTexturesPlugin.LogWarning($"Found field \"{name}\" in class {typeof(O).Name}, but is of type {Field.FieldType.Name} instead of {typeof(T).Name}");
            Field = null;
            return false;
        }
        return true;
    }

    public override bool Exists()
    {
        return Field != null;
    }

    public override T GetValue(O obj)
    {
        if (!Exists())
        {
            return default;
        }
        return (T)Field.GetValue(obj);
    }

    public override bool SetValue(O obj, T value)
    {
        if (!Exists())
        {
            return false;
        }
        Field.SetValue(obj, value);
        return true;
    }
}
