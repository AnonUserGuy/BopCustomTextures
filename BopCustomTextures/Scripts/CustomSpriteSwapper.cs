using BopCustomTextures.Customs;
using UnityEngine;
using System.Collections.Generic;

namespace BopCustomTextures.Scripts;

/// <summary>
/// Unity component that swaps a spriteRenderer's sprite to a custom one if a custom one is available.
/// </summary>
[DefaultExecutionOrder(2)] // because of flow worms
public class CustomSpriteSwapper : MonoBehaviour
{
    public Sprite lastVanilla;
    public Sprite last;
    public readonly List<int> variants = [];
    public CustomTextureManager textureManager;
    public SpriteRenderer spriteRenderer;

    void LateUpdate()
    {
        if (spriteRenderer.sprite != last)
        {
            lastVanilla = spriteRenderer.sprite;
            Replace();
        }
    }

    void OnDisable()
    {
        spriteRenderer.sprite = lastVanilla;
        last = null;
    }

    public void Replace()
    {
        last = textureManager.ReplaceCustomSprite(lastVanilla, variants);
        spriteRenderer.sprite = last;
    }
}

