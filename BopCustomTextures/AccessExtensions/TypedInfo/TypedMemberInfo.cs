namespace BopCustomTextures.AccessExtensions.TypedInfo;
public abstract class TypedMemberInfo<O, T> : TypedInfo<O>
{
    public abstract bool Find(string name);
    public abstract T GetValue(O obj);
    public abstract bool SetValue(O obj, T value);
}
