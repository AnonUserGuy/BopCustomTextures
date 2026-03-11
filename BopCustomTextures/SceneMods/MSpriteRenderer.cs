using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="SpriteRenderer"/> definition
/// </summary>
[MComponent("SpriteRenderer")]
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

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        if (ctx.TryGetJColor(jcomponent, "Color", out var jcolor)) color = jcolor;
        if (ctx.TryGetJVector2(jcomponent, "Size", out var vector2)) size = vector2;
        JValue jval;
        if (ctx.TryGetJValue(jcomponent, "FlipX", JTokenType.Boolean, out jval)) flipX = (bool)jval;
        if (ctx.TryGetJValue(jcomponent, "FlipY", JTokenType.Boolean, out jval)) flipY = (bool)jval;
        MRenderable.JsonParse(ctx, jcomponent, this);
        return true;
    }

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
