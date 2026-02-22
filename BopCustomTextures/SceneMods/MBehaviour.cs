using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod MonoBehaviour definition
/// </summary>
public abstract class MBehaviour<T> : MComponent<T> where T: Behaviour
{
    public bool? enabled;
    public override T Apply(T component)
    {
        if (enabled != null) component.enabled = (bool)enabled;
        return component;
    }
}