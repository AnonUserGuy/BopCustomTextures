using System;

namespace BopCustomTextures.Json;

/// <summary>
/// Attribute indicating Scene mod type shouldn't be automatically registered for
/// scene mod definition resolution when <see cref="MComponentParserRegistry.RegisterAssembly"/> is called on the containing assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NotMTypeAttribute: Attribute;