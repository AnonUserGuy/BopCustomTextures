using UnityEngine;

namespace BopCustomTextures;
internal class CustomSpriteScript : MonoBehaviour
{
    private Sprite last;
    public SceneKey scene;
    public SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (spriteRenderer.sprite != last)
        {
            CustomTextureManagement.ReplaceCustomSprite(spriteRenderer, scene);
            last = spriteRenderer.sprite;
        }
    }
}

