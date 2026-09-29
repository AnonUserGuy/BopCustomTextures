using BopCustomTextures.SceneMods.Unity.Components;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.Unity;

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

    public void ApplyLoader(IDictionary<Type, MLoaderComponentContext> ctxs, MixtapeLoaderCustom loader, Entity entity, float beat, GameObject rootObj)
    {
        mobj.ApplyLoader(ctxs, loader, entity, beat, obj, rootObj);
        foreach (var childObj in childObjs)
        {
            childObj.ApplyLoader(ctxs, loader, entity, beat, rootObj);
        }
    }

    public void ApplyLoader(IDictionary<Type, MLoaderComponentContext> ctxs, MixtapeLoaderCustom loader, Entity entity, float beat)
    {
        ApplyLoader(ctxs, loader, entity, beat, obj);
    }

    public void ApplyLoader(MixtapeLoaderCustom loader, Entity entity, float beat)
    {
        Dictionary<Type, MLoaderComponentContext> ctxs = [];
        ApplyLoader(ctxs, loader, entity, beat, obj);
        ApplyLoaderFinalize(ctxs);
    }

    public static void ApplyLoaderFinalize(IDictionary<Type, MLoaderComponentContext> ctxs)
    {
        foreach (var pair in ctxs)
        {
            if (pair.Value.Component is IMLoaderComponentFinal mcomponentFinal)
            {
                mcomponentFinal.ApplyLoaderFinalize(pair.Value.Context);
            }
        }
    }
}
