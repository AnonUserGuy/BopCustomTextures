using System;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

public class MTransform: MComponent
{
    public Vector3? localPosition;
    public Quaternion? localRotation;
    public Vector3? localEulerAngles;
    public Vector3? localScale;

    public void Apply(Transform component)
    {
        if (localPosition != null) component.localPosition = ApplyVector3((Vector3)localPosition, component.localPosition);
        if (localRotation != null) component.localRotation = ApplyQuaternion((Quaternion)localRotation, component.localRotation);
        else if (localEulerAngles != null) component.localEulerAngles = ApplyVector3((Vector3)localEulerAngles, component.localEulerAngles);
        if (localScale != null) component.localScale = ApplyVector3((Vector3)localScale, component.localScale);
    }
}
