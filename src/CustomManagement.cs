using HarmonyLib;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace BopCustomTextures;
public class CustomManagement
{
    public static readonly Dictionary<string, byte[]> OriginalFiles = [];

    public static readonly AccessTools.FieldRef<MixtapeLoaderCustom, Dictionary<SceneKey, GameObject>> rootObjectsRef =
        AccessTools.FieldRefAccess<MixtapeLoaderCustom, Dictionary<SceneKey, GameObject>>("rootObjects");


    public static void ReadDirectory(string path)
    {
        int filesLoaded = 0;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            if (CustomSceneManagement.IsCustomSceneDirectory(subpath))
            {
                filesLoaded += CustomSceneManagement.LocateCustomScenes(subpath, path);
            }
            else if (CustomTextureManagement.IsCustomTextureDirectory(subpath))
            {
                filesLoaded += CustomTextureManagement.LocateCustomTextures(subpath, path);
            }
        }

        Plugin.Logger.LogInfo($"Loaded {filesLoaded} custom assets");
    }

    public static void WriteDirectory(string path)
    {
        if (HasFiles())
        {
            Plugin.Logger.LogInfo("Saving with custom files");
            WriteFiles(path);
        }
    }

    public static void ResetAll()
    {
        CustomSceneManagement.UnloadCustomScenes();
        CustomTextureManagement.UnloadCustomTextures();
        UnloadFiles();
    }

    public static void InitScene(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        CustomSceneManagement.InitCustomScene(__instance, sceneKey);
        CustomTextureManagement.InitCustomTextures(__instance, sceneKey);
    }


    protected static byte[] ReadFile(string path, string localPath)
    {
        OriginalFiles[localPath] = File.ReadAllBytes(path);
        return OriginalFiles[localPath];
    }

    public static void WriteFiles(string path)
    {
        foreach (var w in OriginalFiles)
        {
            string fullpath = Path.Combine(path, w.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(fullpath));
            File.WriteAllBytes(fullpath, w.Value);
        }
    }

    public static void UnloadFiles()
    {
        OriginalFiles.Clear();
    }

    public static bool HasFiles()
    {
        return OriginalFiles.Count > 0;
    }

    protected static SceneKey ToSceneKeyOrInvalid(string name)
    {
        string[] namesAffixed =
        [
        name,
        name + "Custom",
        name + "Mixtape"
        ];
        foreach (string name2 in namesAffixed)
        {
            SceneKey[] sceneKeys = MixtapeLoaderCustom.allSceneKeys;
            for (int j = 0; j < sceneKeys.Length; j++)
            {
                SceneKey result = sceneKeys[j];
                string keyName = result.ToString();
                if (string.Equals(name2, keyName, StringComparison.OrdinalIgnoreCase))
                {
                    return result;
                }
            }
        }
        return SceneKey.Invalid;
    }
}
