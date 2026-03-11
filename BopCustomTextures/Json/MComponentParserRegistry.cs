using BopCustomTextures.Logging;
using BopCustomTextures.SceneMods;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace BopCustomTextures.Json;

/// <summary>
/// Manages deserializing of scene mod component definitions through registered deserializer functions.
/// </summary>
public class MComponentParserRegistry(ILogger logger)
{
    private static MComponentParserRegistry instance;
    private readonly ILogger logger = logger;
    private readonly Dictionary<string, JsonParse> registry = [];

    public static MComponentParserRegistry Instance { get => instance; }

    public static void Initialize(ILogger logger)
    {
        if (instance != null)
        {
            logger.LogError("Already instance of MComponentParserRegistry");
            return;
        }
        instance = new MComponentParserRegistry(logger);
        instance.RegisterAssembly(Assembly.GetExecutingAssembly());
        logger.LogMComponentRegistering("MComponent registry successfully initialized");
    }

    /// <summary>
    /// A function to use to deserialize a scene mod component.
    /// </summary>
    /// <param name="ctx">The invoking <see cref="CustomJsonInitializer"/>, for logging and general parsing methods.</param>
    /// <param name="jcomponent">JSON component definition.</param>
    /// <param name="mcomponent">Scene mod <see cref="IMComponent"/> outputted by parse.</param>
    /// <returns><see langword="true"/> if parse successful, <see langword="false"/> otherwise.</returns>
    public delegate bool JsonParse(CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent);

    /// <summary>
    /// Register an arbitrary parsing method for an <see cref="IMComponent"/>.
    /// </summary>
    /// <param name="name">String name of component as used in JSON.</param>
    /// <param name="parser">Parsing method. Takes a <see cref="CustomJsonInitializer"/> for logging and general parsing functions, 
    ///  and a <see cref="JObject"/> containing a JSON component definition. Outputs an <see cref="IMComponent"/> via out parameter, 
    ///  and returns <see langword="true"/>/<see langword="false"/> indicating parsing success.</param>
    public void Register(string name, JsonParse parser)
    {
        if (name.StartsWith("!"))
        {
            name = name.Substring(1);
        }
        if (registry.ContainsKey(name))
        {
            logger.LogMComponentRegistering($"MComponent \"{name}\" already registered");
        }
        registry[name] = parser;
        logger.LogMComponentRegistering($"Successfully registered MComponent: {name}");
    }

    /// <summary>
    /// Register an <see cref="IMComponent"/>.
    /// </summary>
    /// <param name="name">String name of component as used in JSON.</param>
    public void Register<T>(string name) where T: IMComponent, new()
    {
        Register(name, (CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent) =>
        {
            mcomponent = new T();
            return mcomponent.JsonParse(ctx, jcomponent);
        });
    }

    /// <summary>
    /// Register an <see cref="IMComponent"/>.
    /// </summary>
    /// <param name="name">String name of component as used in JSON.</param>
    /// <param name="type">Type of scene mod component that will be parsed to. Must implement <see cref="IMComponent"/>.</param>
    public void Register(string name, Type type)
    {
        if (!typeof(IMComponent).IsAssignableFrom(type))
        {
            logger.LogError($"Failed to register invalid MComponent \"{name}\": does not implement IMComponent");
            return;
        }
        if (type.IsAbstract)
        {
            logger.LogError($"Failed to register invalid MComponent \"{name}\": is an abstract class");
            return;
        }
        var ctor = CreateFactory(type);
        Register(name, (CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent) =>
        {
            mcomponent = ctor();
            return mcomponent.JsonParse(ctx, jcomponent);
        });
    }

    private static Func<IMComponent> CreateFactory(Type type)
    {
        var newExpr = Expression.New(type);
        var castExpr = Expression.Convert(newExpr, typeof(IMComponent));
        return Expression.Lambda<Func<IMComponent>>(castExpr).Compile();
    }

    /// <summary>
    /// Register all <see cref="IMComponent"/>s in an assembly that have the <see cref="MComponentAttribute"/>.
    /// </summary>
    /// <param name="assembly">Assembly to scan for classes with <see cref="MComponentAttribute"/>.</param>
    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<MComponentAttribute>();
            if (attr == null)
                continue;
            Register(attr.Name, type);
        }
        logger.LogMComponentRegistering($"Registered all MComponents in assembly: {assembly.FullName}");
    }

    public bool TryParse(CustomJsonInitializer ctx, string name, JToken jcomponent, out IMComponent mcomponent)
    {
        if (registry.TryGetValue(name, out var parse))
        {
            return parse(ctx, jcomponent, out mcomponent);
        }
        mcomponent = default;
        return false;
    }
}
