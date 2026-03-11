using System;

namespace BopCustomTextures.Json;

/// <summary>
/// Attribute specifying a class is a <see cref="SceneMods.IMComponent"/>. 
/// 
/// Will automatically be registered if <see cref="MComponentParserRegistry.RegisterAssembly(System.Reflection.Assembly)"/>
/// is invoked the assembly containing it.
/// </summary>
/// <param name="name">Name of component in JSON.</param>
[AttributeUsage(AttributeTargets.Class)]
public class MComponentAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
