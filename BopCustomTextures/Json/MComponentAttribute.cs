using System;

namespace BopCustomTextures.Json;

/// <summary>
/// Attribute specifying a class is a <see cref="SceneMods.IMComponent"/>. 
/// 
/// Will automatically be registered if <see cref="MComponentParserRegistry.RegisterAssembly(System.Reflection.Assembly)"/>
/// is invoked the assembly containing it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MComponentAttribute(string[] types, float priority = 0) : Attribute
{
    public string[] Names = types;
    public float Priority = priority;

    public MComponentAttribute(string type, float priority = 0) : this([type], priority) { }
}
