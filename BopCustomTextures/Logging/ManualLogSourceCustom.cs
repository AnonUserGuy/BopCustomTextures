using BepInEx.Configuration;
using BepInEx.Logging;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BopCustomTextures.Logging;

/// <summary>
/// Wrapper class for BepInEx's ManualLogSource with special methods for logging messages with configurable log levels
/// and outputing messages to the mixtape editor's dialogue box.
/// </summary>
/// <param name="logger">Internal BepInEx ManualLogSource</param>
/// <param name="pluginName">Plugin name to display when using LogEditor</param>
/// <param name="logFileLoading">Log level for file loading messages</param>
/// <param name="logUnloading">Log level for asset unloading messages</param>
/// <param name="logSeperateTextureSprites">Log level for sprite creation from seperate textures</param>
/// <param name="logAtlasTextureSprites">Log level for sprite creation from atlas textures</param>
public class ManualLogSourceCustom(ManualLogSource logger, 
    ConfigEntry<LogLevel> logFileLoading,
    ConfigEntry<LogLevel> logUnloading,
    ConfigEntry<LogLevel> logSeperateTextureSprites,
    ConfigEntry<LogLevel> logAtlasTextureSprites,
    ConfigEntry<LogLevel> logOutdatedPlugin,
    ConfigEntry<LogLevel> logUpgradeMixtape) : ILogger
{
    private readonly ManualLogSource logger = logger;
    private readonly ConfigEntry<LogLevel> logFileLoading = logFileLoading;
    private readonly ConfigEntry<LogLevel> logUnloading = logUnloading;
    private readonly ConfigEntry<LogLevel> logSeperateTextureSprites = logSeperateTextureSprites;
    private readonly ConfigEntry<LogLevel> logAtlasTextureSprites = logAtlasTextureSprites;
    private readonly ConfigEntry<LogLevel> logOutdatedPlugin = logOutdatedPlugin;
    private readonly ConfigEntry<LogLevel> logUpgradeMixtape = logUpgradeMixtape;

    private GameObject ErrorCanvas = null;

    public void LogFileLoading(object data)
    {
        Log(logFileLoading.Value, data);
    }
    public void LogUnloading(object data)
    {
        Log(logUnloading.Value, data);
    }
    public void LogSeperateTextureSprites(object data)
    {
        Log(logSeperateTextureSprites.Value, data);
    }
    public void LogAtlasTextureSprites(object data)
    {
        Log(logAtlasTextureSprites.Value, data);
    }

    public void LogOutdatedPlugin(object data)
    {
        Log(logOutdatedPlugin.Value, data);
    }
    public void LogUpgradeMixtape(object data)
    {
        Log(logUpgradeMixtape.Value, data);
    }

    public void LogEditor(object data)
    {
        if (ErrorCanvas == null)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "MixtapeEditor")
            {
                return;
            }

            var objs = scene.GetRootGameObjects();
            ErrorCanvas = objs.FirstOrDefault(obj => obj.name == "ErrorCanvas");
            if (ErrorCanvas == null)
            {
                return;
            }
        }
        ErrorCanvas.SetActive(true);
        ErrorCanvas.GetComponentInChildren<TMP_Text>().text = $"[{MyPluginInfo.PLUGIN_GUID}] {data}";
    }

    public void Log(LogLevel level, object data)
    {
        if ((level & LogLevel.MixtapeEditor) == LogLevel.MixtapeEditor)
        {
            LogEditor(data);
        }
        logger.Log((BepInEx.Logging.LogLevel)level & BepInEx.Logging.LogLevel.All, data);
    }

    public void LogFatal(object data)
    {
        logger.LogFatal(data);
    }

    public void LogError(object data)
    {
        logger.LogError(data);
    }

    public void LogWarning(object data)
    {
        logger.LogWarning(data);
    }

    public void LogMessage(object data)
    {
        logger.LogMessage(data);
    }

    public void LogInfo(object data)
    {
        logger.LogInfo(data);
    }

    public void LogDebug(object data)
    {
        logger.LogDebug(data);
    }
}