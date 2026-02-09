using BopCustomTextures.Customs;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

public class MGameObject(string name)
{
    public string name = name;
    public bool? active;
    public MGameObject[] childObjs;
    public MGameObject[] childObjsVolatile;
    public MComponent[] components;

    public void Apply(GameObject obj)
    {
        if (active != null) obj.SetActive((bool)active);
        foreach (var mcomponent in components)
        {
            switch (mcomponent)
            {
                case MTransform mtransform:
                    mtransform.Apply(obj.transform);
                    break;
                case MSpriteRenderer mspriteRenderer:
                    if (obj.TryGetComponent<SpriteRenderer>(out var spriteRenderer)) mspriteRenderer.Apply(spriteRenderer);
                    break;
                case MParallaxObjectScript mparallaxObjectScript:
                    if (obj.TryGetComponent<ParallaxObjectScript>(out var parallaxObjectScript)) mparallaxObjectScript.Apply(parallaxObjectScript);
                    break;
            }
        }

        foreach (var mchildObj in childObjsVolatile)
        {
            foreach (var childObj in CustomSceneManager.FindGameObjectsInChildren(obj, mchildObj.name))
            {
                mchildObj.Apply(childObj);
            }
        }
    }
}
