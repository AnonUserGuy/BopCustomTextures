using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BopCustomTextures;

public class CustomSceneManagement : CustomManagement
{
    public static readonly Dictionary<SceneKey, JObject> CustomScenes = [];
    public static readonly Regex FileRegex = new Regex(@"^(?:level|scene)s?/(\w+).json$", RegexOptions.IgnoreCase);

    public static bool CheckIsCustomScene(ZipArchiveEntry entry)
    {
        Match match = FileRegex.Match(entry.FullName);
        if (match.Success)
        {
            SceneKey scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene != SceneKey.Invalid)
            {
                Plugin.Logger.LogInfo($"Found custom scene: {scene}");
                LoadCustomScene(entry, scene);
                return true;
            }
        }
        return false;
    }

    public static void LoadCustomScene(ZipArchiveEntry entry, SceneKey scene)
    {
        try
        {
            MemoryStream memStream = ReadFile(entry);
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
            Plugin.Logger.LogInfo("Unloading all custom scenes");
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