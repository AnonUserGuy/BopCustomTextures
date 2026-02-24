using BopCustomTextures.Customs;
using BopCustomTextures.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod GameObject definition. Includes no reference to the GameObject to modify, only a path to it.
/// </summary>
/// <param name="name">Name of/Path to GameObject to modify</param>
public class MGameObject(string name): MObject<GameObject>
{
    public string name = name;
    public bool? active;
    public MGameObject[] childObjs;
    public MGameObject[] childObjsDeferred;
    public MComponent[] components;

    public override GameObject Apply(GameObject obj)
    {
        if (active != null) obj.SetActive((bool)active);
        foreach (var mcomponent in components)
        {
            mcomponent.Apply(obj);
        }

        ApplyDeferred(obj);
        return obj;
    }

    public void ApplyDeferred(GameObject obj)
    {
        foreach (var mchildObj in childObjsDeferred)
        {
            foreach (var childObj in CustomSceneManager.FindGameObjectsInChildren(obj, mchildObj.name))
            {
                mchildObj.Apply(childObj);
            }
        }
    }
}
