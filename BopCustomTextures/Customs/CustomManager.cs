using BopCustomTextures.Config;
using BopCustomTextures.Logging;
using BopCustomTextures.EventTemplates;
using SFB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages all custom assets using specific manager classes.
/// </summary>
public class CustomManager : BaseCustomManager
{
    public static readonly List<string> menuOptions = [
        "Copy Customs from File",
        "Copy Customs from Folder",
        "Reload Custom Assets"
    ];
    public enum MenuOption
    {
        OpenCustomsArchive,
        OpenCustomsDirectory,
        ReloadCustomAssets
    }

    public string version;
    public uint release;
    public bool hasCustomAssets = false;

    public string lastPath;
    public DateTime lastModified;
    public bool readNecessary = true;
    public bool interruptLoad = false;

    public CustomSceneManager sceneManager;
    public CustomTextureManager textureManager;
    public CustomVariantNameManager variantManager;
    public CustomFileManager fileManager;
    
    public Dictionary<string, List<MixtapeEventTemplate>> entities;

    public static readonly Regex PathRegex = new Regex(@"[\\/](?:res(?:ource)?s?|BopCustomTextures)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <param name="logger">Plugin-specific logger.</param>
    /// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded.</param>
    /// <param name="sceneModTemplate">Mixtape event template for applying scene mods.</param>
    /// <param name="textureTemplates">Mixtape event templates concerning custom textures.</param>
    /// <param name="entities">List of all mixtape event categories and events.</param>
    public CustomManager(ILogger logger, 
        string tempPath, 
        MixtapeEventTemplate sceneModTemplate, 
        MixtapeEventTemplate[] textureTemplates,
        Dictionary<string, List<MixtapeEventTemplate>> entities) : base(logger)
    {
        variantManager = new CustomVariantNameManager(logger);
        sceneManager = new CustomSceneManager(logger, variantManager, sceneModTemplate);
        textureManager = new CustomTextureManager(logger, variantManager, textureTemplates);
        fileManager = new CustomFileManager(logger, tempPath);
        this.entities = entities;
    }

    public static bool IsCustomResourceDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public void CheckVersionThenReadDirectory(string path, bool backup, bool upgrade, OutdatedPluginHandling outdatedPluginHandling, Display displayEventTemplates, int eventTemplatesIndex)
    {
        if (!readNecessary)
        {
            interruptLoad = false;
            return;
        }

        hasCustomAssets = GetMixtapeVersion(path);
        if (release > BopCustomTexturesPlugin.LowestRelease)
        {
            if (outdatedPluginHandling == OutdatedPluginHandling.LoadVanilla)
            {
                logger.LogOutdatedPlugin(
                    $"Mixtape requires {MyPluginInfo.PLUGIN_GUID} v{version}+, " +
                    $"but you are on v{MyPluginInfo.PLUGIN_VERSION}, so loading custom assets was cancelled."
                );
                interruptLoad = false;
                return;
            }
            logger.LogOutdatedPlugin(
                $"Mixtape requires {MyPluginInfo.PLUGIN_GUID} v{version}+, " +
                $"but you are on v{MyPluginInfo.PLUGIN_VERSION}. You may have to update {MyPluginInfo.PLUGIN_GUID} to play properly."
            );
            if (outdatedPluginHandling == OutdatedPluginHandling.ShowDisclaimer)
            {
                interruptLoad = true;
                return;
            }
        }
        else if (release < BopCustomTexturesPlugin.LowestRelease && backup && upgrade)
        {
            logger.LogUpgradeMixtape(
                $"Mixtape was made for {MyPluginInfo.PLUGIN_GUID} v{version}, " +
                $"while you are on v{MyPluginInfo.PLUGIN_VERSION}. Save this mixtape in the editor to update its version!"
            );
        }
        
        ReadDirectory(path, backup);
        UpdateEventTemplates(displayEventTemplates, eventTemplatesIndex);
        interruptLoad = false;
        return;
    }

    public void ReadDirectory(string path, bool backup = false)
    {
        int filesLoaded = 0;
        bool hasResourceFolder = false;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            if (IsCustomResourceDirectory(subpath))
            {
                hasResourceFolder = true;
                filesLoaded += LocateResources(subpath, path, backup);
            }
        }
        if (!hasResourceFolder)
        {
            filesLoaded += LocateResources(path, path, backup);
        }

        if (filesLoaded > 0)
        {
            logger.LogInfo($"Loaded {filesLoaded} custom assets");
            if (!hasCustomAssets && backup)
            {
                logger.LogUpgradeMixtape(
                    "This mixtape with custom assets is missing a \"BopCustomTextues.json\" file specifying version. " +
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

    public void ReadArchive(string path, bool backup = false)
    {
        string rootPath = null;
        try
        {
            rootPath = fileManager.ExtractArchiveToTempDirectory(path, Path.GetFileNameWithoutExtension(path));
            ReadDirectory(rootPath, backup);
        } 
        catch (Exception e)
        {
            logger.LogError(e);
        } 
        finally
        {
            if (!string.IsNullOrEmpty(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    public void ReadPath(string path, bool backup = false)
    {
        if (File.Exists(path))
        {
            ReadArchive(path, backup);
        } 
        else if (Directory.Exists(path) && 
            File.Exists(Path.Combine(path, "mixtape.json"))) 
        {
            ReadDirectory(path, backup);
        }
        else
        {
            logger.LogError($"Path is not a .bop file or directory: {path}");
        }
    }

    public void ReadLastPath(bool backup = false)
    {
        ReadPath(lastPath, backup);
    }

    public int LocateResources(string path, string parentPath, bool backup)
    {
        int filesLoaded = 0;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            if (CustomTextureManager.IsCustomTextureDirectory(subpath))
            {
                filesLoaded += textureManager.LocateCustomTextures(subpath);
                if (backup)
                {
                    fileManager.BackupDirectory(subpath, subpath.Substring(parentPath.Length + 1));
                }
            }
        }
        foreach (var subpath in subpaths)
        {
            if (CustomSceneManager.IsCustomSceneDirectory(subpath))
            {
                filesLoaded += sceneManager.LocateCustomScenes(subpath, release);
                if (backup)
                {
                    fileManager.BackupDirectory(subpath, subpath.Substring(parentPath.Length + 1));
                }
            }
        }
        return filesLoaded;
    }

    public void WriteDirectory(string path, bool upgrade)
    {
        if (hasCustomAssets)
        {
            logger.LogInfo("Saving with custom files");
            fileManager.WriteDirectory(path);
            WriteMixtapeVersion(path, upgrade);
        };
    }
    
    // NOTE: Bits & Bops currently supports RIQ v1 only.
    public void CheckVersionThenReadRiqArchive(string riqPath, bool backup, bool upgrade, OutdatedPluginHandling outdatedPluginHandling, Display displayEventTemplates, int eventTemplatesIndex)
    {
        string rootPath = null;
        if (!readNecessary)
        {
            return;
        }
        try
        {
            rootPath = fileManager.ExtractArchiveToTempDirectory(riqPath, Path.GetFileNameWithoutExtension(riqPath));
            CheckVersionThenReadDirectory(rootPath, backup, upgrade, outdatedPluginHandling, displayEventTemplates, eventTemplatesIndex);
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to load custom assets from RIQ v1 file: {e}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    // NOTE: Bits & Bops currently supports RIQ v1 only.
    public void SaveAsRiq(string riqPath, bool upgrade)
    {
        if (!hasCustomAssets)
        {
            return;
        }

        try
        {
            var rootPath = fileManager.ExtractArchiveToTempDirectory(riqPath, Path.GetDirectoryName(riqPath));
            if (fileManager.WriteDirectory(rootPath))
            {
                logger.LogInfo("Saving RIQ v1 with custom files");
                WriteMixtapeVersion(rootPath, upgrade);
                fileManager.PackDirectoryToArchive(rootPath, riqPath);
            }
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to save custom assets into RIQ v1 file: {e}");
        }
    }

    public void Unload()
    {
        sceneManager.UnloadCustomScenes();
        textureManager.UnloadCustomTextures();
        variantManager.UnloadCustomTextureVariants();
        fileManager.DeleteTempDirectory();
    }

    public void ResetAll(Display displayEventTemplates, int eventTemplatesIndex)
    {
        Unload();
        lastPath = null;
        lastModified = default;
        hasCustomAssets = false;
        readNecessary = true;
        UpdateEventTemplates(displayEventTemplates, eventTemplatesIndex);
    }

    public void ResetIfNecessary(string path, Display displayEventTemplates, int eventTemplatesIndex)
    {
        var modified = File.GetLastWriteTime(path);
        if (lastPath != path || lastModified != modified)
        {
            ResetAll(displayEventTemplates, eventTemplatesIndex);
        }
        else
        {
            logger.LogInfo("Avoided customs reload for reopened mixtape");
            readNecessary = false;
        }
        lastPath = path;
        lastModified = modified;
    }

    public void ResetAndReload(string path, bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        var modified = File.GetLastWriteTime(lastPath);
        if (lastPath != path || modified != lastModified)
        {
            Unload();
            ReadPath(path, backup);
            UpdateEventTemplates(displayEventTemplates, eventTemplatesIndex);
        } 
        else
        {
            logger.LogInfo("Custom assets appear to be unmodified");
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
        sceneManager.InitCustomScene(__instance, sceneKey);
    }

    public void Prepare(MixtapeLoaderCustom __instance)
    {
        foreach (var dict in rootObjectsRef(__instance))
        {
            textureManager.InitCustomTextures(__instance, dict.Key);
            sceneManager.InitCustomSceneDeferred(__instance, dict.Key);
        }
        PrepareEvents(__instance, entitiesRef(__instance));
    }

    public void PrepareEvents(MixtapeLoaderCustom __instance, Entity[] entities)
    {
        sceneManager.PrepareEvents(__instance, entities);
        textureManager.PrepareEvents(__instance, entities);
    }

    public void UpdateEventTemplates(Display displayEventTemplates, int eventTemplatesIndex)
    {
        bool needsTemplates = 
            sceneManager.UpdateEventTemplates() |
            textureManager.UpdateEventTemplates();

        switch (displayEventTemplates)
        {
            case Display.Never:
                entities.Remove(MyPluginInfo.PLUGIN_GUID);
                break;
            case Display.WhenActive:
                if (needsTemplates) AddEventTemplates(eventTemplatesIndex);
                else entities.Remove(MyPluginInfo.PLUGIN_GUID);
                break;
            case Display.Always:
                AddEventTemplates(eventTemplatesIndex);
                break;
        }
    }

    public void AddEventTemplates(int index)
    {
        if (entities.ContainsKey(MyPluginInfo.PLUGIN_GUID))
        {
            return;
        }
        var list = entities.ToList();
        if (index > list.Count || index < 1)
        {
            index = list.Count;
        }
        list.Insert(index, new KeyValuePair<string, List<MixtapeEventTemplate>>(MyPluginInfo.PLUGIN_GUID, new List<MixtapeEventTemplate>(BopCustomTexturesEventTemplates.templates)));
        entities.Clear();
        foreach (var pair in list)
        {
            entities[pair.Key] = pair.Value;
        }
    }

    public void HandleMenuOption(MixtapeEditorScript __instance, MenuOption option, bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        switch (option)
        {
            case MenuOption.OpenCustomsArchive:
                FileOpenCustomsArchive(__instance, backup, displayEventTemplates, eventTemplatesIndex);
                break;
            case MenuOption.OpenCustomsDirectory:
                FileOpenCustomsDirectory(__instance, backup, displayEventTemplates, eventTemplatesIndex);
                break;
            case MenuOption.ReloadCustomAssets:
                ResetAndReload(lastPath, backup, displayEventTemplates, eventTemplatesIndex);
                break;
        }
    }

    public void FileOpenCustomsArchive(MixtapeEditorScript __instance, bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        __instance.StartCoroutine(OpenCustomsArchive(backup, displayEventTemplates, eventTemplatesIndex));
    }

    public IEnumerator OpenCustomsArchive(bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        ExtensionFilter[] extensions =
        [
            new ExtensionFilter("All Mixtape Files", "bop", "riq", "zip"),
            new ExtensionFilter("Mixtape Files", "bop"),
            new ExtensionFilter("RIQ Files", "riq"),
            new ExtensionFilter("Zip Files", "zip"),

        ];
        string path = null;
        bool complete = false;
        yield return null;
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", extensions, multiselect: false, delegate (string[] paths)
        {
            complete = true;
            if (paths.Length != 0 && paths[0] != "")
            {
                path = paths[0];
            }
        });
        while (!complete)
        {
            yield return null;
        }
        if (path != null)
        {
            ResetAndReload(path, backup, displayEventTemplates, eventTemplatesIndex);
        }
    }


    public void FileOpenCustomsDirectory(MixtapeEditorScript __instance, bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        __instance.StartCoroutine(OpenCustomsDirectory(backup, displayEventTemplates, eventTemplatesIndex));
    }

    public IEnumerator OpenCustomsDirectory(bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        string path = null;
        bool complete = false;
        yield return null;
        StandaloneFileBrowser.OpenFolderPanelAsync("Open Folder", "", multiselect: false, delegate (string[] paths)
        {
            complete = true;
            if (paths.Length != 0 && paths[0] != "")
            {
                path = paths[0];
            }
        });
        while (!complete)
        {
            yield return null;
        }
        if (path != null)
        {
            ResetAndReload(path, backup, displayEventTemplates, eventTemplatesIndex);
        }
    }

    public bool GetMixtapeVersion(string path)
    {
        string filePath = Path.Combine(path, $"{MyPluginInfo.PLUGIN_GUID}.json");
        if (!File.Exists(filePath))
        {
            version = BopCustomTexturesPlugin.LowestVersion;
            release = BopCustomTexturesPlugin.LowestRelease;
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
                    release = BopCustomTexturesPlugin.LowestRelease;
                }
            } 
            else
            {
                logger.LogWarning("Version data missing release, will treat as latest.");
                release = BopCustomTexturesPlugin.LowestRelease;
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
                    version = BopCustomTexturesPlugin.LowestVersion;
                }
            }
            else
            {
                logger.LogWarning("Version data missing version, will treat as latest.");
                version = BopCustomTexturesPlugin.LowestVersion;
            }

        }
        catch (JsonReaderException e)
        {
            logger.LogError($"Error reading verison data, will treat as latest: {e}");
            version = BopCustomTexturesPlugin.LowestVersion;
            release = BopCustomTexturesPlugin.LowestRelease;
        }
        
        return true;
    }

    public void WriteMixtapeVersion(string path, bool upgrade)
    {
        var jobj = new JObject();
        jobj["version"] = new JValue(upgrade ? BopCustomTexturesPlugin.LowestVersion : version);
        jobj["release"] = new JValue(upgrade ? BopCustomTexturesPlugin.LowestRelease : release);

        try
        {
            using StreamWriter outputFile = new StreamWriter(Path.Combine(path, $"{MyPluginInfo.PLUGIN_GUID}.json"));
            outputFile.Write(JsonConvert.SerializeObject(jobj));
        } 
        catch (Exception e)
        {
            logger.LogError($"Error writing version data: {e}");
        }
    }
}
