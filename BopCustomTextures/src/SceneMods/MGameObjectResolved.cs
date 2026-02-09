using BopCustomTextures.Customs;
using System.Collections.Generic;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

public class MGameObjectResolved(MGameObject mobj, GameObject obj)
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
