using BopCustomTextures.SceneMods.Base;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene Mod generic unity <see cref="UnityEngine.Object"/> definition.
/// </summary>
public abstract class MUnityObject<T>: MObject<T> where T : UnityEngine.Object;