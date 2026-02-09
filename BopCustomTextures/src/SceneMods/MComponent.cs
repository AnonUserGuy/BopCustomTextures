using System;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

public abstract class MComponent
{
    public Vector2 ApplyVector2(Vector2 src, Vector2 dest)
    {
        if (!float.IsNaN(src.x)) dest.x = src.x;
        if (!float.IsNaN(src.y)) dest.y = src.y;
        return dest;
    }

    public Vector3 ApplyVector3(Vector3 src, Vector3 dest)
    {
        if (!float.IsNaN(src.x)) dest.x = src.x;
        if (!float.IsNaN(src.y)) dest.y = src.y;
        if (!float.IsNaN(src.z)) dest.z = src.z;
        return dest;
    }

    public Quaternion ApplyQuaternion(Quaternion src, Quaternion dest)
    {
        if (!float.IsNaN(src.x)) dest.x = src.x;
        if (!float.IsNaN(src.y)) dest.y = src.y;
        if (!float.IsNaN(src.z)) dest.z = src.z;
        if (!float.IsNaN(src.w)) dest.w = src.w;
        return dest;
    }

    public Color ApplyColor(Color src, Color dest)
    {
        if (!float.IsNaN(src.r)) dest.r = src.r;
        if (!float.IsNaN(src.g)) dest.g = src.g;
        if (!float.IsNaN(src.b)) dest.b = src.b;
        if (!float.IsNaN(src.a)) dest.a = src.a;
        return dest;
    }
}
