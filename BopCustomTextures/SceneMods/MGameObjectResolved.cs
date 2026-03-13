using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Wrapper for <see cref="MGameObject"/> with reference to actual <see cref="GameObject"/>.
/// </summary>
/// <param name="mobj"><see cref="MGameObject"/> describing modifications to make to the <see cref="GameObject"/>.</param>
/// <param name="obj"><see cref="GameObject"/> to modify.</param>
public class MGameObjectResolved(MGameObject mobj, GameObject obj)
{
    public MGameObject mobj = mobj;
    public GameObject obj = obj;
    public MGameObjectResolved[] childObjs;

    /// <summary>
    /// Apply the resolved scene mod.
    /// </summary>
    /// <param name="rootObj">Root <see cref="GameObject"/> of game. 
    /// Deferred child selectors won't be able to ascend past it with "..".</param>
    public void Apply(GameObject rootObj)
    {
        mobj.Apply(obj, rootObj);
        foreach (var childObj in childObjs)
        {
            childObj.Apply(rootObj);
        }
    }

    /// <summary>
    /// Apply the resolved scene mod.
    /// </summary>
    public void Apply()
    {
        Apply(obj);
    }
}
