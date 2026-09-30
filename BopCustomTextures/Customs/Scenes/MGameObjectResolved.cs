using BopCustomTextures.SceneMods.Unity.Components;
using UnityEngine;
using System;
using System.Collections.Generic;
using BopCustomTextures.SceneMods.Unity;

namespace BopCustomTextures.Customs.Scenes;

using LoaderComponentContexts = IDictionary<Type, MLoaderComponentContext>;

/// <summary>
/// Wrapper for <see cref="MGameObject"/> with reference to actual <see cref="GameObject"/>.
/// </summary>
/// <param name="mobj"><see cref="MGameObject"/> describing modifications to make to the <see cref="GameObject"/>.</param>
/// <param name="obj"><see cref="GameObject"/> to modify.</param>
public class MGameObjectResolved(MGameObject mobj, GameObject obj) : ISceneResolved
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

    public bool HasApplyInstant() => mobj.HasApplyInstant();

    public void ApplyInstant()
    {
        ApplyInstant(obj);
    }

    public void ApplyInstant(GameObject rootObj)
    {
        mobj.ApplyInstant(obj, rootObj);
        foreach (var childObj in childObjs)
        {
            childObj.ApplyInstant(rootObj);
        }
    }

    public void Apply(MixtapeLoaderCustom __instance, LoaderComponentContexts ctxs, float beat, float length = 0)
    {
        Apply(__instance, beat, ctxs, obj);
    }

    public void Apply(MixtapeLoaderCustom loader, float beat, LoaderComponentContexts ctxs, GameObject rootObj)
    {
        ApplyLoader(loader, ctxs, beat, rootObj);
        loader.scheduler.Schedule(beat, Apply);
    }

    public void ApplyLoader(MixtapeLoaderCustom loader, LoaderComponentContexts ctxs, float beat, GameObject rootObj)
    {
        mobj.ApplyLoader(loader, ctxs, beat, obj, rootObj);
        foreach (var childObj in childObjs)
        {
            childObj.ApplyLoader(loader, ctxs, beat, rootObj);
        }
    }

    public static void ApplyLoaderFinalize(MixtapeLoaderCustom loader, LoaderComponentContexts ctxs)
    {
        foreach (var pair in ctxs)
        {
            if (pair.Value.Component is IMLoaderComponentFinal mcomponentFinal)
            {
                mcomponentFinal.ApplyLoaderFinalize(loader, pair.Value.Context);
            }
        }
    }
}
