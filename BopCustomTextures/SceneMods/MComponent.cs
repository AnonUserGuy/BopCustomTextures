using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene Mod generic component definition.
/// </summary>
public abstract class MComponent: MObject;
public abstract class MComponent<T>: MComponent where T: Component
{
    public abstract T Apply(T obj);
}