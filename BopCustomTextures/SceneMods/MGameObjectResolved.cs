using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Wrapper for <see cref="MGameObject"/> with reference to actual <see cref="GameObject"/>.
/// </summary>
/// <param name="mobj"><see cref="MGameObject"/> describing modifications to make to the <see cref="GameObject"/>.</param>
/// <param name="obj"><see cref="GameObject"/> to modify.</param>
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
