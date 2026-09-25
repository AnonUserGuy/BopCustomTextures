#if false
// not done
using System;

namespace BopCustomTextures.Json;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MTypeAttribute(Type[] types, float priority = 0) : Attribute
{
    public Type[] Types = types;
    public float Priority = priority;

    public MTypeAttribute(Type type, float priority = 0) : this([type], priority) { }
}
#endif