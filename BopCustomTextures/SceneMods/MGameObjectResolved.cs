using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Wrapper for MGameObject with reference to actual GameObject.
/// </summary>
/// <param name="mobj">MGameObject describing modifications to make to the GameObject.</param>
/// <param name="obj">GameObject to modify.</param>
public class MGameObjectResolved(MGameObject mobj, GameObject obj): MObject
{
    public MGameObject mobj = mobj;
    public GameObject obj = obj;
    public MGameObjectResolved[] childObjs;

    public void Apply()
    {
        mobj.Apply(obj);
        foreach (var childObj in childObjs)
        {
            childObj.Apply();
        }
    }
}
