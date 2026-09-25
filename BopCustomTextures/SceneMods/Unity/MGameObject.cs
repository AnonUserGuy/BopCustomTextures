using BopCustomTextures.Json;
using BopCustomTextures.Customs;
using BopCustomTextures.SceneMods.Unity.Components;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene mod <see cref="GameObject"/> definition. 
/// Includes no reference to the <see cref="GameObject"/> to modify, only a path to it.
/// </summary>
public class MGameObject : MUnityObject<GameObject>
{
    /// <summary>
    /// Temporary container of <see cref="MComponent{T}"/> definition that's still serialized. 
    /// Once the target <see cref="GameObject"/> is loaded and target <see cref="Component"/> 
    /// can be determined, will be replaced with <see cref="MComponent{T}"/>.
    /// </summary>
    /// <param name="ctx">Deserialization context.</param>
    /// <param name="name"><see cref="Component"/> name.</param>
    /// <param name="jcomponent">Serialized <see cref="MComponent{T}"/> definition.</param>
    public readonly struct UnkownMComponent(CustomJsonInitializer ctx, string name, JToken jcomponent)
    {
        public readonly CustomJsonInitializer Ctx = ctx;
        public readonly string Name = name;
        public readonly JToken JToken = jcomponent;
    }

    public string name;
    public bool isDeferred;
    public readonly List<MGameObject> childObjs = [];
    public readonly List<MGameObject> childObjsDeferred = [];
    public readonly List<UnkownMComponent> unknownComponents = [];
    public readonly List<IMComponent> components = [];

    private static readonly Regex TerminalComponentRegex = new Regex(@"^(.*)[\\/]!([^\\/]*)$", RegexOptions.Compiled);

    public override bool JsonParse(CustomJsonInitializer ctx, JToken jtoken)
    {
        /*
        while (jobj.Count == 1)
        {
            var dict = jobj.Properties().First();
            logger.LogWarning($"{name} - {dict.Name}");
            if (dict.Value.Type != JTokenType.Object ||
                dict.Name.StartsWith("!") ||
                dict.Name.StartsWith("~"))
            {
                break;
            }
            if (name.EndsWith("/") || name.EndsWith("\\"))
            {
                name += dict.Name;
            } 
            else
            {
                name += "/" + dict.Name;
            }
            logger.LogError(name);
            jobj = (JObject)dict.Value;
        }
        */

        // check if is a single component on a gameobject
        var match = TerminalComponentRegex.Match(name);
        if (match.Success)
        {
            name = match.Groups[1].Value;
            string componentName = match.Groups[2].Value;
            return AddComponent(ctx, jtoken, componentName, jtoken);
        }

        if (jtoken.Type != JTokenType.Object)
        {
            ctx.Logger.LogJsonParseError(jtoken.Path, $"GameObject \"{name}\"", $"is a {jtoken.Type} when it should be an object");
            return false;
        }

        foreach (KeyValuePair<string, JToken> dict in (JObject)jtoken)
        {
            if (dict.Key.StartsWith("!"))
            {
                string componentName = dict.Key.Substring(1);
                AddComponent(ctx, jtoken, componentName, dict.Value);
            }
            else
            {
                string childName = dict.Key;
                bool isChildDeferred = isDeferred;
                if (childName.StartsWith("~"))
                {
                    isChildDeferred = true;
                    childName = childName.Substring(1);
                }

                var mchildObj = new MGameObject{name = childName, isDeferred = isChildDeferred};

                if (!mchildObj.JsonParse(ctx, dict.Value))
                {
                    continue;
                }
                if (isChildDeferred)
                {
                    childObjsDeferred.Add(mchildObj);
                }
                else
                {
                    childObjs.Add(mchildObj);
                }
            }
        }

        if (components.Count == 0 && childObjs.Count == 0 && childObjsDeferred.Count == 0 && unknownComponents.Count == 0)
        {
            ctx.Logger.LogJsonParseError(jtoken.Path, $"GameObject \"{name}\"", "doesn't do anything");
            return false;
        }

        return true;
    }


    public bool AddComponent(CustomJsonInitializer ctx, JToken jtoken, string componentName, JToken jcomponent)
    {
        if (ctx.Mixtape.Unsafe && !MComponentParserRegistry.Instance.HasComponentRegistered(componentName))
        {
            unknownComponents.Add(new(ctx, componentName, jcomponent));
        }
        else if (MComponentParserRegistry.Instance.TryParseComponent(ctx, componentName, jcomponent, out var mcomponent))
        {
            components.Add(mcomponent);
        }
        else
        {
            ctx.Logger.LogJsonParseError(jtoken.Path, $"GameObject \"{name}\"", $"JSON Component \"{componentName}\" failed to parse");
            return false;
        }
        return true;
    }

    public override GameObject Apply(GameObject obj)
    {
        return Apply(obj, null);
    }

    /// <summary>
    /// Apply scene mod to <see cref="GameObject"/>, using a rootObj that deferred child objects can use to prevent 
    /// bad access using "..".
    /// </summary>
    /// <param name="obj"><see cref="GameObject"/> to apply scene mod to.</param>
    /// <param name="rootObj">Root <see cref="GameObject"/> of game, or <see langword="null"/>. 
    /// If not null, deferred child selectors won't be able to ascend past it with "..".</param>
    /// <returns><see cref="GameObject"/> with scene mod applied to it.</returns>
    public GameObject Apply(GameObject obj, GameObject rootObj)
    {
        for (var i = 0; i < unknownComponents.Count; i++)
        {
            var munknown = unknownComponents[i];
            if (MComponentParserRegistry.Instance.TryLateParseComponent(munknown.Ctx, munknown.Name, munknown.JToken, obj, out var mcomponent))
            {
                unknownComponents.RemoveAt(i--);
                components.Add(mcomponent);
            }
        }
        foreach (var mcomponent in components)
        {
            mcomponent.Apply(obj);
        }

        foreach (var mchildObj in childObjsDeferred)
        {
            foreach (var childObj in CustomSceneManager.FindGameObjectsInChildren(rootObj, obj, mchildObj.name))
            {
                mchildObj.Apply(childObj, rootObj);
            }
        }
        return obj;
    }
}
