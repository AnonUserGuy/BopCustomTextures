using BopCustomTextures.SceneMods.System;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene Mod <see cref="UnityEngine.Object"/> definition.
/// </summary>
/// <typeparam name="T">Target <see cref="UnityEngine.Object"/> type.</typeparam>
public class MUnityObject<T>: MObject<T> where T : UnityEngine.Object;