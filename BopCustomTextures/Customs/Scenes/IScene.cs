using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BopCustomTextures.Customs.Scenes;

public interface IScene
{
    public bool TryResolve(CustomSceneManager sceneManager, GameObject rootObj, SceneKey scene, string key, out ISceneResolved res);
}

public class SceneMultiple(int capacity = 0) : IScene, IEnumerable<IScene>
{
    public readonly List<IScene> Elements = new(capacity);

    public int Count => Elements.Count;

    public IScene this[int i] { get => Elements[i]; set => Elements[i] = value; }

    public static SceneMultiple Add(IScene sceneA, IScene sceneB)
    {
        if (sceneA is not SceneMultiple sceneAMultiple)
        {
            sceneAMultiple = [];
            sceneAMultiple.Elements.Add(sceneA);
        }
        if (sceneB is SceneMultiple sceneBMultiple)
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

    public IEnumerator<IScene> GetEnumerator() => Elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();

    public bool TryResolve(CustomSceneManager sceneManager, GameObject rootObj, SceneKey scene, string key, out ISceneResolved res)
    {
        res = new SceneResolvedMultiple();
        bool success = false;
        for (int i = 0; i < Count; i++)
        {
            if (sceneManager.TryResolveCustomScene(rootObj, scene, key, out var mobjResolved))
            {
                success = true;
                ((SceneResolvedMultiple)res).Elements.Add(mobjResolved);
            }
        }
        return success;
    }
}