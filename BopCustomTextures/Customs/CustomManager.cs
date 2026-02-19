using BopCustomTextures.Config;
using BopCustomTextures.Logging;
using BopCustomTextures.EventTemplates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages all custom assets using specific manager classes.
/// </summary>
public class CustomManager : BaseCustomManager
{
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
        string tempPath = Path.GetTempFileName();
        File.Delete(tempPath);
        Directory.CreateDirectory(tempPath);
        try
        {
            ZipFile.ExtractToDirectory(path, tempPath);
            ReadDirectory(tempPath, backup);
        } 
        catch (Exception e)
        {
            logger.LogError(e);
        } 
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
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

    public void ResetAll(Display displayEventTemplates, int eventTemplatesIndex)
    {
        sceneManager.UnloadCustomScenes();
        textureManager.UnloadCustomTextures();
        variantManager.UnloadCustomTextureVariants();
        fileManager.DeleteTempDirectory();
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
