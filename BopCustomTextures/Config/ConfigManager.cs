using BopCustomTextures.EventTemplates;
using BepInEx.Configuration;
using UnityEngine;
using LogLevel = BopCustomTextures.Logging.LogLevel;

namespace BopCustomTextures.Config;
public class ConfigManager
{
    public ConfigEntry<bool> LoadCustomAssets;

    public ConfigEntry<OutdatedPluginHandling> LoadOutdatedPluginPlayer;

    public ConfigEntry<bool> SaveCustomFiles;
    public ConfigEntry<bool> UpgradeOldMixtapes;
    public ConfigEntry<bool> UploadAppendDescription;
    public ConfigEntry<bool> LoadOutdatedPluginEditor;

    public ConfigEntry<KeyCode> CopyCustomsFromFileKeybind;
    public ConfigEntry<KeyCode> CopyCustomsFromFolderKeybind;
    public ConfigEntry<KeyCode> ReloadCustomAssetsKeybind;
    public ConfigEntry<KeyCode> SelectEventCatagoryKeybind;

    public ConfigEntry<string> HijackEventCategory;
    public ConfigEntry<Display> DisplayCopyOptions;
    public ConfigEntry<Display> DisplayReloadOptions;
    public ConfigEntry<Display> DisplayEventTemplates;
    public ConfigEntry<int> EventTemplatesIndex;

    public ConfigEntry<LogLevel> LogOutdatedPlugin;
    public ConfigEntry<LogLevel> LogUpgradeMixtape;

    public ConfigEntry<LogLevel> LogFileLoading;
    public ConfigEntry<LogLevel> LogUnloading;
    public ConfigEntry<LogLevel> LogSeperateTextureSprites;
    public ConfigEntry<LogLevel> LogAtlasTextureSprites;
    public ConfigEntry<LogLevel> LogMComponentRegistering;

    public ConfigEntry<LogLevel> LogSceneIndices;

    public ConfigManager(ConfigFile config)
    {
        LoadConfigs(config);
    }

    public void LoadConfigs(ConfigFile config)
    {
        LoadCustomAssets = config.Bind("General",
            "LoadCustomAssets",
            true,
            "When opening a modded mixtape, load the custom assets stored in it.\n" +
            "(Note: modded mixtapes won't maintain their custom files if saved while this is disabled.)");


        LoadOutdatedPluginPlayer = config.Bind("Player",
            "LoadOutdatedPluginPlayer",
            OutdatedPluginHandling.ShowDisclaimer,
            "How to handle opening a modded mixtape in the Mixtape Player that was made for a newer version of BopCustomTextures.");


        SaveCustomFiles = config.Bind("Editor",
            "SaveCustomFiles",
            true,
            "When opening a modded mixtape in the editor, maintain its custom asset files whenever the mixtape is saved.");

        UpgradeOldMixtapes = config.Bind("Editor",
            "UpgradeOldMixtapes",
            true,
            "When opening a modded mixtape for an older version of the plugin in the editor, " +
            "upgrade the mixtape version to the current one when saving.");

        UploadAppendDescription = config.Bind("Editor",
            "UploadAppendDescription",
            true,
            "When uploading a modded mixtape to the Steam Workshop, add a blurb to the end of the description with a link to download BopCustomTextures.");

        LoadOutdatedPluginEditor = config.Bind("Editor",
            "LoadOutdatedPluginEditor",
            true,
            "When opening a modded mixtape in the editor made for a newer version of BopCustomTextures, attempt to load custom assets.");


        CopyCustomsFromFileKeybind = UpgradeOrBind(config, "Editor", "Editor.Keybinds",
            "CopyCustomsFromFileKeybind",
            KeyCode.F3,
            "Keybind used to access Copy Customs From File.");

        CopyCustomsFromFolderKeybind = UpgradeOrBind(config, "Editor", "Editor.Keybinds",
            "CopyCustomsFromFolderKeybind",
            KeyCode.F4,
            "Keybind used to access Copy Customs From Folder.");

        ReloadCustomAssetsKeybind = UpgradeOrBind(config, "Editor", "Editor.Keybinds",
            "ReloadCustomAssetsKeybind",
            KeyCode.F5,
            "Keybind used to access Reload Custom Assets.");

        SelectEventCatagoryKeybind = UpgradeOrBind(config, "Editor", "Editor.Keybinds",
            "SelectEventCatagoryKeybind",
            KeyCode.F6,
            "Keybind used to switch to \"Bop Custom Textures\" catagory.\n" +
            "(Note: only works post editor UI update.)");


        HijackEventCategory = config.Bind("Editor.Display",
            "HijackEventCategory",
            "effects",
            "If this event category's button is clicked multiple times, it'll cycle through all modded event categories.\n" +
            "Set to blank (\"HijackEventCategory = \") to disable this feature.\n" +
            "\n" + 
            "Useful values include:\n" +
            "\t- _ (this is global)\t\n\t- gameManager\t\n\t- effects\t\n\t- accessibility\n\t- debug\t");

        DisplayCopyOptions = config.Bind("Editor.Display",
            "DisplayOptionsCopy",
            Display.Always,
            $"When to display \"{BopCustomTexturesEventTemplates.PropertyCopyOptions[0]}\" and \"{BopCustomTexturesEventTemplates.PropertyCopyOptions[1]}\" in \"Global Properties\".");

        DisplayReloadOptions = config.Bind("Editor.Display",
            "DisplayOptionsReload",
            Display.Always,
            $"When to display \"{BopCustomTexturesEventTemplates.PropertyReloadOptions[0]}\" in \"Global Properties\".");

        DisplayEventTemplates = config.Bind("Editor.Display",
            "DisplayEventTemplates",
            Display.Always,
            "When to display mixtape events category \"Bop Custom Textures\".\n" +
            "Mostly irrelevant as of editor UI update, will only affect behavior of HijackEventCategory setting.\n" +
            "(Note: options besides \"Always\" can be buggy when attempting to work with a modded mixtape.)");

        EventTemplatesIndex = config.Bind("Editor.Display",
            "EventTemplatesIndex",
            4,
            "Position in mixtape event categories list to display \"Bop Custom Textures\" at. " +
            "Values lower than 1 will put category at end of list.\n" +
            "Mostly irrelevant as of editor UI update, will only affect behavior of HijackEventCategory setting.\n" +
            "(Note: position 0 unsupported as editor is hardcoded to only support category \"Global\" there.)");


        LogOutdatedPlugin = config.Bind("Logging",
            "logOutdatedPlugin",
            LogLevel.Error | LogLevel.MixtapeEditor,
            "Log level for message indicating BopCustomTextures needs to be updated to play a mixtape.");

        LogUpgradeMixtape = config.Bind("Logging",
            "LogUpgradeMixtape",
            LogLevel.Warning | LogLevel.MixtapeEditor,
            "Log level for messaage reminding user to save a mixtape to add/upgrade its BopCustomTextures.json file.");


        LogFileLoading = UpgradeOrBind(config, "Logging", "Logging.Debugging",
            "LogFileLoading",
            LogLevel.Debug,
            "Log level for verbose file loading of custom files in .bop archives.");

        LogUnloading = UpgradeOrBind(config, "Logging", "Logging.Debugging",
            "LogUnloading",
            LogLevel.Debug,
            "Log level for verbose custom asset unloading");

        LogSeperateTextureSprites = UpgradeOrBind(config, "Logging", "Logging.Debugging",
            "LogSeperateTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from seperate textures.");

        LogAtlasTextureSprites = UpgradeOrBind(config, "Logging", "Logging.Debugging",
            "LogAtlasTextureSprites",
            LogLevel.Debug,
            "Log level for verbose custom sprite creation from atlas textures.");

        LogMComponentRegistering = config.Bind("Logging.Debugging",
            "LogMComponentRegistering",
            LogLevel.Debug,
            "Log level for registering of MComponents.");


        LogSceneIndices = UpgradeOrBind(config, "Logging", "Logging.Modding",
            "LogSceneIndices",
            LogLevel.None,
            "Log level for vanilla scene loading, including scene name + build index. (for locating level and sharedassets files)");
    }

    private ConfigEntry<T> UpgradeOrBind<T>(ConfigFile config, string oldSection, string newSection, string key, T defaultValue, string description)
    {
        var oldEntry = config.Bind(
            oldSection,
            key,
            defaultValue,
            description
        );
        config.Remove(new ConfigDefinition(oldSection, key));
        return config.Bind(
            newSection,
            key,
            oldEntry.Value,
            description
        );
    }

    public OutdatedPluginHandling GetOutdatedPluginHandling()
    {
        return (TempoSceneManager.GetActiveSceneKey() == SceneKey.RiqLoader) ? LoadOutdatedPluginPlayer.Value :
            LoadOutdatedPluginEditor.Value ? OutdatedPluginHandling.LoadModded : OutdatedPluginHandling.LoadVanilla;
    }
}
