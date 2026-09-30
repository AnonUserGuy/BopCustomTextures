using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BopCustomTextures.Customs.Scenes;

public class SceneSet : IScene, IEnumerable<SceneSetElement>
{
    public readonly SceneSetElement[] Elements;
    public readonly int[] IndicesInstant;
    public readonly SceneKey Scene;
    public readonly float? Loop;

    public SceneSet(SceneSetElement[] elements,
        SceneKey scene = SceneKey.Invalid,
        float? loop = null)
    {
        Elements = elements;
        Scene = scene;
        Loop = loop;
        List<int> indicesInstant = [];
        for (int i = 0; i < Elements.Length; i++)
        {
            var el = Elements[i];
            if (el.Beat == 0f && !el.Offset.HasValue && !el.Ratio.HasValue)
            {
                indicesInstant.Add(i);
            }
        }
        IndicesInstant = indicesInstant.ToArray();
    }

    public int Length => Elements.Length;

    public SceneSetElement this[int i] => Elements[i];

    public IEnumerator<SceneSetElement> GetEnumerator() => ((IEnumerable<SceneSetElement>)Elements).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();

    public bool TryResolve(CustomSceneManager sceneManager, GameObject rootObj, SceneKey scene, string key, out ISceneResolved res)
    {
        res = new SceneSetResolved(this);
        bool success = false;
        if (SceneSetElement.TryResolveArray(sceneManager, rootObj, scene, Elements, ((SceneSetResolved)res).Elements))
        {
            success = true;
        }
        return success;
    }
}

public readonly struct SceneSetElement(float beat,
    float? length = null,
    float? ratio = null,
    float? offset = null,
    bool useLength = false,
    bool applyByEnd = false)
{
    public readonly List<string> Names = [];

    public readonly float Beat = beat;
    public readonly float? Length = length;

    public readonly float? Ratio = ratio;

    public readonly float? Offset = offset;
    public readonly bool UseLength = useLength;
    public readonly bool ApplyByEnd = applyByEnd;

    public static bool TryResolveArray(CustomSceneManager sceneManager, GameObject rootObj, SceneKey scene, SceneSetElement[] elements, SceneResolvedMultiple[] elementsResolved)
    {
        bool success = false;
        for (var i = 0; i < elements.Length; i++)
        {
            var names = elements[i].Names;
            elementsResolved[i] = [];
            foreach (var name in names)
            {
                if (sceneManager.TryResolveCustomScene(rootObj, scene, name, out var innerRes))
                {
                    success = true;
                    elementsResolved[i].Elements.Add(innerRes);
                }
            }
        }
        return success;
    }
}
