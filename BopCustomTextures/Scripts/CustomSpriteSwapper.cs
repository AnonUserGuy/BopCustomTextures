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
    public Sprite LastVanilla;
    public Sprite Last;
    public readonly List<int> Variants = [];
    public CustomTextureManager TextureManager;
    public SpriteRenderer SpriteRenderer;

    void LateUpdate()
    {
        if (SpriteRenderer.sprite != Last)
        {
            LastVanilla = SpriteRenderer.sprite;
            Replace();
        }
    }

    void OnDisable()
    {
        SpriteRenderer.sprite = LastVanilla;
        Last = null;
    }

    public void Replace()
    {
        Last = TextureManager.ReplaceCustomSprite(LastVanilla, Variants);
        SpriteRenderer.sprite = Last;
    }

    public void ApplyVariants(List<int> newVariants)
    {
        Variants.Clear();
        foreach (var variants in newVariants)
        {
            Variants.Add(variants);
        }
        Replace();
    }
    public void ApplyVariants(Dictionary<int, int> indexedVariants)
    {
        foreach (var pair in indexedVariants)
        {
            Variants[pair.Key] = pair.Value;
        }
        Replace();
    }
}

