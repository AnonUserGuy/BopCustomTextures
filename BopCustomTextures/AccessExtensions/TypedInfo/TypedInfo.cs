using System;

namespace BopCustomTextures.AccessExtensions.TypedInfo;
public abstract class TypedInfo<T>
{
    public abstract bool Exists();

    public abstract Type Type { get; }
}
