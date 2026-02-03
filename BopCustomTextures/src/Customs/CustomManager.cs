using BopCustomTextures.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages all custom assets using specific manager classes.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
/// <param name="pluginGUID">GUID of plugin. plugin-specific data in mixtape files will be saved at "{pluginGUID}.json"</param>
/// <param name="pluginVersion">Current version of plugin. Only used in logging</param>
/// <param name="lowestRelease">Lowest release number saved mixtapes with customs will support</param>
/// <param name="lowestVersion">Lowest version string saved mixtapes with customs will support</param>
/// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded</param>
public class CustomManager(ILogger logger, string pluginGUID, string pluginVersion, uint lowestRelease, string lowestVersion, string tempPath) : BaseCustomManager(logger)
{
    public readonly string pluginGUID = pluginGUID;
    public readonly string latestVersion = pluginVersion;
    public readonly uint lowestRelease = lowestRelease;
    public readonly string lowestVersion = lowestVersion;

    public string version;
    public uint release;
    public bool hasCustomAssets = false;

    public string lastPath;
    public DateTime lastModified;
    public bool readNecessary = true;

    public CustomSceneManager sceneManager = new CustomSceneManager(logger);
    public CustomTextureManager textureManager = new CustomTextureManager(logger);
    public CustomFileManager fileManager = new CustomFileManager(logger, tempPath);

    public void ReadDirectory(string path, bool backup)
    {
        if (!readNecessary)
        {
            return;
        }

        hasCustomAssets = GetMixtapeVersion(path);
        if (release > lowestRelease)
        {
            logger.LogEditorError($"Mixtape requires {pluginGUID} v{version}+, but you are on v{latestVersion}. You may have to update {pluginGUID} to play properly.");
        }

        int filesLoaded = 0;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            var backup2 = false;
            if (CustomSceneManager.IsCustomSceneDirectory(subpath))
            {
                filesLoaded += sceneManager.LocateCustomScenes(subpath, path, release);
                backup2 = backup;
            }
            else if (CustomTextureManager.IsCustomTextureDirectory(subpath))
            {
                filesLoaded += textureManager.LocateCustomTextures(subpath, path);
                backup2 = backup;
            }
            if (backup)
            {
                fileManager.BackupDirectory(subpath, subpath.Substring(path.Length + 1));
            }
        }
        if (filesLoaded > 0)
        {
            logger.LogInfo($"Loaded {filesLoaded} custom assets");
            if (!hasCustomAssets)
            {
                logger.LogEditorWarning("This file with custom assets is missing a \"BopCustomTextues.json\" file specifying version. " +
                    "Save this mixtape in the editor to add a \"BopCustomTextures.json\" file automatically!"
                    );
                hasCustomAssets = true;
            }
        }
        else
        {
            logger.LogInfo("No custom assets found");
        }
    }

    public void WriteDirectory(string path)
    {
        if (hasCustomAssets)
        {
            logger.LogInfo("Saving with custom files");
            fileManager.WriteDirectory(path);
            WriteMixtapeVersion(path);
        };
    }

    public void ResetAll()
    {
        sceneManager.UnloadCustomScenes();
        textureManager.UnloadCustomTextures();
        fileManager.DeleteTempDirectory();
        lastPath = null;
        lastModified = default;
        hasCustomAssets = false;
        readNecessary = true;
    }

    public void ResetIfNecessary(string path)
    {
        var modified = File.GetLastWriteTime(path);
        if (lastPath != path || lastModified != modified)
        {
            ResetAll();
        }
        else
        {
            logger.LogInfo("Avoided customs reload for reopened mixtape");
            readNecessary = false;
        }
        lastPath = path;
        lastModified = modified;
    }

    public void DeleteTempDirectory()
    {
        fileManager.DeleteTempDirectory();
    }

    public void InitScene(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        textureManager.InitCustomTextures(__instance, sceneKey);
        sceneManager.InitCustomScene(__instance, sceneKey);
    }

    public void InitScenes(MixtapeLoaderCustom __instance)
    {
        foreach (var dict in rootObjectsRef(__instance))
        {
            InitScene(__instance, dict.Key);
        }
    }

    public bool GetMixtapeVersion(string path)
    {
        string filePath = Path.Combine(path, $"{pluginGUID}.json");
        if (!File.Exists(filePath))
        {
            version = lowestVersion;
            release = lowestRelease;
            return false;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            MemoryStream memStream = new MemoryStream(bytes);
            using StreamReader reader = new StreamReader(memStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);
            var jobj = JObject.Load(jsonReader);

            if (jobj.TryGetValue("release", out var jrelease))
            {
                if (jrelease.Type == JTokenType.Integer)
                {
                    release = (uint)jrelease;
                } 
                else
                {
                    logger.LogWarning($"Release is a {jrelease.Type} when it should be an int, will treat as latest.");
                    release = lowestRelease;
                }
            } 
            else
            {
                logger.LogWarning("Version data missing release, will treat as latest.");
                release = lowestRelease;
            }
            if (jobj.TryGetValue("version", out var jversion))
            {
                if (jversion.Type == JTokenType.String)
                {
                    version = (string)jversion;
                }
                else
                {
                    logger.LogWarning($"Version is a {jversion.Type} when it should be an int, will treat as latest.");
                    version = lowestVersion;
                }
            }
            else
            {
                logger.LogWarning("Version data missing version, will treat as latest.");
                version = lowestVersion;
            }

        }
        catch (JsonReaderException e)
        {
            logger.LogError($"Error reading verison data, will treat as latest: {e}");
            version = lowestVersion;
            release = lowestRelease;
        }
        
        return true;
    }

    public void WriteMixtapeVersion(string path)
    {
        var jobj = new JObject();
        jobj["version"] = new JValue(version);
        jobj["release"] = new JValue(release);

        try
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, $"{pluginGUID}.json")))
            {
                outputFile.Write(JsonConvert.SerializeObject(jobj));
            }
        } 
        catch (Exception e)
        {
            logger.LogError($"Error writing version data: {e}");
        }
    }
}
