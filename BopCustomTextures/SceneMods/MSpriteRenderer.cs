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

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (ctx.TryGetJColor(key, val, "Color", out var jcolor)) color = jcolor;
        else if (ctx.TryGetJVector2(key, val, "Size", out var vector2)) size = vector2;
        else if (ctx.TryGetJValue(key, val, "FlipX", JTokenType.Boolean, out var jval)) flipX = (bool)jval;
        else if (ctx.TryGetJValue(key, val, "FlipY", JTokenType.Boolean, out var jval2)) flipY = (bool)jval2;
        else if (!MRenderable.JsonParsePair(ctx, key, val, this))
        {
            base.JsonParsePair(ctx, key, val);
        }
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
