using BopCustomTextures.SceneMods.System;
using BopCustomTextures.SceneMods.System.Generic;
using BopCustomTextures.SceneMods.Unity.Components;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Json;

/// <summary>
/// Manages deserializing of scene mod component definitions through registered deserializer functions.
/// </summary>
public class MComponentParserRegistry(ILogger logger)
{
    private const string GenericConstraint = "Generic MTypes must be of format " +
        "<Target generic class, IMBase<T1>, T1, IMBase<T2>, T2, etc.> or <IMBase<T1>, T1, IMBase<T2>, T2, etc.>";

    /// <summary>
    /// A function to use to deserialize a scene mod component.
    /// </summary>
    /// <param name="ctx">The invoking <see cref="CustomJsonInitializer"/>, for logging and general parsing methods.</param>
    /// <param name="jcomponent">JSON component definition.</param>
    /// <param name="mcomponent">Scene mod <see cref="IMComponent"/> outputted by parse.</param>
    /// <returns><see langword="true"/> if parse successful, <see langword="false"/> otherwise.</returns>
    public delegate bool MComponentJsonParser(CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent);

    private class MComponentRegister(MComponentJsonParser parser, float priority = 0)
    {
        public MComponentJsonParser Parser = parser;
        public float Priority = priority;
    }

    private class MTypeRegister(Type type, float priority = 0)
    {
        public Type Type = type;
        public float Priority = priority;
    }

    private class Tree<T> : IEnumerable<KeyValuePair<T, Tree<T>>>
    {
        public Dictionary<T, Tree<T>> Descendents = [];
        public Tree<T> this[T i] { get => Descendents[i]; set => Descendents[i] = value; }
        public IEnumerator<KeyValuePair<T, Tree<T>>> GetEnumerator() => Descendents.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool TryGetValue(T i, out Tree<T> val) => Descendents.TryGetValue(i, out val);

        public bool TryFind<Q>(Q tin, out T tout, Func<Q, T, bool> comparer)
        {
            foreach (var pair in this)
            {
                if (comparer(tin, pair.Key))
                {
                    if (!pair.Value.TryFind(tin, out tout, comparer))
                    {
                        tout = pair.Key;
                    }
                    return true;
                }
            }
            tout = default;
            return false;
        }

        public Tree<T> Add(IEnumerable<T> tins)
        {
            var node = this;
            foreach (var tin in tins)
            {
                if (!node.TryGetValue(tin, out var node0))
                {
                    //BopCustomTexturesPlugin.LogWarning($"{tin} created");
                    node0 = new();
                    node[tin] = node0;
                }
                else
                {
                    //BopCustomTexturesPlugin.LogWarning($"{tin} had");
                }
                node = node0;
            }
            return node;
        }
    }

    private class MTypeTree : Tree<Type>
    {
        public static bool IsMatch(Type typeIn, Type typeOut)
        {
            //BopCustomTexturesPlugin.LogWarning("");
            //BopCustomTexturesPlugin.LogWarning($"{typeIn} == {typeOut}?");

            if (typeOut.IsGenericTypeDefinition)
            {
                var argCount = typeOut.GetGenericArguments().Length;
                if (argCount % 2 == 0)
                {
                    if (!typeIn.IsGenericType
                        || typeOut.BaseType == null
                        || !typeOut.BaseType.IsGenericType)
                    {
                        return false;
                    }
                    var typeOutXXX = typeOut.BaseType.GetGenericArguments()[0];

                    //BopCustomTexturesPlugin.LogWarning("case 1");

                    return typeOutXXX.IsGenericType
                        && typeOutXXX.GetGenericTypeDefinition() == typeIn.GetGenericTypeDefinition();
                }
                else
                {
                    //BopCustomTexturesPlugin.LogWarning("case 2");

                    Type[] testArgs = new Type[argCount];
                    testArgs[0] = typeIn;
                    for (var i = 1; i < testArgs.Length; i += 2)
                    {
                        testArgs[i] = typeof(MFloat);
                        testArgs[i + 1] = typeof(float);
                    }
                    try
                    {
                        typeOut.MakeGenericType(testArgs);
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
            }

            //BopCustomTexturesPlugin.LogWarning("case 3");
            //BopCustomTexturesPlugin.LogWarning(typeOut.BaseType);
            //BopCustomTexturesPlugin.LogWarning(typeOut.BaseType != null && typeOut.BaseType.IsGenericType);
            //BopCustomTexturesPlugin.LogWarning(typeOut.BaseType != null && typeOut.BaseType.IsGenericType ? typeOut.BaseType.GetGenericArguments()[0] : "invalid");

            return typeOut.BaseType != null 
                && typeOut.BaseType.IsGenericType 
                && typeOut.BaseType.GetGenericArguments()[0] == typeIn;
        }

        public bool TryFind(Type typeIn, out Type typeOut) => TryFind(typeIn, out typeOut, IsMatch);
    }

    private static MComponentParserRegistry instance;
    private readonly ILogger Logger = logger;

    private readonly Dictionary<string, MComponentRegister> MComponentRegistry = [];
    private readonly Dictionary<Type, MTypeRegister> MTypeRegistry = [];
    private readonly MTypeTree MTypeTreeBase = new();
    private readonly Dictionary<Type, Type> TypeToMType = [];

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

    private bool Register(string name, MComponentRegister register)
    {
        if (name.StartsWith("!"))
        {
            name = name.Substring(1);
        }
        if (MComponentRegistry.TryGetValue(name, out var register0))
        {
            if (register.Priority < register0.Priority)
            {
                Logger.LogMComponentRegistering($"MComponent \"{name}\" was already registered, but not overriden ({register.Priority} < {register0.Priority})");
                return false;
            }
            else
            {
                Logger.LogMComponentRegistering($"MComponent \"{name}\" is already registered, and will be overriden ({register.Priority} >= {register0.Priority})");
            }
        }
        MComponentRegistry[name] = register;
        Logger.LogMComponentRegistering($"Successfully registered MComponent: {name}");
        return true;
    }

    /// <summary>
    /// Register an arbitrary parsing method for an <see cref="IMComponent"/>.
    /// </summary>
    /// <param name="name">String name of component as used in JSON.</param>
    /// <param name="parser">Parsing method. Takes a <see cref="CustomJsonInitializer"/> for logging and general parsing functions, 
    ///  and a <see cref="JObject"/> containing a JSON component definition. Outputs an <see cref="IMComponent"/> via out parameter, 
    ///  and returns <see langword="true"/>/<see langword="false"/> indicating parsing success.</param>
    public bool Register(string name, MComponentJsonParser parser, float priority = 0)
    {
        return Register(name, new(parser, priority));
    }

    /// <summary>
    /// Register an <see cref="IMComponent"/>.
    /// </summary>
    /// <param name="name">String name of component as used in JSON.</param>
    public bool Register<T>(string name, float priority = 0) where T : IMComponent, new()
    {
        return Register(name, (CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent) =>
        {
            mcomponent = new T();
            return mcomponent.JsonParse(ctx, jcomponent);
        }, priority);
    }

    public bool Register(string name, Type type, float priority = 0)
    {
        if (!typeof(IMComponent).IsAssignableFrom(type))
        {
            Logger.LogError($"Failed to register MComponent \"{type.Name}\": does not implement IMComponent");
            return false;
        }
        if (type.IsAbstract)
        {
            Logger.LogError($"Failed to register MComponent \"{type.Name}\": is an abstract class");
            return false;
        }

        var ctor = CreateFactory<IMComponent>(type);
        Register(name, (CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent) =>
        {
            mcomponent = ctor();
            return mcomponent.JsonParse(ctx, jcomponent);
        }, priority);
        return true;
    }

    public bool Register(Type type)
    {
        var attrs = type.GetCustomAttributes<MComponentAttribute>();
        if (!attrs.Any())
            return false;

        if (!typeof(IMComponent).IsAssignableFrom(type))
        {
            Logger.LogError($"Failed to register MComponent \"{type.Name}\": does not implement IMComponent");
            return false;
        }
        if (type.IsAbstract)
        {
            Logger.LogError($"Failed to register MComponent \"{type.Name}\": is an abstract class");
            return false;
        }

        var ctor = CreateFactory<IMComponent>(type);
        MComponentJsonParser parser = (CustomJsonInitializer ctx, JToken jcomponent, out IMComponent mcomponent) =>
        {
            mcomponent = ctor();
            return mcomponent.JsonParse(ctx, jcomponent);
        };

        bool changed = false;
        foreach (var attr in attrs)
        {
            MComponentRegister register = new(parser, attr.Priority);

            foreach (var name in attr.Names)
            {
                if (Register(name, register)) changed = true;
            }
        }
        return changed;
    }

    private bool RegisterType(Type type, MTypeRegister register)
    {
        if (MTypeRegistry.TryGetValue(type, out var register0))
        {
            if (register.Priority < register0.Priority)
            {
                Logger.LogMComponentRegistering($"MType \"{type.Name}\" was already registered, but not overriden ({register.Priority} < {register0.Priority})");
                return false;
            }
            else
            {
                Logger.LogMComponentRegistering($"MType \"{type.Name}\" is already registered, and will be overriden ({register.Priority} >= {register0.Priority})");
            }
        }
        MTypeRegistry[type] = register;
        Logger.LogMComponentRegistering($"Successfully registered MType: {type}");
        return true;
    }

    public bool RegisterType(Type type)
    {
        /*var attrs = type.GetCustomAttributes<MTypeAttribute>();
        if (attrs.Any())
        {
            if (!typeof(IMBase).IsAssignableFrom(type))
            {
                Logger.LogError($"Failed to register MType \"{type.Name}\": does not implement IMBase");
                return false;
            }
            if (type.IsAbstract)
            {
                Logger.LogError($"Failed to register MType \"{type.Name}\": is an abstract class");
                return false;
            }

            bool changed = false;
            foreach (var attr in attrs)
            {
                MTypeRegister register = new(type, attr.Priority);

                foreach (var targetType in attr.Types)
                {
                    if (RegisterType(targetType, register)) changed = true;
                }
            }
            return changed;
        } 
        else*/ if (type.GetCustomAttribute<NotMTypeAttribute>() == null)
        {
            var chain = GetDescendenceChain(typeof(MBase<>), type);
            if (chain != null)
            {
                if (type.IsGenericType)
                {
                    var args = type.GetGenericArguments();
                    for (var i = args.Length % 2; i < args.Length; i += 2)
                    {
                        Type genericType;
                        try
                        {
                            genericType = typeof(IMBase<>).MakeGenericType(args[i + 1]);
                        } 
                        catch (ArgumentException)
                        {
                            Logger.LogError($"Failed to register generic MType \"{type.Name}\": IMBase<{args[i + 1]}> is invalid");
                            return false;
                        }
                        if (!genericType.IsAssignableFrom(args[i]))
                        {
                            Logger.LogError($"Failed to register generic MType \"{type.Name}\": {args[i]} should be IMBase<{args[i + 1]}>. " + GenericConstraint);

                            return false;
                        }
                    }
                }
                MTypeTreeBase.Add(chain);
                Logger.LogMComponentRegistering($"Successfully registered generic MType: {type.Name}");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Register all <see cref="IMComponent"/>s in an assembly that have the <see cref="MComponentAttribute"/>.
    /// </summary>
    /// <param name="assembly">Assembly to scan for classes with <see cref="MComponentAttribute"/>.</param>
    public void RegisterAssembly(Assembly assembly)
    {
        bool changed = false;
        foreach (var type in assembly.GetTypes())
        {
            Register(type);
            if (RegisterType(type)) changed = true;
        }
        if (changed)
        {
            TypeToMType.Clear();
#if DEBUG
            PrintTree("base", MTypeTreeBase);
#endif
        }
        Logger.LogMComponentRegistering($"Registered all MComponents in assembly: {assembly.FullName}");
    }

#if DEBUG
    private void PrintTree<T>(string path, Tree<T> node)
    {
        Logger.LogWarning(path);
        foreach (var subnode in node)
        {
            PrintTree(path + " -> " + subnode.Key, subnode.Value);
        }
    }
#endif

    public bool HasComponentRegistered(string name)
    {
        return MComponentRegistry.ContainsKey(name);
    }

    public bool TryParseComponent(CustomJsonInitializer ctx, string name, JToken jcomponent, out IMComponent mcomponent)
    {
        if (MComponentRegistry.TryGetValue(name, out var register) && register != null)
        {
            return register.Parser(ctx, jcomponent, out mcomponent);
        }

        mcomponent = default;
        return false;
    }

    public bool TryLateParseComponent(CustomJsonInitializer ctx, string name, JToken jcomponent, GameObject obj, out IMComponent mcomponent)
    {
        if (!TryParseComponent(ctx, name, jcomponent, out mcomponent))
        {
            Component component = obj.GetComponent(name);
            if (component == null)
            {
                Logger.LogWarning($"Failed to late register MComponent \"{name}\": Did not exist on specified object. Will try on other objects");
                return false;
            }

            if (!TryGetType(component.GetType(), out var mtype))
            {
                Logger.LogError($"Failed to late register MComponent \"{name}\": No suitable MType found");
                MComponentRegistry[name] = null;
                return false;
            }
            
            if (!Register(name, mtype, float.NegativeInfinity) || !TryParseComponent(ctx, name, jcomponent, out mcomponent))
            {
                Logger.LogError($"Failed to late register MComponent \"{name}\": Weird error, contact developer");
                MComponentRegistry[name] = null;
                return false;
            }
        }
        return true;
    }

    public bool TryGetType(Type type, out Type mtype)
    {
        if (!TypeToMType.TryGetValue(type, out mtype))
        {
            mtype = FindType(type);
            if (mtype != null)
            {
                Logger.LogMComponentRegistering($"Successfully registered concrete MType for {type.FullName}: {mtype.FullName ?? mtype.Name}"); 
                // if mtype.FullName is null then it most certainly did not successfully register concrete mtype
            }
            TypeToMType[type] = mtype;
        }
        return mtype != null;
    }

    private Type FindType(Type type)
    {
        /*foreach (var type in SelfAndAncestors(type1))
        {
            if (MTypeRegistry.TryGetValue(type, out var register))
            {
                return register.Type;
            }
            else if (type.IsGenericType && MTypeRegistry.TryGetValue(type.GetGenericTypeDefinition(), out register))
            {
                Type[] subTypes = type.GetGenericArguments();
                Type[] msubTypes = new Type[register.Type.GetGenericArguments().Length];

                if (!TryGetType(subTypes[0], out var msubType))
                {
                    return null;
                }
                msubTypes[0] = msubType;

                var j = 1;
                for (var i = 1; i < subTypes.Length; i++)
                {
                    if (!TryGetType(subTypes[i], out msubType))
                    {
                        return null;
                    }
                    subTypes[i] = msubType;

                    j += 2;
                }
                return register.Type.MakeGenericType(subTypes);
            }
            else if (type.IsArray)
            {
                var subType = type.GetElementType();
                if (!TryGetType(subType, out var msubType))
                {
                    return null;
                }
                return typeof(MArray<,>).MakeGenericType(msubType, subType);
            }
        }*/

        if (type.IsArray)
        {
            var subType = type.GetElementType();
            if (!TryGetType(subType, out var msubType))
            {
                Logger.LogError($"Failed to register concrete MType for \"{type.Name}\": Couldn't register element type");
                return null;
            }
            return typeof(MArray<,>).MakeGenericType(msubType, subType);
        }

        if (MTypeTreeBase.TryFind(type, out var mtype))
        {
            while (mtype != typeof(MBase<>) && mtype.IsAbstract)
            {
                mtype = mtype.BaseType.IsGenericType ? mtype.BaseType.GetGenericTypeDefinition() : mtype.BaseType;
            }
            if (mtype.IsAbstract)
            {
                Logger.LogError($"Failed to register concrete MType for \"{type.Name}\": MType {mtype.Name} is abstract");
                return null;
            }
            if (!mtype.IsGenericTypeDefinition)
            {
                return mtype;
            }

            var margsCount = mtype.GetGenericArguments().Length;
            if (margsCount == 1)
            {
                Logger.LogError("hello");
                return mtype.MakeGenericType(type);
            }

            if (!type.IsGenericType)
            {
                Logger.LogError($"Failed to register concrete MType for \"{type.Name}\": MType {mtype.Name} is generic, but {type} is not");
                return null;
            }

            var args = type.GetGenericArguments();
           
            if (args.Length != margsCount / 2)
            {
                Logger.LogError($"Failed to register concrete MType for \"{type.Name}\": Invalid generic argument count. " + GenericConstraint);
                return null;
            }

            var margs = new Type[margsCount];
            var offset = margsCount % 2;

            if (offset == 1)
            {
                margs[0] = type;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var subType = args[i];
                if (!TryGetType(subType, out var msubType))
                {
                    Logger.LogError($"Failed to register concrete MType for \"{type.Name}\": Couldn't register generic parameter type");
                    return null;
                }
                margs[offset + 2 * i] = msubType;
                margs[offset + 2 * i + 1] = subType;
            }

            return mtype.MakeGenericType(margs);
        }

        Logger.LogError($"Failed to register concrete MType for \"{type.Name}\": No suitable MType found");
        return null;
    }

    public bool TryParseJson(CustomJsonInitializer ctx, Type type, JToken val, out IMBase mval)
    {
        if (!TryGetType(type, out var mtype))
        {
            mval = default;
            return false;
        }

        mval = (IMBase)Activator.CreateInstance(mtype);
        return mval.JsonParse(ctx, type, val);
    }

    private static Func<T> CreateFactory<T>(Type type)
    {
        var newExpr = Expression.New(type);
        var castExpr = Expression.Convert(newExpr, typeof(T));
        return Expression.Lambda<Func<T>>(castExpr).Compile();
    }

    private static IEnumerable<Type> GetDescendenceChain(Type openGenericType, Type type)
    {
        if (type == null)
        {
            return null;
        }

        Stack<Type> types = [];

        while (type.BaseType != null 
            && !(type.BaseType.IsGenericType && type.BaseType.GetGenericArguments().Length % 2 == 1) )
        {
            type = type.BaseType;
        }

        if (type.IsGenericType)
        {
            types.Push(type.GetGenericTypeDefinition());
        }
        else
        {
            types.Push(type);
        }

        type = type.BaseType;
        for (; type != null && type.IsGenericType; type = type.BaseType)
        {
            var openGenericType0 = type.GetGenericTypeDefinition();
            if (openGenericType0 == openGenericType)
            {
                return types;
            }
            types.Push(openGenericType0);
        }
        return null;
    }

    private static IEnumerable<Type> SelfAndAncestors(Type type)
    {
        if (type == null) yield break;

        // self
        yield return type;

        // interfaces
        foreach (var interface0 in type.GetInterfaces())
        {
            yield return interface0;
        }

        // parents
        for (var parent = type.BaseType; parent != null; parent = parent.BaseType)
        {
            yield return parent;
        }
    }
}