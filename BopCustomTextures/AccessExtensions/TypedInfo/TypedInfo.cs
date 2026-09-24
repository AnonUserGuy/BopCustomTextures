using System;
using System.Reflection;

namespace BopCustomTextures.AccessExtensions.TypedInfo;

public abstract class TypedInfo<T>
{
    public const BindingFlags BindingFlagsAny = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public abstract bool Exists();

    public abstract Type Type { get; }
}
