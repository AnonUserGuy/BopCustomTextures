using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BopCustomTextures;

public class CustomSceneManagement : CustomManagement
{
    public static readonly Dictionary<SceneKey, JObject> CustomScenes = [];
    public static readonly Regex PathRegex = new Regex(@"\\(?:level|scene)s?$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegex = new Regex(@"(\w+).json$", RegexOptions.IgnoreCase);

    public static bool IsCustomSceneDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public static int LocateCustomScenes(string path, string parentPath)
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

    public static bool CheckIsCustomScene(string path, string localPath)
    {
        Match match = FileRegex.Match(localPath);
        if (match.Success)
        {
            SceneKey scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene != SceneKey.Invalid)
            {
                Plugin.LogFileLoading($"Found custom scene: {scene}");

                LoadCustomScene(path, localPath, scene);
                return true;
            }
        }
        return false;
    }

    public static void LoadCustomScene(string path, string localPath, SceneKey scene)
    {
        try
        {
            byte[] bytes = ReadFile(path, localPath);
            MemoryStream memStream = new MemoryStream(bytes);
            using StreamReader reader = new StreamReader(memStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);
            CustomScenes[scene] = JObject.Load(jsonReader);
        }
        catch (JsonReaderException e)
        {
            Plugin.Logger.LogError(e);
            CustomScenes.Remove(scene);
        }
        
    }

    public static void UnloadCustomScenes()
    {
        if (CustomScenes.Count > 0)
        {
            Plugin.LogUnloading("Unloading all custom scenes");
            CustomScenes.Clear();
        }
    }

    public static void InitCustomScene(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        if (!CustomScenes.ContainsKey(sceneKey))
        {
            return;
        }
        Plugin.Logger.LogInfo($"Applying custom scene: {sceneKey}");
        GameObject rootObj = rootObjectsRef(__instance)[sceneKey];
        JObject jall = CustomScenes[sceneKey];
        foreach (KeyValuePair<string, JToken> dict in jall)
        {
            CustomInitializer.InitCustomGameObject(dict.Value, dict.Key, rootObj);
        }
    }
}