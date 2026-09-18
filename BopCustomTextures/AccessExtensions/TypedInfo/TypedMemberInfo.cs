using HarmonyLib;

namespace BopCustomTextures.AccessExtensions.TypedInfo;
public abstract class TypedMemberInfo<O, T> : TypedInfo<O>
{
    public static TypedMemberInfo<O, T> Create(string name)
    {
        var field = AccessTools.Field(typeof(O), name);
        if (field != null)
        {
            if (!typeof(T).IsAssignableFrom(field.FieldType))
            {
                BopCustomTexturesPlugin.LogWarning($"Found field \"{name}\" in class {typeof(O).Name}, but is of type {field.FieldType.Name} instead of {typeof(T).Name}");
                return null;
            }
            return new TypedFieldInfo<O, T>
            {
                Field = field
            };
        }

        var property = AccessTools.Property(typeof(O), name);
        if (property != null)
        {
            if (!typeof(T).IsAssignableFrom(property.PropertyType))
            {
                BopCustomTexturesPlugin.LogWarning($"Found property \"{name}\" in class {typeof(O).Name}, but is of type {property.PropertyType.Name} instead of {typeof(T).Name}");
                return null;
            }
            return new TypedPropertyInfo<O, T>
            {
                Property = property
            };
        }

        BopCustomTexturesPlugin.LogWarning($"Unable to find field or property \"{name}\" in class {typeof(O).Name} of type {typeof(T).Name}");
        return null;
    }
    public abstract bool Find(string name);
    public abstract T GetValue(O obj);
    public abstract bool SetValue(O obj, T value);
}
