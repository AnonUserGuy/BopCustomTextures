using BopCustomTextures.Logging;
using System;
using System.IO;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages all custom assets using specific manager classes.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
/// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded</param>
public class CustomManager(ILogger logger, string tempPath) : BaseCustomManager(logger)
{
    public DateTime lastModified;
    public string lastPath;
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
        int filesLoaded = 0;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            var backup2 = false;
            if (CustomSceneManager.IsCustomSceneDirectory(subpath))
            {
                filesLoaded += sceneManager.LocateCustomScenes(subpath, path);
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
        }
        else
        {
            logger.LogInfo($"No custom assets found");
        }
    }

    public void WriteDirectory(string path)
    {
        fileManager.WriteDirectory(path);
    }

    public void ResetAll()
    {
        sceneManager.UnloadCustomScenes();
        textureManager.UnloadCustomTextures();
        fileManager.DeleteTempDirectory();
        lastPath = null;
        lastModified = default;
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
        if (cancelLoadRef(__instance))
        {
            return;
        }
        sceneManager.InitCustomScene(__instance, sceneKey);
        textureManager.InitCustomTextures(__instance, sceneKey);
    }
}
