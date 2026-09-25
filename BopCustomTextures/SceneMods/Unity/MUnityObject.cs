using BopCustomTextures.SceneMods.System;
using System;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene Mod <see cref="UnityEngine.Object"/> definition.
/// </summary>
/// <typeparam name="T">Target <see cref="UnityEngine.Object"/> type.</typeparam>
public class MUnityObject<T>: MObject<T> where T : UnityEngine.Object
{
    public static bool KeyMatch(string key1, string key2)
    {
        return key1.Equals(key2, StringComparison.OrdinalIgnoreCase);
    }
}