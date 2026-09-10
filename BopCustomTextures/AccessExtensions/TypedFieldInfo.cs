using HarmonyLib;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions;
public class TypedFieldInfo<O, T>: TypedMemberInfo<O, T>
{
    public FieldInfo Field;

    public TypedFieldInfo(string name)
    {
        Field = AccessTools.Field(typeof(O), name);
        if (Field == null)
        {
            Log($"Unable to find field \"{name}\" in class {typeof(O).FullName} of type {typeof(T).FullName}");
        }
        else if (Field.FieldType != typeof(T))
        {
            Log($"Found field \"{name}\" in class {typeof(O).FullName}, but is of type {Field.FieldType.FullName} instead of {typeof(T).FullName}");
            Field = null;
        }
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
