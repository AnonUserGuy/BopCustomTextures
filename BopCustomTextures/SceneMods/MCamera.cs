using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod Transform definition
/// </summary>
public class MCamera : MComponent<Camera>
{
    public bool? orthographic;
    public float? orthographicSize;
    public float? aspect;
    public Color? backgroundColor;

    public override Camera Apply(Camera component)
    {
        if (orthographic != null) component.orthographic = (bool)orthographic;
        if (orthographicSize != null) component.orthographicSize = (float)orthographicSize;
        if (aspect != null) component.aspect = (float)aspect;
        if (backgroundColor != null) component.backgroundColor = ApplyColor((Color)backgroundColor, component.backgroundColor);
        return component;
    }
}
