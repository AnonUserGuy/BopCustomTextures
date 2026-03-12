using BopCustomTextures.Json;
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
    public readonly Dictionary<SceneKey, Dictionary<string, MGameObjectResolved>> CustomScenesResolved = [];
    private MixtapeLoaderCustom lastMixtapeLoader = null;
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
            CustomScenesResolved.Clear();
            lastMixtapeLoader = null;
        }
    }

    public bool TryGetCustomSceneResolved(MixtapeLoaderCustom __instance, SceneKey scene, string key, out MGameObjectResolved mobjResolved)
    {
        // check if same mixtape loader, meaning root game objects haven't changed
        if (__instance != lastMixtapeLoader)
        {
            lastMixtapeLoader = __instance;
            CustomScenesResolved.Clear();
        }

        // check if game present and game has custom scenes and game has custom scene of name key
        if (!CustomScenes.ContainsKey(scene) ||
            !CustomScenes[scene].TryGetValue(key, out var mobj) || 
            !__instance.RootObjects.TryGetValue(scene, out var rootObj))
        {
            mobjResolved = null;
            return false;
        }

        // check game has resolved some custom scenes
        if (!CustomScenesResolved.TryGetValue(scene, out var mobjsResolved))
        {
            mobjsResolved = [];
            CustomScenesResolved[scene] = mobjsResolved;
        }

        // check this custom scene has been resolved
        if (!mobjsResolved.TryGetValue(key, out mobjResolved))
        {
            mobjResolved = ResolveGameObject(rootObj, mobj);
        }
        return true;
    }

    public void InitCustomScene(MixtapeLoaderCustom __instance, SceneKey scene, string key = "")
    {
        if (!TryGetCustomSceneResolved(__instance, scene, key, out var mobjResolved))
        {
            return;
        }
        logger.LogInfo($"Applying custom scene: {scene}");
        mobjResolved.Apply();
    }

    public void InitCustomSceneDeferred(MixtapeLoaderCustom __instance, SceneKey scene, string key = "")
    {
        if (!TryGetCustomSceneResolved(__instance, scene, key, out var mobjResolved))
        {
            return;
        }
        logger.LogInfo($"Applying custom scene (deferred): {scene}");
        mobjResolved.Apply();
    }

    public void PrepareEvents(MixtapeLoaderCustom __instance, Entity[] entities)
    {
        foreach (Entity entity in entities)
        {
            PrepareEvent(__instance, entity);
        }
    }

    public void PrepareEvent(MixtapeLoaderCustom __instance, Entity entity)
    {
        if (entity.dataModel != $"{MyPluginInfo.PLUGIN_GUID}/apply scene mod")
        {
            return;
        }

        var key = entity.GetString("key");
        var sceneStr = entity.GetString("scene");
        var scene = ToSceneKeyOrInvalid(sceneStr);
        if (scene == SceneKey.Invalid)
        {
            logger.LogError($"Scene \"{sceneStr}\" is not a valid scene key");
            return;
        }
        if (!CustomScenes.ContainsKey(scene))
        {
            logger.LogError($"Cannot apply scene mod to vanilla scene {scene}");
            return;
        }
        if (!__instance.RootObjects.TryGetValue(scene, out var rootObj))
        {
            logger.LogError($"Cannot apply scene mod to missing scene {scene}");
            return;
        }
        if (TryGetCustomSceneResolved(__instance, scene, key, out var mobjResolved))
        {
            __instance.scheduler.Schedule(entity.beat, mobjResolved.Apply);
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
        string[] names = Regex.Split(path.TrimEnd(['\\','/']), @"[\\/]");
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