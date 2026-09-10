namespace BopCustomTextures.AccessExtensions;
public abstract class TypedMemberInfo<O, T>: TypedInfo<O>
{
    public abstract T GetValue(O obj);

    public abstract bool SetValue(O obj, T value);
}
