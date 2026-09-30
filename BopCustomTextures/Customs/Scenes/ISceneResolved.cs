using BopCustomTextures.SceneMods.Unity.Components;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BopCustomTextures.Customs.Scenes;

using LoaderComponentContexts = IDictionary<Type, MLoaderComponentContext>;

public interface ISceneResolved
{
    public bool HasApplyInstant();

    public void ApplyInstant();

    public void Apply();

    public void Apply(MixtapeLoaderCustom __instance, LoaderComponentContexts ctxs, float beat, float length = 0f);
}

public class SceneResolvedMultiple(int capacity = 0) : ISceneResolved, IEnumerable<ISceneResolved>
{
    public readonly List<ISceneResolved> Elements = new(capacity);

    public int Count => Elements.Count;

    public ISceneResolved this[int i] { get => Elements[i]; set => Elements[i] = value; }

    public bool HasApplyInstant()
    {
        foreach (var el in Elements)
        {
            if (el.HasApplyInstant()) return true;
        }
        return false;
    }

    public void ApplyInstant()
    {
        foreach (var el in Elements)
        {
            el.ApplyInstant();
        }
    }

    public void Apply()
    {
        foreach (var el in Elements)
        {
            el.Apply();
        }
    }

    public void Apply(MixtapeLoaderCustom __instance, LoaderComponentContexts ctxs, float beat, float length = 0f)
    {
        foreach (var el in Elements)
        {
            el.Apply(__instance, ctxs, beat, length);
        }
    }

    public static SceneResolvedMultiple Add(ISceneResolved sceneA, ISceneResolved sceneB)
    {
        if (sceneA is not SceneResolvedMultiple sceneAMultiple)
        {
            sceneAMultiple = [];
            sceneAMultiple.Elements.Add(sceneA);
        }
        if (sceneB is SceneResolvedMultiple sceneBMultiple)
        {
            foreach (var subsceneB in sceneBMultiple)
            {
                sceneAMultiple.Elements.Add(subsceneB);
            }
        }
        else
        {
            sceneAMultiple.Elements.Add(sceneB);
        }
        return sceneAMultiple;
    }

    public IEnumerator<ISceneResolved> GetEnumerator() => Elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();
}

public static class SceneResolvedExtensions
{
    public static void Apply(this ISceneResolved instance, MixtapeLoaderCustom __instance, float beat, float length = 0f)
    {
        Dictionary<Type, MLoaderComponentContext> ctxs = [];
        instance.Apply(__instance, ctxs, beat, length);
        MGameObjectResolved.ApplyLoaderFinalize(__instance, ctxs);
    }
}