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

    public bool HasApplyInstant()
    {
        foreach (var i in IndicesInstant)
        {
            if (Elements[i].HasApplyInstant())
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

    public void Apply()
    {
        return;
    }

    public void Apply(MixtapeLoaderCustom __instance, LoaderComponentContexts ctxs, float outerBeat, float outerLength = 0f)
    {
        for (var i = 0; i < Elements.Length; i++)
        {
            var el = SceneSet.Elements[i];
            var mobjs = Elements[i];

            var length = el.Length ?? outerLength;
            var beat = outerBeat + el.Beat;
            if (el.Ratio.HasValue)
            {
                beat += (SceneSet.Loop ?? length) * el.Ratio.Value;
            }
            if (el.Offset.HasValue && !CustomSceneManager.TryGetOffsetBeat(__instance.jukebox, ref beat, length,
                el.Offset.Value,
                el.UseLength,
                el.ApplyByEnd))
            {
                continue;
            }

            if (SceneSet.Loop.HasValue)
            {
                for (; beat < outerBeat + outerLength; beat += SceneSet.Loop.Value)
                {
                    foreach (var mobj in mobjs)
                    {
                        mobj.Apply(__instance, ctxs, beat, length);
                    }
                }
            }
            else
            {
                foreach (var mobj in mobjs)
                {
                    mobj.Apply(__instance, ctxs, beat, length);
                }
            }
        }
    }
}

