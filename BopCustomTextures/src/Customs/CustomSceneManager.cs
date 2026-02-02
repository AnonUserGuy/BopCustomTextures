using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages scene mods, including loading them from the source file and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
public class CustomSceneManager(ILogger logger) : BaseCustomManager(logger)
{
    public CustomJsonInitializer jsonInitializer = new CustomJsonInitializer(logger);
    public readonly Dictionary<SceneKey, JObject> CustomScenes = [];
    public static readonly Regex PathRegex = new Regex(@"[\\/](?:level|scene)s?$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegex = new Regex(@"(\w+).json$", RegexOptions.IgnoreCase);

    public static bool IsCustomSceneDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public int LocateCustomScenes(string path, string parentPath)
    {
        int filesLoaded = 0;
        var fullFilepaths = Directory.EnumerateFiles(path);
        foreach (var fullFilepath in fullFilepaths)
        {
            var localFilepath = fullFilepath.Substring(parentPath.Length + 1);
            if (CheckIsCustomScene(fullFilepath, localFilepath))
            {
                filesLoaded++;
            }
        }
        return filesLoaded;
    }

    public bool CheckIsCustomScene(string path, string localPath)
    {
        Match match = FileRegex.Match(localPath);
        if (match.Success)
        {
            SceneKey scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene != SceneKey.Invalid)
            {
                logger.LogFileLoading($"Found custom scene: {scene}");

                LoadCustomScene(path, localPath, scene);
                return true;
            }
        }
        return false;
    }

    public void LoadCustomScene(string path, string localPath, SceneKey scene)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            MemoryStream memStream = new MemoryStream(bytes);
            using StreamReader reader = new StreamReader(memStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);
            CustomScenes[scene] = JObject.Load(jsonReader);
        }
        catch (JsonReaderException e)
        {
            logger.LogError(e);
            CustomScenes.Remove(scene);
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

    public void InitCustomScene(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        if (!CustomScenes.ContainsKey(sceneKey))
        {
            return;
        }
        logger.LogInfo($"Applying custom scene: {sceneKey}");
        GameObject rootObj = rootObjectsRef(__instance)[sceneKey];
        JObject jall = CustomScenes[sceneKey];
        foreach (KeyValuePair<string, JToken> dict in jall)
        {
            jsonInitializer.InitCustomGameObject(dict.Value, dict.Key, rootObj);
        }
    }
}