using System.Reflection;

namespace BopCustomTextures.AccessExtensions.TypedInfo;

public abstract class TypedMemberInfo<O, T> : TypedInfo<O>
{
    public abstract bool IsReadOnly { get; }

    public static TypedMemberInfo<O, T> Create(string name)
    {
        var field = typeof(O).GetField(name, BindingFlagsAny);
        if (field != null)
        {
            return new TypedFieldInfo<O, T> { Field = field };
        }

        var property = typeof(O).GetProperty(name, BindingFlagsAny);
        if (property != null)
        {
            return new TypedPropertyInfo<O, T> { Property = property };
        }
        return null;
    }
    public abstract bool Find(string name);
    public abstract T GetValue(O obj);
    public abstract bool SetValue(O obj, T value);
}
