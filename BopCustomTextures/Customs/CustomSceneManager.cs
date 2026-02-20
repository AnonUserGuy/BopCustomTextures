using BopCustomTextures.SceneMods;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages scene mods, including loading them from the source file and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Passed to CustomJsonInitializer.</param>
/// <param name="mixtapeEventTemplate">Mixtape event template for applying scene mods.</param>
public class CustomSceneManager(ILogger logger, CustomVariantNameManager variantManager, MixtapeEventTemplate mixtapeEventTemplate) : BaseCustomManager(logger)
{
    public MixtapeEventTemplate mixtapeEventTemplate = mixtapeEventTemplate;
    public CustomJsonInitializer jsonInitializer = new CustomJsonInitializer(logger, variantManager);
    public readonly Dictionary<SceneKey, Dictionary<string, MGameObject>> CustomScenes = [];
    public static readonly Regex PathRegex = new Regex(@"[\\/](?:level|scene)s?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static readonly Regex FileRegex = new Regex(@"(\w+).jsonc?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsCustomSceneDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public IEnumerable<string> LocateCustomScenes(string path, int index, uint release)
    {
        var filepaths = Directory.EnumerateFiles(path);
        foreach (var filepath in filepaths)
        {
            if (CheckIsCustomScene(filepath, release))
            {
                string localPath = filepath.Substring(index);
                yield return localPath;
            }
        }
    }

    public bool CheckIsCustomScene(string path, uint release)
    {
        Match match = FileRegex.Match(path);
        if (match.Success)
        {
            SceneKey scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene != SceneKey.Invalid)
            {
                logger.LogFileLoading($"Found custom scene: {scene}");

                LoadCustomScene(path, scene, release);
                return true;
            } 
        }
        return false;
    }

    public void LoadCustomScene(string path, SceneKey scene, uint release)
    {
        JObject jobj;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            MemoryStream memStream = new MemoryStream(bytes);
            using StreamReader reader = new StreamReader(memStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);

            jobj = JObject.Load(jsonReader);
        }
        catch (JsonReaderException e)
        {
            logger.LogError(e);
            return;
        }
        if (CustomScenes.ContainsKey(scene))
        {
            logger.LogWarning($"Duplicate custom scene definition for scene {scene}");
        }
        CustomScenes[scene] = [];
        bool isSimple = true;
        if (release >= 2)
        {
            if (jsonInitializer.TryGetJObject(jobj, "init", out var jinit))
            {
                isSimple = false;
                CustomScenes[scene][""] = jsonInitializer.InitGameObject(jinit, scene);
            }
            if (jsonInitializer.TryGetJObject(jobj, "events", out var jevents))
            {
                isSimple = false;
                foreach (KeyValuePair<string, JToken> dict in jevents)
                {
                    if (dict.Value.Type == JTokenType.Object)
                    {
                        CustomScenes[scene][dict.Key] = jsonInitializer.InitGameObject((JObject)dict.Value, scene);
                    }
                    else
                    {
                        logger.LogWarning($"Event \"{dict.Key}\" in {scene} is a {jinit.Type} when it should be an Object.");
                    }
                }
            }
        }
        if (isSimple)
        {
            CustomScenes[scene][""] = jsonInitializer.InitGameObject(jobj, scene);
        }
    }

    public void UnloadCustomScenes()
    {
        if (CustomScenes.Count > 0)
        {
            logger.LogUnloading("Unloading all custom scenes");
            CustomScenes.Clear();
        }
    }

    public void InitCustomScene(MixtapeLoaderCustom __instance, SceneKey sceneKey, string key = "")
    {
        if (!CustomScenes.ContainsKey(sceneKey) ||
            !__instance.RootObjects.TryGetValue(sceneKey, out var rootObj) ||
            !CustomScenes[sceneKey].TryGetValue(key, out var mobj))
        {
            return;
        }
        logger.LogInfo($"Applying custom scene: {sceneKey}");
        ResolveGameObject(rootObj, mobj).Apply();
    }

    public void InitCustomSceneDeferred(MixtapeLoaderCustom __instance, SceneKey sceneKey, string key = "")
    {
        if (!CustomScenes.ContainsKey(sceneKey) ||
            !__instance.RootObjects.TryGetValue(sceneKey, out var rootObj) ||
            !CustomScenes[sceneKey].TryGetValue(key, out var mobj))
        {
            return;
        }
        logger.LogInfo($"Applying custom scene (deferred): {sceneKey}");
        ResolveGameObject(rootObj, mobj).Apply();
    }

    public void PrepareEvents(MixtapeLoaderCustom __instance, Entity[] entities)
    {
        var mobjsResolved = new Dictionary<SceneKey, Dictionary<string, MGameObjectResolved>>();
        foreach (var pair in CustomScenes)
        {
            if (!__instance.RootObjects.TryGetValue(pair.Key, out var rootObj))
            {
                continue;
            }
            mobjsResolved[pair.Key] = new Dictionary<string, MGameObjectResolved>();
            foreach (var eventPair in pair.Value)
            {
                mobjsResolved[pair.Key][eventPair.Key] = ResolveGameObject(rootObj, eventPair.Value);
            }
        }
        foreach (Entity entity in entities)
        {
            if (entity.dataModel == $"{MyPluginInfo.PLUGIN_GUID}/apply scene mod")
            {
                var key = entity.GetString("key");
                var sceneStr = entity.GetString("scene");
                var scene = ToSceneKeyOrInvalid(sceneStr);
                if (scene == SceneKey.Invalid)
                {
                    logger.LogError($"Scene \"{sceneStr}\" is not a valid scene key");
                    continue;
                }
                if (!CustomScenes.ContainsKey(scene))
                {
                    logger.LogError($"Cannot apply scene mod to vanilla scene {scene}");
                    continue;
                }
                if (!__instance.RootObjects.TryGetValue(scene, out var rootObj))
                {
                    logger.LogError($"Cannot apply scene mod to missing scene {scene}");
                    continue;
                }
                if (mobjsResolved[scene].TryGetValue(key, out var mobjResolved))
                {
                    __instance.scheduler.Schedule(entity.beat, mobjResolved.Apply);
                }
            }
        }
    }

    public bool UpdateEventTemplates()
    {
        if (CustomScenes.Count < 1)
        {
            mixtapeEventTemplate.properties["scene"] = "";
            return false;
        }
        else
        {
            mixtapeEventTemplate.properties["scene"] = new MixtapeEventTemplates.ChoiceField<string>(
                CustomScenes.Keys.Select(FromSceneKeyOrInvalid).ToArray());
            return true;
        }
    }


    public MGameObjectResolved ResolveGameObject(GameObject obj, MGameObject mobj)
    {
        var mobjResolved = new MGameObjectResolved(mobj, obj);
        var mchildObjsResolved = new List<MGameObjectResolved>();
        foreach (var mchildObj in mobj.childObjs)
        {
            bool found = false;
            foreach (var childObj in FindGameObjectsInChildren(obj, mchildObj.name))
            {
                found = true;
                var mchildObjResolved = ResolveGameObject(childObj, mchildObj);
                mchildObjsResolved.Add(mchildObjResolved);
            }
            if (!found)
            {
                logger.LogWarning($"Couldn't find gameObject \"{mchildObj.name}\" in \"{obj.name}\"");
            }
        }
        mobjResolved.childObjs = mchildObjsResolved.ToArray();
        return mobjResolved;
    }

    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject obj, string path)
    {
        string[] names = Regex.Split(path, @"[\\/]");
        return FindGameObjectsInChildren(obj, names);
    }
    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject rootObj, string[] names, int i = 0)
    {
        for (var j = 0; j < rootObj.transform.childCount; j++)
        {
            var obj = rootObj.transform.GetChild(j).gameObject;
            if (Regex.IsMatch(obj.name, WildCardToRegex(names[i])))
            {
                if (i == names.Length - 1)
                {
                    yield return obj;
                }
                else
                {
                    foreach (var childObj in FindGameObjectsInChildren(obj, names, i + 1))
                    {
                        yield return childObj;
                    }
                }
            }
        }
    }
    private static string WildCardToRegex(string value)
    {
        return "^" + Regex.Escape(value).Replace(@"\?", ".").Replace(@"\*", ".*") + "$";
    }
}