using BopCustomTextures.Customs;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="GameObject"/> definition. 
/// Includes no reference to the <see cref="GameObject"/> to modify, only a path to it.
/// </summary>
/// <param name="name">Name of/Path to GameObject to modify</param>
public class MGameObject(string name): MObject<GameObject>
{
    public string name = name;
    public MGameObject[] childObjs;
    public MGameObject[] childObjsDeferred;
    public IMComponent[] components;

    public override GameObject Apply(GameObject obj)
    {
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
