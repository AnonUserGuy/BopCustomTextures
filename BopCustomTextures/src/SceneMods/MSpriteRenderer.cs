using UnityEngine;

namespace BopCustomTextures.SceneMods;

public class MSpriteRenderer : MComponent
{
    public Color? color;
    public Vector2? size;
    public bool? flipX;
    public bool? flipY;
    public Material material;

    public void Apply(SpriteRenderer component)
    {
        if (color != null) component.color = ApplyColor((Color)color, component.color);
        if (size != null) component.size = ApplyVector2((Vector2)size, component.size);
        if (flipX != null) component.flipX = (bool)flipX;
        if (flipY != null) component.flipY = (bool)flipY;
        if (material != null) component.material = material;
    }
}
