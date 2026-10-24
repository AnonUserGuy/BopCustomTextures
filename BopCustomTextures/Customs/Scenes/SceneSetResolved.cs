using BopCustomTextures.SceneMods.Unity.Components;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.Customs.Scenes;

using LoaderComponentContexts = IDictionary<Type, MLoaderComponentContext>;

public class SceneSetResolved(SceneSet sceneSet) : ISceneResolved
{
    public readonly SceneSet SceneSet = sceneSet;
    public readonly SceneResolvedMultiple[] Elements = new SceneResolvedMultiple[sceneSet.Elements.Length];
    public readonly int[] IndicesInstant = sceneSet.IndicesInstant;

    public bool HasApplyInstant(MixtapeLoaderCustom __instance)
    {
        if (CheckPredicate(__instance))
        {
            return false;
        }
        foreach (var i in IndicesInstant)
        {
            if (Elements[i].HasApplyInstant(__instance))
            {
                return true;
            }
        }
        return false;
    }

    public void ApplyInstant()
    {
        foreach (var i in IndicesInstant)
        {
            Elements[i].ApplyInstant();
        }
    }

    public void Apply(MixtapeLoaderCustom __instance)
    {
        if (CheckPredicate(__instance))
        {
            return;
        }
        foreach (var mobj in Elements)
        {
            mobj.Apply(__instance);
        }
    }

    public void Apply(MixtapeLoaderCustom __instance, LoaderComponentContexts ctxs, float outerBeat, float outerLength = 0f)
    {
        if (CheckPredicate(__instance))
        {
            return;
        }
        for (var i = 0; i < Elements.Length; i++)
        {
            var el = SceneSet.Elements[i];
            var mobj = Elements[i];

            var length = el.Length ?? outerLength;
            var beat = outerBeat + el.Beat;
            if (el.Ratio.HasValue)
            {
                beat += length * el.Ratio.Value;
            }
            if (el.Offset.HasValue && !CustomSceneManager.TryGetOffsetBeat(__instance.jukebox, ref beat, length,
                el.Offset.Value,
                el.UseLength,
                el.ApplyByEnd))
            {
                continue;
            }

            if (SceneSet.Loop.HasValue && !el.Ratio.HasValue)
            {
                for (; beat < outerBeat + outerLength; beat += SceneSet.Loop.Value)
                {
                    mobj.Apply(__instance, ctxs, beat, length);
                }
            }
            else
            {
                mobj.Apply(__instance, ctxs, beat, length);
            }
        }
    }

    private bool CheckPredicate(MixtapeLoaderCustom __instance)
    {
        return SceneSet.Autoplay.HasValue && SceneSet.Autoplay.Value != __instance.RootObjects[SceneSet.Scene].GetComponentInChildren<GameplayScript>().inputManager.autoplay.AutoplayActive;
    }
}

