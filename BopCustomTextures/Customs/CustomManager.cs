using BopCustomTextures.Json;
using BopCustomTextures.Config;
using BopCustomTextures.AccessExtensions;
using SFB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages all custom assets using specific manager classes.
/// </summary>
public class CustomManager : BaseCustomManager
{
    private const int VersionMaxLength = 50;
    public static readonly Regex VersionRegex = new Regex("<.*?>", RegexOptions.Compiled);
    public static readonly Regex PathRegex = new Regex(@"[\\/](?:res(?:ource)?s?|BopCustomTextures)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // these are set by CustomMenuManager
    public MixtapeEventScript EditorPropertiesEvent;
    public MixtapeEventScript MixtapePropertiesEvent;

    public bool HasCustomAssets = false;

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

    private bool _unsafe;
    public bool Unsafe
    {
        get
        {
            if (MixtapePropertiesEvent != null)
            {
                var unsafe0 = MixtapePropertiesEvent.Entity.GetBool("unsafe");
                if (unsafe0 != _unsafe)
                {
                    _unsafe = unsafe0;
                }
            }
            return _unsafe;
        }
        set
        {
            _unsafe = value;
            if (MixtapePropertiesEvent != null)
            {
                MixtapePropertiesEvent.Entity.SetBool("unsafe", _unsafe);
            }
        }
    }


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
    public MixtapeInfo LastMixtapeInfo;

    public bool ReadNecessary = true;
    public bool InterruptLoad = false;

    public ConfigManager ConfigManager;

    public CustomSceneManager SceneManager;
    public CustomTextureManager TextureManager;
    public CustomVariantNameManager VariantManager;
    public CustomFileManager FileManager;

    /// <param name="logger">Plugin-specific logger.</param>
    /// <param name="configManager">BopCustomTextures configuration manager</param>
    /// <param name="tempPath">Where to temporarily save source files in custom mixtape while custom mixtape is loaded.</param>
    /// <param name="sceneModTemplate">Mixtape event template for applying scene mods.</param>
    /// <param name="textureTemplates">Mixtape event templates concerning custom textures.</param>
    /// <param name="entities">List of all mixtape event categories and events.</param>
    public CustomManager(ILogger logger, ConfigManager configManager,
        string tempPath, 
        MixtapeEventTemplate sceneModTemplate, 
        MixtapeEventTemplate[] textureTemplates) : base(logger)
    {
        ConfigManager = configManager;
        VariantManager = new CustomVariantNameManager(logger);
        SceneManager = new CustomSceneManager(logger, VariantManager, sceneModTemplate);
        TextureManager = new CustomTextureManager(logger, VariantManager, textureTemplates);
        FileManager = new CustomFileManager(logger, tempPath);
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
            ConfigManager.GetOutdatedPluginHandling());
    }
    public void CheckVersionThenReadDirectory(string path, 
        bool backup, 
        bool upgrade, 
        OutdatedPluginHandling outdatedPluginHandling)
    {
        if (!ReadNecessary || !CheckMixtapeVersion(path, backup, upgrade, outdatedPluginHandling))
        {
            return;
        }

        ReadDirectory(path, backup);
        UpdateEventTemplates();
    }

    // NOTE: Bits & Bops currently supports RIQ v1 only.
    public void CheckVersionThenReadArchive(string path)
    {
        CheckVersionThenReadArchive(path,
            ConfigManager.SaveCustomFiles.Value && CustomFileManager.ShouldBackupDirectory(),
            ConfigManager.UpgradeOldMixtapes.Value,
            ConfigManager.GetOutdatedPluginHandling());
    }
    public void CheckVersionThenReadArchive(string path,
        bool backup,
        bool upgrade,
        OutdatedPluginHandling outdatedPluginHandling)
    {
        if (!ReadNecessary)
        {
            return;
        }

        using var tempDirectory = FileManager.ExtractArchiveToTempDirectory(path);
        CheckVersionThenReadDirectory(tempDirectory, backup, upgrade, outdatedPluginHandling);
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
        using var tempDirectory = FileManager.ExtractArchiveToTempDirectory(path);
        ReadDirectory(tempDirectory, backup);
    }

    public void ReadPath(bool backup = false)
    {
        ReadPath(LastPath, backup);
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

    public int LocateResources(string path, string parentPath, bool backup, bool isResources)
    {
        int filesLoaded = 0;
        int index = parentPath.Length + 1;
        var subpaths = Directory.EnumerateDirectories(path);
        LastMixtapeInfo = GetMixtapeInfo();
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
                    filesLoaded += FileManager.BackupFiles(SceneManager.LocateCustomScenes(subpath, index, LastMixtapeInfo), parentPath);
                }
                else
                {
                    filesLoaded += SceneManager.LocateCustomScenes(subpath, index, LastMixtapeInfo).Count();
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
    public void WriteArchive(string path)
    {
        WriteArchive(path, ConfigManager.UpgradeOldMixtapes.Value);
    }
    public void WriteArchive(string path, bool upgrade)
    {
        if (HasCustomAssets)
        {
            using var tempDirectory = FileManager.ExtractArchiveToTempDirectory(path);
            WriteDirectory(tempDirectory, upgrade);
            FileManager.PackDirectoryToArchive(tempDirectory, path);
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
        Unload();
        LastPath = null;
        LastModified = default;
        LastMixtapeInfo = default;
        HasCustomAssets = false;
        ReadNecessary = true;
        UpdateEventTemplates();
    }

    public bool CheckReadNecessary(string path, bool requireIfDirectory = true)
    {
        var isFile = File.Exists(path);
        var modified = isFile ? File.GetLastWriteTime(path) : default;
        var mixtapeInfo = GetMixtapeInfo();

        var result = (requireIfDirectory && !isFile)
            || path != LastPath
            || modified != LastModified 
            || mixtapeInfo != LastMixtapeInfo;

        LastPath = path;
        LastModified = modified;
        LastMixtapeInfo = mixtapeInfo;

        return result;
    }

    public void ResetIfNecessary(string path, bool requireIfDirectory = true)
    {
        if (CheckReadNecessary(path, requireIfDirectory))
        {
            ResetAll();
            CheckReadNecessary(path);
        }
        else
        {
            Logger.LogInfo("Avoided customs reload for reopened mixtape");
            ReadNecessary = false;
        }
    }

    public void ResetAndReload(bool require = false)
    {
        ResetAndReload(LastPath, ConfigManager.SaveCustomFiles.Value, require);
    }

    public void ResetAndReload(string path, bool backup, bool require)
    {
        if (require || CheckReadNecessary(path))
        {
            Unload();
            ReadPath(path, backup);
            CheckReadNecessary(path);
            UpdateEventTemplates();
        } 
        else
        {
            Logger.LogInfo("Custom assets appear to be unmodified");
        }
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

    public void FileOpenCustomsArchive(MixtapeEditorScript __instance)
    {
        FileOpenCustomsArchive(__instance, ConfigManager.SaveCustomFiles.Value);
    }
    public void FileOpenCustomsArchive(MixtapeEditorScript __instance, bool backup)
    {
        __instance.StartCoroutine(OpenCustomsArchive(__instance, backup));
    }

    private IEnumerator OpenCustomsArchive(MixtapeEditorScript __instance, bool backup)
    {
        ExtensionFilter[] extensions =
        [
            new("All Mixtape Files", "bop", "riq", "zip"),
            new("Mixtape Files", "bop"),
            new("RIQ Files", "riq"),
            new("Zip Files", "zip"),
            new("All Files", "*"),
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
            ResetAndReload(path, backup, false);
            __instance.FormatMenu();
        }
    }

    public void FileOpenCustomsDirectory(MixtapeEditorScript __instance)
    {
        FileOpenCustomsDirectory(__instance, ConfigManager.SaveCustomFiles.Value);
    }
    public void FileOpenCustomsDirectory(MixtapeEditorScript __instance, bool backup)
    {
        __instance.StartCoroutine(OpenCustomsDirectory(__instance, backup));
    }

    private IEnumerator OpenCustomsDirectory(MixtapeEditorScript __instance, bool backup)
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
            ResetAndReload(path, backup, false);
            __instance.FormatMenu();
        }
    }

    public bool GetMixtapeVersion(string folderPath)
    {
        string path = Path.Combine(folderPath, $"{MyPluginInfo.PLUGIN_GUID}.json");
        if (!File.Exists(path))
        {
            Version = BopCustomTexturesPlugin.LowestVersion;
            Release = BopCustomTexturesPlugin.LowestRelease;
            Unsafe = false;
            return false;
        }

        JObject jobj;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            MemoryStream memStream = new MemoryStream(bytes);
            using StreamReader reader = new StreamReader(memStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);

            jobj = JObject.Load(jsonReader);
        }
        catch (JsonReaderException e)
        {
            Logger.LogError($"Error reading verison data, will treat as latest: {e}");
            Version = BopCustomTexturesPlugin.LowestVersion;
            Release = BopCustomTexturesPlugin.LowestRelease;
            Unsafe = false;
            return true;
        }

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

        if (jobj.TryGetValue("unsafe", out var junsafe))
        {
            if (junsafe.Type == JTokenType.Boolean)
            {
                Unsafe = (bool)junsafe;
            }
            else
            {
                Logger.LogWarning($"Unsafe is a {junsafe.Type} when it should be a boolean, will treat as false.");
                Unsafe = false;
            }
        }
        else
        {
            Logger.LogWarning("Version data missing unsafe, will treat as false.");
            Unsafe = false;
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
            ["release"] = new JValue(Release),
            ["unsafe"] = new JValue(Unsafe),
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

    public bool GetUnsafe() => Unsafe && ConfigManager.UnsafeMode.Value;

    public MixtapeInfo GetMixtapeInfo() => new(Release, GetUnsafe());

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
