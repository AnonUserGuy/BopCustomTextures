using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod SpriteRenderer definition
/// </summary>
public class MSpriteRenderer : MComponent<SpriteRenderer>, IMRenderable
{
    public Color? color;
    public Vector2? size;
    public bool? flipX;
    public bool? flipY;
    public Material material;
    public MMaterial mmaterial;

    public Material Material { get => material; set => material = value; }
    public MMaterial MMaterial { get => mmaterial; set => mmaterial = value; }

    public override SpriteRenderer Apply(SpriteRenderer component)
    {
        if (color != null) component.color = ApplyColor((Color)color, component.color);
        if (size != null) component.size = ApplyVector2((Vector2)size, component.size);
        if (flipX != null) component.flipX = (bool)flipX;
        if (flipY != null) component.flipY = (bool)flipY;
        if (material != null) component.material = material;
        if (mmaterial != null) component.material = mmaterial.Apply(component.material);
        return component;
    }
}
