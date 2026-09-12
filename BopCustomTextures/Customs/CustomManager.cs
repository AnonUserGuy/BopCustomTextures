using BopCustomTextures.Config;
using BopCustomTextures.Scripts;
using BopCustomTextures.EventTemplates;
using BopCustomTextures.AccessExtensions;
using SFB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Display = BopCustomTextures.Config.Display;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages all custom assets using specific manager classes.
/// </summary>
public class CustomManager : BaseCustomManager
{

    private bool lastCopyActive = false;
    private bool lastReloadActive = false;
    private int lastTemplatesIndex = -1;
    public MixtapeEventTemplate EditorPropertiesTemplate;
    private MixtapeEventScript EditorPropertiesEvent;
    public MixtapeEventTemplate MixtapePropertiesTemplate;
    private MixtapeEventScript MixtapePropertiesEvent;

    public int ModdedCategoryIndex = -1;
    private string HijackedCategorySelected;
    public BopCustomTexturesButton MixtapeCategoryButton = null;

    private const int VersionMaxLength = 50;
    public static readonly Regex VersionRegex = new Regex("<.*?>", RegexOptions.Compiled);
    public static readonly Regex PathRegex = new Regex(@"[\\/](?:res(?:ource)?s?|BopCustomTextures)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private string _version;
    public string Version
    {
        get
        {
            if (MixtapePropertiesEvent != null)
            {
                var version = MixtapePropertiesEvent.Entity.GetString("version");
                if (version != _version)
                {
                    _version = version;
                }
            }
            return _version;
        }
        set
        {
            _version = value;
            if (MixtapePropertiesEvent != null)
            {
                MixtapePropertiesEvent.Entity.SetString("version", value);
            }
        }
    }
    private uint _release;
    public uint Release
    {
        get
        {
            if (MixtapePropertiesEvent != null)
            {
                var release = (uint)MixtapePropertiesEvent.Entity.GetInt("release");
                if (release != _release)
                {
                    _release = release;
                }
            }
            return _release;
        }
        set
        {
            _release = value;
            if (MixtapePropertiesEvent != null)
            {
                MixtapePropertiesEvent.Entity.SetInt("release", (int)value);
            }
        }
    }
    public bool HasCustomAssets = false;

    private string _lastPath;
    public string LastPath
    {
        get
        {
            if (EditorPropertiesEvent != null) {
                var lastPath = EditorPropertiesEvent.Entity.GetString("last path");
                if (lastPath != _lastPath && lastPath != "")
                {
                    _lastPath = lastPath;
                }
            }
            return _lastPath;
        }
        set
        {
            _lastPath = value;
            if (EditorPropertiesEvent != null)
            {
                EditorPropertiesEvent.Entity.SetString("last path", value);
            }
        }
    }
    public DateTime LastModified;
    public bool ReadNecessary = true;
    public bool InterruptLoad = false;

    public ConfigManager ConfigManager;

    public CustomSceneManager SceneManager;
    public CustomTextureManager TextureManager;
    public CustomVariantNameManager VariantManager;
    public CustomFileManager FileManager;
    
    public Dictionary<string, List<MixtapeEventTemplate>> Entities;


    /// <param name="logger">Plugin-specific logger.</param>
    /// <param name="configManager">BopCustomTextures configuration manager</param>
    /// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded.</param>
    /// <param name="sceneModTemplate">Mixtape event template for applying scene mods.</param>
    /// <param name="textureTemplates">Mixtape event templates concerning custom textures.</param>
    /// <param name="entities">List of all mixtape event categories and events.</param>
    public CustomManager(ILogger logger, ConfigManager configManager,
        string tempPath, 
        MixtapeEventTemplate sceneModTemplate, 
        MixtapeEventTemplate[] textureTemplates,
        MixtapeEventTemplate editorPropertiesTemplate,
        MixtapeEventTemplate mixtapePropertiesTemplate,
        Dictionary<string, List<MixtapeEventTemplate>> entities) : base(logger)
    {
        ConfigManager = configManager;
        VariantManager = new CustomVariantNameManager(logger);
        SceneManager = new CustomSceneManager(logger, VariantManager, sceneModTemplate);
        TextureManager = new CustomTextureManager(logger, VariantManager, textureTemplates);
        FileManager = new CustomFileManager(logger, tempPath);
        EditorPropertiesTemplate = editorPropertiesTemplate;
        MixtapePropertiesTemplate = mixtapePropertiesTemplate;
        Entities = entities;
    }

    public static bool IsCustomResourceDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public void CheckVersionThenReadDirectory(string path)
    {
        CheckVersionThenReadDirectory(path,
            ConfigManager.SaveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory(),
            ConfigManager.UpgradeOldMixtapes.Value,
            ConfigManager.GetOutdatedPluginHandling(),
            ConfigManager.DisplayEventTemplates.Value,
            ConfigManager.EventTemplatesIndex.Value);
    }
    public void CheckVersionThenReadDirectory(string path, 
        bool backup, 
        bool upgrade, 
        OutdatedPluginHandling outdatedPluginHandling,
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        if (!ReadNecessary || !CheckMixtapeVersion(path, backup, upgrade, outdatedPluginHandling))
        {
            return;
        }

        ReadDirectory(path, backup);
        UpdateEventTemplates();
        return;
    }

    public void ReadDirectory(string path, bool backup)
    {
        int filesLoaded = 0;
        bool hasResourceFolder = false;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            if (IsCustomResourceDirectory(subpath))
            {
                hasResourceFolder = true;
                filesLoaded += LocateResources(subpath, path, backup, true);
            }
        }
        if (!hasResourceFolder)
        {
            filesLoaded += LocateResources(path, path, backup, false);
        }

        if (filesLoaded > 0)
        {
            Logger.LogInfo($"Loaded {filesLoaded} custom assets");
            if (!HasCustomAssets && backup)
            {
                Logger.LogUpgradeMixtape(
                    "This mixtape with custom assets is missing a \"BopCustomTextues.json\" file specifying version. " +
                    "Save this mixtape in the editor to add a \"BopCustomTextures.json\" file automatically!"
                );
                HasCustomAssets = true;
            }
        }
        else
        {
            Logger.LogInfo("No custom assets found");
        }
    }

    public void ReadArchive(string path, bool backup = false)
    {
        string rootPath = null;
        try
        {
            rootPath = FileManager.ExtractArchiveToTempDirectory(path, Path.GetFileNameWithoutExtension(path));
            ReadDirectory(rootPath, backup);
        } 
        catch (Exception e)
        {
            Logger.LogError(e);
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
        else if (Directory.Exists(path)) 
        {
            ReadDirectory(path, backup);
        }
        else
        {
            Logger.LogError($"Path is not a .bop file or directory: {path}");
        }
    }

    public void ReadLastPath(bool backup = false)
    {
        ReadPath(LastPath, backup);
    }

    public int LocateResources(string path, string parentPath, bool backup, bool isResources)
    {
        int filesLoaded = 0;
        int index = parentPath.Length + 1;
        var subpaths = Directory.EnumerateDirectories(path);
        foreach (var subpath in subpaths)
        {
            if (CustomTextureManager.IsCustomTextureDirectory(subpath))
            {
                if (backup)
                {
                    filesLoaded += FileManager.BackupFiles(TextureManager.LocateCustomTextures(subpath, index, isResources), parentPath);
                }
                else
                {
                    filesLoaded += TextureManager.LocateCustomTextures(subpath, index, isResources).Count();
                }
            }
        }
        foreach (var subpath in subpaths)
        {
            if (CustomSceneManager.IsCustomSceneDirectory(subpath))
            {
                if (backup)
                {
                    filesLoaded += FileManager.BackupFiles(SceneManager.LocateCustomScenes(subpath, index, Release), parentPath);
                }
                else
                {
                    filesLoaded += SceneManager.LocateCustomScenes(subpath, index, Release).Count();
                }
            }
        }
        return filesLoaded;
    }

    public void WriteDirectory(string path)
    {
        WriteDirectory(path, ConfigManager.UpgradeOldMixtapes.Value);
    }
    public void WriteDirectory(string path, bool upgrade)
    {
        if (HasCustomAssets)
        {
            Logger.LogInfo("Saving with custom files");
            FileManager.WriteDirectory(path);
            WriteMixtapeVersion(path, upgrade);
        };
    }
    
    // NOTE: Bits & Bops currently supports RIQ v1 only.
    public void CheckVersionThenReadRiqArchive(string riqPath)
    {
        CheckVersionThenReadRiqArchive(riqPath,
            ConfigManager.SaveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory(),
            ConfigManager.UpgradeOldMixtapes.Value,
            ConfigManager.GetOutdatedPluginHandling(),
            ConfigManager.DisplayEventTemplates.Value,
            ConfigManager.EventTemplatesIndex.Value);
    }
    public void CheckVersionThenReadRiqArchive(string riqPath, 
        bool backup, 
        bool upgrade, 
        OutdatedPluginHandling outdatedPluginHandling, 
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        string rootPath = null;
        if (!ReadNecessary)
        {
            return;
        }
        try
        {
            rootPath = FileManager.ExtractArchiveToTempDirectory(riqPath, Path.GetFileNameWithoutExtension(riqPath));
            CheckVersionThenReadDirectory(rootPath, backup, upgrade, outdatedPluginHandling, displayEventTemplates, eventTemplatesIndex);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to load custom assets from RIQ v1 file: {e}");
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
    public void SaveAsRiq(string riqPath)
    {
        SaveAsRiq(riqPath, ConfigManager.UpgradeOldMixtapes.Value);
    }
    public void SaveAsRiq(string riqPath, bool upgrade)
    {
        if (!HasCustomAssets)
        {
            return;
        }

        try
        {
            var rootPath = FileManager.ExtractArchiveToTempDirectory(riqPath, Path.GetDirectoryName(riqPath));
            if (FileManager.WriteDirectory(rootPath))
            {
                Logger.LogInfo("Saving RIQ v1 with custom files");
                WriteMixtapeVersion(rootPath, upgrade);
                FileManager.PackDirectoryToArchive(rootPath, riqPath);
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to save custom assets into RIQ v1 file: {e}");
        }
    }

    public void Unload()
    {
        SceneManager.UnloadCustomScenes();
        TextureManager.UnloadCustomTextures();
        VariantManager.UnloadCustomTextureVariants();
        FileManager.DeleteTempDirectory();
    }

    public void ResetAll()
    {
        ResetAll(ConfigManager.DisplayEventTemplates.Value, ConfigManager.EventTemplatesIndex.Value);
    }
    public void ResetAll(Display displayEventTemplates, int eventTemplatesIndex)
    {
        Unload();
        LastPath = null;
        LastModified = default;
        HasCustomAssets = false;
        ReadNecessary = true;
        UpdateEventTemplates();
    }

    public void ResetIfNecessary(string path)
    {
        ResetIfNecessary(path, ConfigManager.DisplayEventTemplates.Value, ConfigManager.EventTemplatesIndex.Value);
    }
    public void ResetIfNecessary(string path, Display displayEventTemplates, int eventTemplatesIndex)
    {
        var modified = File.Exists(path) ? File.GetLastWriteTime(path) : default;
        if (LastPath != path || LastModified != modified)
        {
            ResetAll(displayEventTemplates, eventTemplatesIndex);
        }
        else
        {
            Logger.LogInfo("Avoided customs reload for reopened mixtape");
            ReadNecessary = false;
        }
        LastPath = path;
        LastModified = modified;
    }


    public void ResetAndReload()
    {
        ResetAndReload(LastPath);
    }
    public void ResetAndReload(string path)
    {
        ResetAndReload(path, 
            ConfigManager.SaveCustomFiles.Value,
            ConfigManager.DisplayEventTemplates.Value,
            ConfigManager.EventTemplatesIndex.Value);
    }
    public void ResetAndReload(string path, bool backup, Display displayEventTemplates, int eventTemplatesIndex)
    {
        var modified = File.Exists(path) ? File.GetLastWriteTime(path) : default;
        if (LastPath != path || modified != LastModified || Directory.Exists(path))
        {
            Unload();
            ReadPath(path, backup);
            UpdateEventTemplates();
        } 
        else
        {
            Logger.LogInfo("Custom assets appear to be unmodified");
        }
        LastPath = path;
        LastModified = modified;
    }

    public void DeleteTempDirectory()
    {
        FileManager.DeleteTempDirectory();
    }

    public void InitScene(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        SceneManager.InitCustomScene(__instance, sceneKey);
    }

    public void Prepare(MixtapeLoaderCustom __instance)
    {
        foreach (var dict in __instance.RootObjects)
        {
            TextureManager.InitCustomTextures(__instance, dict.Key);
            SceneManager.InitCustomSceneDeferred(__instance, dict.Key);
        }
        PrepareEvents(__instance, __instance.GetEntities());
    }

    public void PrepareEvents(MixtapeLoaderCustom __instance, Entity[] entities)
    {
        SceneManager.PrepareEvents(__instance, entities);
        TextureManager.PrepareEvents(__instance, entities);
    }

    public void UpdateEventTemplates()
    {
        SceneManager.UpdateEventTemplates();
        TextureManager.UpdateEventTemplates();
    }

    public void UpdateEventCategoryPosition()
    {
        var index = FindEventCategoryIndex();
        if (index == lastTemplatesIndex && Entities.ContainsKey(MyPluginInfo.PLUGIN_GUID))
        {
            return;
        }
        Entities.Remove(MyPluginInfo.PLUGIN_GUID);
        var list = Entities.ToList();
        if (index > list.Count || index < 1)
        {
            index = list.Count;
        }
        list.Insert(index, new KeyValuePair<string, List<MixtapeEventTemplate>>(MyPluginInfo.PLUGIN_GUID, new List<MixtapeEventTemplate>(BopCustomTexturesEventTemplates.Templates)));
        Entities.Clear();
        foreach (var pair in list)
        {
            Entities[pair.Key] = pair.Value;
        }
        lastTemplatesIndex = index;

        /*Logger.LogError("{");
        foreach (var cat in MixtapeEventTemplates.Categories)
        {
            Logger.LogError($" - {cat}");
        }
        Logger.LogError("}");*/
    }

    public int FindEventCategoryIndex()
    {
        var x = FindEventCategoryIndexInternal();
        if (Entities.ContainsKey(MyPluginInfo.PLUGIN_GUID) && x > Entities.Keys.ToList().IndexOf(MyPluginInfo.PLUGIN_GUID))
        {
            x--;
        }
        return x;
    }

    private int FindEventCategoryIndexInternal()
    {
        foreach (string category in ConfigManager.GetEventTemplatesBefore())
        {
            if (Entities.ContainsKey(category))
            {
                return Entities.Keys.ToList().IndexOf(category);
            }
        }
        foreach (string category in ConfigManager.GetEventTemplatesAfter())
        {
            if (Entities.ContainsKey(category))
            {
                return Entities.Keys.ToList().IndexOf(category) + 1;
            }
        }
        return ConfigManager.EventTemplatesIndex.Value;
    }

    public void HandleMenuOption(MixtapeEditorScript __instance, int index)
    {
        HandleMenuOption(__instance, index,
            ConfigManager.DisplayCopyOptions.Value,
            ConfigManager.DisplayReloadOptions.Value,
            ConfigManager.SaveCustomFiles.Value,
            ConfigManager.UpgradeOldMixtapes.Value,
            ConfigManager.DisplayEventTemplates.Value,
            ConfigManager.EventTemplatesIndex.Value);
    }
    public void HandleMenuOption(MixtapeEditorScript __instance, int index, 
        Display showCopyOptions, 
        Display showReloadOptions, 
        bool backup,
        bool upgrade,
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        if (!DisplayActive(showCopyOptions, HasCustomAssets))
        {
            index += 2;
        }
        switch (index)
        {
            case 0:
                FileOpenCustomsArchive(__instance, backup, displayEventTemplates, eventTemplatesIndex);
                break;
            case 1:
                FileOpenCustomsDirectory(__instance, backup, displayEventTemplates, eventTemplatesIndex);
                break;
            case 2:
                if (DisplayActive(showReloadOptions, HasCustomAssets))
                {
                    ResetAndReload(LastPath, backup, displayEventTemplates, eventTemplatesIndex);
                }
                break;
        }
    }

    public void UpdateEditorPropertiesEvent(MixtapeEditorScript __instance)
    {
        bool copyActive = DisplayActive(ConfigManager.DisplayCopyOptions.Value, HasCustomAssets);
        bool reloadActive = DisplayActive(ConfigManager.DisplayReloadOptions.Value, HasCustomAssets);

        if (EditorPropertiesEvent != null)
        {
            if (lastCopyActive != copyActive || lastReloadActive != reloadActive)
            {
                UnityEngine.Object.Destroy(EditorPropertiesEvent.gameObject);
                EditorPropertiesEvent = null;
            }
        }

        if (EditorPropertiesEvent == null)
        {
            lastCopyActive = copyActive;
            lastReloadActive = reloadActive;

            EditorPropertiesTemplate.properties = new(BopCustomTexturesEventTemplates.EditorPropertiesTemplatePropertiesBase);
            if (copyActive)
            {
                foreach (string str in BopCustomTexturesEventTemplates.PropertyCopyOptions)
                {
                    EditorPropertiesTemplate.properties[str] = new MixtapeEventTemplates.ButtonField();
                }
            }
            if (reloadActive)
            {
                foreach (string str in BopCustomTexturesEventTemplates.PropertyReloadOptions)
                {
                    EditorPropertiesTemplate.properties[str] = new MixtapeEventTemplates.ButtonField();
                }
            }
            Entity entity = Entity.FromTemplate(EditorPropertiesTemplate);
            entity.beat = -100f;
            entity.SetString("last path", _lastPath);
            EditorPropertiesEvent = MixtapeEventScript.Spawn(__instance.mixtapeEventPrefab, entity);
            var singletonEvents = __instance.GetSingletonEvents();
            if (singletonEvents != null)
            {
                singletonEvents[entity.dataModel] = EditorPropertiesEvent;
            }
        }
    }

    public void UpdateMixtapePropertiesEvent(MixtapeEditorScript __instance)
    {
        if (MixtapePropertiesEvent == null)
        {
            Entity entity = Entity.FromTemplate(MixtapePropertiesTemplate);
            entity.beat = -100f;
            entity.SetString("version", _version);
            entity.SetInt("release", (int)_release);
            MixtapePropertiesEvent = MixtapeEventScript.Spawn(__instance.mixtapeEventPrefab, entity);
            var singletonEvents = __instance.GetSingletonEvents();
            if (singletonEvents != null)
            {
                singletonEvents[entity.dataModel] = MixtapePropertiesEvent;
            }
        }
    }

    public void UpdateSingletonEvents(MixtapeEditorScript __instance)
    {
        UpdateEventCategoryPosition();
        UpdateEditorPropertiesEvent(__instance);
        UpdateMixtapePropertiesEvent(__instance);
        UpdateMixtapeCategoryButton(__instance);
    }

    public bool CycleModdedCategory(MixtapeEditorScript __instance, ref string category)
    {
        return CycleModdedCategory(__instance, ref category, ConfigManager.GetHijackEventCategory());
    }
    public bool CycleModdedCategory(MixtapeEditorScript __instance, ref string category, IEnumerable<string> hijackedCategories)
    {
        if (!hijackedCategories.Contains(category))
        {
            return false;
        }
        var moddedCategories = DefaultEventCategories.ModdedCategories;

        if (category == HijackedCategorySelected)
        {
            ModdedCategoryIndex++;
            var index = moddedCategories.IndexOf(category);
            if (index >= 0 && ModdedCategoryIndex >= moddedCategories.IndexOf(category))
            {
                ModdedCategoryIndex++;
            }
            ModdedCategoryIndex = (ModdedCategoryIndex + 1) % (moddedCategories.Count + 1) - 1;
        }
        HijackedCategorySelected = category;

        category = (ModdedCategoryIndex < 0 || ModdedCategoryIndex >= moddedCategories.Count) ? category : moddedCategories[ModdedCategoryIndex];
        return true;
    }

    public void CycleProperty(MixtapeEditorScript __instance, int option)
    {
        if (__instance.GetSelectedEventsCount() == 1 && 
            __instance.GetSelectedEventsIndex(0).Datamodel == EditorPropertiesTemplate.dataModel && 
            option >= BopCustomTexturesEventTemplates.EditorPropertiesTemplatePropertiesBase.Count)
        {
            HandleMenuOption(__instance, option - BopCustomTexturesEventTemplates.EditorPropertiesTemplatePropertiesBase.Count);
        }
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    public bool SelectedEventIsSingleton(MixtapeEditorScript __instance)
    {
        return __instance.GetSelectedEventsCount() == 1 && 
            (__instance.GetSelectedEventsIndex(0) == EditorPropertiesEvent || 
            __instance.GetSelectedEventsIndex(0) == MixtapePropertiesEvent);
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    public bool CheckSingletonEventSelected(MixtapeEditorScript __instance)
    {
        if (__instance.GetLevelIndex() == Entities.Keys.ToList().IndexOf(MyPluginInfo.PLUGIN_GUID))
        {
            if (__instance.GetEventIndex() == 0)
            {
                __instance.SetSelectedEvent(EditorPropertiesEvent);
                return true;
            }
            else if (__instance.GetEventIndex() == 1)
            {
                __instance.SetSelectedEvent(MixtapePropertiesEvent);
                return true;
            }
        }
        return false;
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    public bool CheckSingletonEventSpawning(MixtapeEditorScript __instance, MixtapeEventTemplate templateEvent)
    {
        if (templateEvent == EditorPropertiesTemplate)
        {
            __instance.SetSelectedEvent(EditorPropertiesEvent);
            return true;
        }
        else if (templateEvent == MixtapePropertiesTemplate)
        {
            __instance.SetSelectedEvent(MixtapePropertiesEvent);
            return true;
        }
        return false;
    }

    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    public void FormatIfSingletonSelected(MixtapeEditorScript __instance)
    {
        if (CheckSingletonEventSelected(__instance))
        {
            __instance.ZSortEvents();
            __instance.FormatLevels();
            __instance.FormatEvents();
            __instance.FormatProperties();
            __instance.FormatValues();
        }
    }

    public void HandleKeybind(MixtapeEditorScript __instance)
    {
        if (Input.GetKeyDown(ConfigManager.CopyCustomsFromFileKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Copy Customs from File");
            FileOpenCustomsArchive(__instance);
        }
        else if (Input.GetKeyDown(ConfigManager.CopyCustomsFromFolderKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Copy Customs from Folder");
            FileOpenCustomsDirectory(__instance);
        }
        else if (Input.GetKeyDown(ConfigManager.ReloadCustomAssetsKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Reload Custom Assets");
            ResetAndReload();
        }
        else if (Input.GetKeyDown(ConfigManager.SelectEventCatagoryKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Select Event Catagory");
            __instance.OnSelectCategory(MyPluginInfo.PLUGIN_GUID);
        }
    }

    public void FileOpenCustomsArchive(MixtapeEditorScript __instance)
    {
        FileOpenCustomsArchive(__instance, 
            ConfigManager.SaveCustomFiles.Value,
            ConfigManager.DisplayEventTemplates.Value,
            ConfigManager.EventTemplatesIndex.Value);
    }
    public void FileOpenCustomsArchive(MixtapeEditorScript __instance,
        bool backup,
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        __instance.StartCoroutine(OpenCustomsArchive(__instance, backup, displayEventTemplates, eventTemplatesIndex));
    }

    public IEnumerator OpenCustomsArchive(MixtapeEditorScript __instance,
        bool backup,
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        ExtensionFilter[] extensions =
        [
            new ExtensionFilter("All Mixtape Files", "bop", "riq", "zip"),
            new ExtensionFilter("Mixtape Files", "bop"),
            new ExtensionFilter("RIQ Files", "riq"),
            new ExtensionFilter("Zip Files", "zip"),
            new ExtensionFilter("All Files", "*" ),

        ];
        string path = null;
        bool complete = false;
        yield return null;
        StandaloneFileBrowser.OpenFilePanelAsync("Copy from File", Path.GetDirectoryName(LastPath), extensions, multiselect: false, delegate (string[] paths)
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
            __instance.FormatMenu();
        }
    }

    public void FileOpenCustomsDirectory(MixtapeEditorScript __instance)
    {
        FileOpenCustomsDirectory(__instance, 
            ConfigManager.SaveCustomFiles.Value,
            ConfigManager.DisplayEventTemplates.Value,
            ConfigManager.EventTemplatesIndex.Value);
    }
    public void FileOpenCustomsDirectory(MixtapeEditorScript __instance,
        bool backup, 
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        __instance.StartCoroutine(OpenCustomsDirectory(__instance, backup, displayEventTemplates, eventTemplatesIndex));
    }

    public IEnumerator OpenCustomsDirectory(MixtapeEditorScript __instance,
        bool backup, 
        Display displayEventTemplates, 
        int eventTemplatesIndex)
    {
        string path = null;
        bool complete = false;
        yield return null;
        StandaloneFileBrowser.OpenFolderPanelAsync("Copy from Folder", Path.GetDirectoryName(LastPath), multiselect: false, delegate (string[] paths)
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
            if (IsCustomResourceDirectory(path))
            {
                path = Path.GetDirectoryName(path);
            }
            ResetAndReload(path, backup, displayEventTemplates, eventTemplatesIndex);
            __instance.FormatMenu();
        }
    }

    public bool UpdateMixtapeCategoryButton(MixtapeEditorScript __instance)
    {
        var showButton = DisplayActive(ConfigManager.DisplayEventTemplates.Value, HasCustomAssets);
        if (MixtapeCategoryButton != null)
        {
            MixtapeCategoryButton.UpdateDisplay(showButton, FindEventCategoryIndex());
            return false;
        }
        if (!showButton)
        {
            return false;
        }
        MixtapeCategoryButton = BopCustomTexturesButton.Create(__instance);
        MixtapeCategoryButton.UpdateDisplay(showButton, FindEventCategoryIndex());
        return true;
    }

    public bool GetMixtapeVersion(string path)
    {
        string filePath = Path.Combine(path, $"{MyPluginInfo.PLUGIN_GUID}.json");
        if (!File.Exists(filePath))
        {
            Version = BopCustomTexturesPlugin.LowestVersion;
            Release = BopCustomTexturesPlugin.LowestRelease;
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
                    Release = (uint)jrelease;
                } 
                else
                {
                    Logger.LogWarning($"Release is a {jrelease.Type} when it should be an int, will treat as latest.");
                    Release = BopCustomTexturesPlugin.LowestRelease;
                }
            } 
            else
            {
                Logger.LogWarning("Version data missing release, will treat as latest.");
                Release = BopCustomTexturesPlugin.LowestRelease;
            }
            if (jobj.TryGetValue("version", out var jversion))
            {
                if (jversion.Type == JTokenType.String)
                {
                    Version = SanitizeVersion((string)jversion);
                }
                else
                {
                    Logger.LogWarning($"Version is a {jversion.Type} when it should be an int, will treat as latest.");
                    Version = BopCustomTexturesPlugin.LowestVersion;
                }
            }
            else
            {
                Logger.LogWarning("Version data missing version, will treat as latest.");
                Version = BopCustomTexturesPlugin.LowestVersion;
            }

        }
        catch (JsonReaderException e)
        {
            Logger.LogError($"Error reading verison data, will treat as latest: {e}");
            Version = BopCustomTexturesPlugin.LowestVersion;
            Release = BopCustomTexturesPlugin.LowestRelease;
        }
        
        return true;
    }

    public void WriteMixtapeVersion(string path, bool upgrade)
    {
        if (upgrade)
        {
            Version = BopCustomTexturesPlugin.LowestVersion;
            Release = BopCustomTexturesPlugin.LowestRelease;
        }
        var jobj = new JObject
        {
            ["version"] = new JValue(Version),
            ["release"] = new JValue(Release)
        };

        try
        {
            using StreamWriter outputFile = new StreamWriter(Path.Combine(path, $"{MyPluginInfo.PLUGIN_GUID}.json"));
            outputFile.Write(JsonConvert.SerializeObject(jobj));
        } 
        catch (Exception e)
        {
            Logger.LogError($"Error writing version data: {e}");
        }
    }

    /// <summary>
    /// Gets the mixtape version and checks it, possibly interrupting a load if mixtape is for a newer version of the plugin.
    /// </summary>
    /// <param name="path">Mixtape root directory path to check.</param>
    /// <param name="backup">Whether or not source custom files backup is enabled.</param>
    /// <param name="upgrade">Whether or not outdated mixtape upgrading is enabled.</param>
    /// <param name="outdatedPluginHandling">Outdated plugin handling setting.</param>
    /// <returns><see langword="true"/> if assets should be loaded immediately after, and <see langword="true"/> otherwise. 
    /// May return false if a disclaimer screen should be shown before loading, or if 
    /// loading is completely disabled for a mixtape of this version.</returns>
    public bool CheckMixtapeVersion(string path, bool backup, bool upgrade, OutdatedPluginHandling outdatedPluginHandling)
    {
        HasCustomAssets = GetMixtapeVersion(path);
        if (Release > BopCustomTexturesPlugin.LowestRelease)
        {
            if (outdatedPluginHandling == OutdatedPluginHandling.LoadVanilla)
            {
                Logger.LogOutdatedPlugin(
                    $"Mixtape requires {MyPluginInfo.PLUGIN_GUID} v{Version}+, " +
                    $"but you are on v{MyPluginInfo.PLUGIN_VERSION}, so loading custom assets was cancelled."
                );
                InterruptLoad = false;
                return false;
            }
            Logger.LogOutdatedPlugin(
                $"Mixtape requires {MyPluginInfo.PLUGIN_GUID} v{Version}+, " +
                $"but you are on v{MyPluginInfo.PLUGIN_VERSION}. You may have to update {MyPluginInfo.PLUGIN_GUID} to play properly."
            );
            if (outdatedPluginHandling == OutdatedPluginHandling.ShowDisclaimer)
            {
                InterruptLoad = true;
                return false;
            }
        }
        else if (Release < BopCustomTexturesPlugin.LowestRelease && backup && upgrade)
        {
            Logger.LogUpgradeMixtape(
                $"Mixtape was made for {MyPluginInfo.PLUGIN_GUID} v{Version}, " +
                $"while you are on v{MyPluginInfo.PLUGIN_VERSION}. Save this mixtape in the editor to update its version!"
            );
        }
        return true;
    }

    public string GetDescriptionAppended(string description)
    {
        if (HasCustomAssets)
        {
            return $"{description}\n" +
                $"\n" +
                $"This mixtape optionally supports custom textures through the mod {MyPluginInfo.PLUGIN_GUID} v{Version}, " +
                $"which can be downloaded here: {BopCustomTexturesPlugin.PluginRepoUrl}/releases";
        } 
        else
        {
            return description;
        }
    }

    public static bool DisplayActive(Display display, bool active)
    {
        return display == Display.Always || display == Display.WhenActive && active;
    }

    public static string SanitizeVersion(string version)
    {
        version = VersionRegex.Replace(version, "");
        if (version.Length > VersionMaxLength)
        {
            version = version.Substring(0, VersionMaxLength - 3) + "...";
        }
        return version;
    }
}
