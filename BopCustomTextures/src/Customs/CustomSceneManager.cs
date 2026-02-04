using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;
using System.Linq;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages scene mods, including loading them from the source file and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
public class CustomSceneManager(ILogger logger, MixtapeEventTemplate sceneModTemplate) : BaseCustomManager(logger)
{
    public MixtapeEventTemplate sceneModTemplate = sceneModTemplate;
    public CustomJsonInitializer jsonInitializer = new CustomJsonInitializer(logger);
    public readonly Dictionary<SceneKey, Dictionary<string, JObject>> CustomScenes = [];
    public static readonly Regex PathRegex = new Regex(@"[\\/](?:level|scene)s?$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegex = new Regex(@"(\w+).json$", RegexOptions.IgnoreCase);

    public static bool IsCustomSceneDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public int LocateCustomScenes(string path, string parentPath, uint release)
    {
        int filesLoaded = 0;
        var fullFilepaths = Directory.EnumerateFiles(path);
        foreach (var fullFilepath in fullFilepaths)
        {
            var localFilepath = fullFilepath.Substring(parentPath.Length + 1);
            if (CheckIsCustomScene(fullFilepath, localFilepath, release))
            {
                filesLoaded++;
            }
        }
        return filesLoaded;
    }

    public bool CheckIsCustomScene(string path, string localPath, uint release)
    {
        Match match = FileRegex.Match(localPath);
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
            CustomScenes.Remove(scene);
            return;
        }

        CustomScenes[scene] = new Dictionary<string, JObject>();
        if (release < 2)
        {
            CustomScenes[scene][""] = jobj;
        } 
        else
        {
            bool isSimple = true;
            if (jsonInitializer.TryGetJObject(jobj, "init", out var jinit))
            {
                isSimple = false;
                CustomScenes[scene][""] = jinit;
            }
            if (jsonInitializer.TryGetJObject(jobj, "events", out var jevents))
            {
                isSimple = false;
                foreach (KeyValuePair<string, JToken> dict in jevents)
                {
                    if (dict.Value.Type == JTokenType.Object)
                    {
                        CustomScenes[scene][dict.Key] = (JObject)dict.Value;
                    }
                    else
                    {
                        logger.LogWarning($"Event \"{dict.Key}\" in {scene} is a {jinit.Type} when it should be an Object.");
                    }
                }
            }
            if (isSimple)
            {
                CustomScenes[scene][""] = jobj;
            }
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
        if (!CustomScenes.ContainsKey(sceneKey))
        {
            return;
        }
        logger.LogInfo($"Applying custom scene: {sceneKey}");
        if (!rootObjectsRef(__instance).TryGetValue(sceneKey, out var rootObj))
        {
            logger.LogError($"Cannot apply scene mod to missing scene {sceneKey}");
            return;
        }
        if (!CustomScenes[sceneKey].TryGetValue(key, out var jall))
        {
            logger.LogError($"{sceneKey} does not have a scene mod of the key \"{key}\"");
            return;
        }

        jsonInitializer.InitCustomGameObject(jall, rootObj);
    }

    public void ScheduleCustomScene(double beat, MixtapeLoaderCustom __instance, SceneKey sceneKey, string key = "")
    {
        if (!CustomScenes.ContainsKey(sceneKey))
        {
            return;
        }
        if (!rootObjectsRef(__instance).TryGetValue(sceneKey, out var rootObj))
        {
            logger.LogError($"Cannot apply scene mod to missing scene {sceneKey}");
            return;
        }
        if (!CustomScenes[sceneKey].TryGetValue(key, out var jall))
        {
            logger.LogError($"{sceneKey} does not have a scene mod of the key \"{key}\"");
            return;
        }
        __instance.scheduler.Schedule(beat, delegate
        {
            jsonInitializer.InitCustomGameObject(jall, rootObj);
        });
    }

    public void UpdateEventTemplates()
    {
        if (CustomScenes.Count < 1)
        {
            sceneModTemplate.properties["scene"] = "";
        }
        else
        {
            // TODO: "Custom" and "Mixtape" is being added to the end, which wouldn't be very space efficient in the long run
            sceneModTemplate.properties["scene"] = new MixtapeEventTemplates.ChoiceField<string>(
                CustomScenes.Keys.Select(t => t.ToString()).ToArray());
        }
    }
}