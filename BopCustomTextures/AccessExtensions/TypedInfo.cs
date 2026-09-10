namespace BopCustomTextures.AccessExtensions;
public abstract class TypedInfo<T>
{
    public abstract bool Exists();

    protected void Log(object data)
    {
        if (BopCustomTexturesPlugin.Logger != null)
        {
            BopCustomTexturesPlugin.Logger.LogWarning(data);
        }
    }
}
