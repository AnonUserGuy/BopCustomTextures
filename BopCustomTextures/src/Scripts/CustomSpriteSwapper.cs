using BopCustomTextures.Customs;
using UnityEngine;

namespace BopCustomTextures.Scripts;

[DefaultExecutionOrder(2)] // because of flow worms
internal class CustomSpriteSwapper : MonoBehaviour
{
    public Sprite last;
    public SceneKey scene;
    public CustomTextureManager textureManager;
    public SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer.sprite != last)
        {
            textureManager.ReplaceCustomSprite(spriteRenderer, scene);
            last = spriteRenderer.sprite;
        }
    }
}

