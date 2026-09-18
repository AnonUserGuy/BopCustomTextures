using BopCustomTextures.Logging;
using BopCustomTextures.SceneMods.Unity;
using BopCustomTextures.SceneMods.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace BopCustomTextures.Json;

/// <summary>
/// Manages deserializing of scene mod component definitions through registered deserializer functions.
/// </summary>
public class SceneModParserRegistry(ILogger logger)
{
    private static SceneModParserRegistry instance;
    private readonly ILogger Logger = logger;

    public delegate bool JsonParser(Type type, JToken val, out MBase res);
    private readonly Dictionary<Type, JsonParser> Registry = [];
    private readonly Dictionary<Type, float> RegistryPriority = [];

    public static SceneModParserRegistry Instance { get => instance; }

    public static void Initialize(ILogger logger)
    {
        if (instance != null)
        {
            logger.LogError("Already instance of MComponentParserRegistry");
            return;
        }
        instance = new SceneModParserRegistry(logger);
        instance.RegisterAssembly(Assembly.GetExecutingAssembly());
        logger.LogMComponentRegistering("MComponent registry successfully initialized");
    }

    public bool Register(Type type, float priority, JsonParser parser)
    {
        if (RegistryPriority.TryGetValue(type, out var oldPriority) && priority < oldPriority)
        {
            Logger.LogMComponentRegistering($"Already have Scene Mod parser: {type.Name}");
            return false;
        }
        Registry[type] = parser;
        RegistryPriority[type] = priority;
        Logger.LogMComponentRegistering($"Successfully registered Scene Mod parser: {type.Name}");
        return true;
    }

    /// <summary>
    /// Register all <see cref="IMComponent"/>s in an assembly that have the <see cref="SceneModParserAttribute"/>.
    /// </summary>
    /// <param name="assembly">Assembly to scan for classes with <see cref="SceneModParserAttribute"/>.</param>
    public void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods())
            {
                var attrs = type.GetCustomAttributes<SceneModParserAttribute>();
                if (!attrs.Any())
                {
                    continue;
                }
                if (!method.IsStatic)
                {
                    Logger.LogWarning($"Couldn't register Scene Mod parser {method.Name}, as its not static.");
                    continue;
                }
                if (method.ReturnType != typeof(bool))
                {
                    Logger.LogWarning($"Couldn't register Scene Mod parser {method.Name}, as its return type isn't bool.");
                    continue;
                }

                JsonParser parser;
                var params0 = method.GetParameters();
                if (params0.Length == 2 
                    && params0[0].ParameterType == typeof(JToken) 
                    && typeof(MBase).IsAssignableFrom(params0[1].ParameterType))
                {
                    parser = delegate (Type type, JToken val, out MBase res)
                    {
                        object[] args = [type, val, null];
                        bool success = (bool)method.Invoke(null, args);
                        res = (MBase)args[2];
                        return success;
                    };
                }
                else if (params0.Length == 3
                    && params0[0].ParameterType == typeof(Type)
                    && params0[1].ParameterType == typeof(JToken) 
                    && typeof(MBase).IsAssignableFrom(params0[2].ParameterType))
                {
                    parser = delegate (Type type, JToken val, out MBase res)
                    {
                        object[] args = [val, null];
                        bool success = (bool)method.Invoke(null, args);
                        res = (MBase)args[2];
                        return success;
                    };
                }
                else
                {
                    Logger.LogWarning($"Couldn't register Scene Mod parser {method.Name}, as its arguments aren't of type (Type, JToken, T) or (JToken, T) where T: MBase.");
                    continue;
                }

                foreach (var attr in attrs)
                {
                    foreach(var type0 in attr.Types)
                    {
                        Register(type0, attr.Priority, parser);
                    }
                }
            }
        }
        Logger.LogMComponentRegistering($"Registered all Scene Mod parsers in assembly: {assembly.FullName}");
    }

    public bool TryJsonParse(Type type, JToken val, out MBase res)
    {
        if (Registry.TryGetValue(type, out var parse) ||
            (type.IsGenericType && Registry.TryGetValue(type.GetGenericTypeDefinition(), out parse)))
        {
            return parse(type, val, out res);
        }
        foreach (var pair in Registry)
        {
            if (pair.Key.IsAssignableFrom(type))
            {
                return pair.Value(type, val, out res);
            }
        }
        res = default;
        return false;
    }

    public static bool TryJsonParseStatic(Type type, JToken val, out MBase res)
    {
        if (instance == null)
        {
            res = null;
            return false;
        }
        return instance.TryJsonParse(type, val, out res);
    }
}
