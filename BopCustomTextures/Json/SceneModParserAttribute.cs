using System;

namespace BopCustomTextures.Json;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class SceneModParserAttribute: Attribute
{
    public Type[] Types;
    public float Priority = 0;

    public SceneModParserAttribute(Type type)
    {
        Types = [type];
    }

    public SceneModParserAttribute(Type type, float priority)
    {
        Types = [type];
        Priority = priority;
    }

    public SceneModParserAttribute(Type[] types)
    {
        Types = types;
    }

    public SceneModParserAttribute(Type[] types, float priority)
    {
        Types = types;
        Priority = priority;
    }
}
