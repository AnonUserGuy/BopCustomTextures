using HarmonyLib;
using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace BopCustomTextures;
public class CustomManagement
{
    public static readonly Dictionary<string, byte[]> OriginalFiles = [];
    public static readonly AccessTools.FieldRef<MixtapeLoaderCustom, Dictionary<SceneKey, GameObject>> rootObjectsRef =
        AccessTools.FieldRefAccess<MixtapeLoaderCustom, Dictionary<SceneKey, GameObject>>("rootObjects");

    protected static MemoryStream ReadFile(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        MemoryStream memStream = new MemoryStream();
        stream.CopyTo(memStream);
        OriginalFiles[entry.FullName] = memStream.GetBuffer();
        memStream.Position = 0;
        return memStream;
    }

    public static void WriteFiles(ZipArchive archive)
    {
        foreach (var w in OriginalFiles)
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(archive.CreateEntry(w.Key).Open()))
            {
                binaryWriter.Write(w.Value);
            }
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
