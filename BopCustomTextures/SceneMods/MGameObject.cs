using BopCustomTextures.Customs;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="GameObject"/> definition. 
/// Includes no reference to the <see cref="GameObject"/> to modify, only a path to it.
/// </summary>
/// <param name="name">Name of/Path to GameObject to modify</param>
public class MGameObject(string name, MGameObject[] childObjs, MGameObject[] childObjsDeferred, IMComponent[] components) : MObject<GameObject>
{
    public string name = name;
    public MGameObject[] childObjs = childObjs;
    public MGameObject[] childObjsDeferred = childObjsDeferred;
    public IMComponent[] components = components;

    public override GameObject Apply(GameObject obj)
    {
        foreach (var mcomponent in components)
        {
            mcomponent.Apply(obj);
        }

        foreach (var mchildObj in childObjsDeferred)
        {
            foreach (var childObj in CustomSceneManager.FindGameObjectsInChildren(obj, mchildObj.name))
            {
                mchildObj.Apply(childObj, obj);
            }
        }
        return obj;
    }

    /// <summary>
    /// Apply scene mod to <see cref="GameObject"/>, using a rootObj that deferred child objects can use to prevent 
    /// bad access using "..".
    /// </summary>
    /// <param name="obj"><see cref="GameObject"/> to apply scene mod to.</param>
    /// <param name="rootObj">Root <see cref="GameObject"/> of game. 
    /// Deferred child selectors won't be able to ascend past it with "..".</param>
    /// <returns><see cref="GameObject"/> with scene mod applied to it.</returns>
    public GameObject Apply(GameObject obj, GameObject rootObj)
    {
        foreach (var mcomponent in components)
        {
            mcomponent.Apply(obj);
        }

        foreach (var mchildObj in childObjsDeferred)
        {
            foreach (var childObj in CustomSceneManager.FindGameObjectsInChildren(rootObj, obj, mchildObj.name))
            {
                mchildObj.Apply(childObj, rootObj);
            }
        }
        return obj;
    }
}
