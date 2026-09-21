using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using BopCustomTextures.SceneMods.Unity.Structs;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods.Unity.Components;

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
    public MMaterial mmaterial;

    public MMaterial MMaterial { get => mmaterial; set => mmaterial = value; }

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if      (KeyMatch(key, "Color") && MColor.TryJsonParse(ctx, val, out var jcolor)) color = jcolor;
        else if (KeyMatch(key, "Size") && MVector2.TryJsonParse(ctx, val, out var vector2)) size = vector2;
        else if (KeyMatch(key, "FlipX") && MValue<bool>.TryJsonParse(ctx, val, out var jval)) flipX = jval;
        else if (KeyMatch(key, "FlipY") && MValue<bool>.TryJsonParse(ctx, val, out var jval2)) flipY = jval2;
        else if (!MRenderable.JsonParsePair(ctx, key, val, this))
        {
            base.JsonParsePair(ctx, key, val);
        }
    }

    public override SpriteRenderer Apply(SpriteRenderer component)
    {
        if (color != null) component.color = MColor.Apply(color, component.color);
        if (size != null) component.size = MVector2.Apply(size, component.size);
        if (flipX != null) component.flipX = (bool)flipX;
        if (flipY != null) component.flipY = (bool)flipY;
        if (mmaterial != null) component.material = mmaterial.Apply(component.material);
        return base.Apply(component);
    }
}
