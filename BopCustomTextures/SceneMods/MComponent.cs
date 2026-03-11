using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene Mod generic <see cref="Component"/> definition.
/// </summary>
public abstract class MComponent: MObject
{
    public abstract void Apply(GameObject obj);
}
public abstract class MComponent<T>: MComponent where T: Component
{
    public abstract T Apply(T obj);

    public override void Apply(GameObject obj)
    {
        T component = obj.GetComponent<T>();
        if (component)
        {
            Apply(component);
        }
    }
}