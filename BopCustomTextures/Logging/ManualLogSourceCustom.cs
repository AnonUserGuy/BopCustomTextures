using BopCustomTextures.Config;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.Logging;

/// <summary>
/// Wrapper class for BepInEx's ManualLogSource with special methods for logging messages with configurable log levels
/// and outputing messages to the mixtape editor's dialogue box.
/// </summary>
/// <param name="logger">Internal BepInEx ManualLogSource</param>
/// <param name="configManager">BopCustomTextures configuration manager</param>
public class ManualLogSourceCustom(ManualLogSource logger, ConfigManager configManager) : ILogger
{
    private readonly ManualLogSource Logger = logger;
    private readonly ConfigManager ConfigManager = configManager;

    private GameObject ErrorCanvas = null;
    private TMP_Text TxtTitle = null;
    private TMP_Text TxtBody = null;

    public void LogFileLoading(object data)
    {
        Log(ConfigManager.LogFileLoading.Value, data);
    }
    public void LogUnloading(object data)
    {
        Log(ConfigManager.LogUnloading.Value, data);
    }
    public void LogSeperateTextureSprites(object data)
    {
        Log(ConfigManager.LogSeperateTextureSprites.Value, data);
    }
    public void LogAtlasTextureSprites(object data)
    {
        Log(ConfigManager.LogAtlasTextureSprites.Value, data);
    }

    public void LogMComponentRegistering(object data)
    {
        Log(ConfigManager.LogMComponentRegistering.Value, data);
    }

    public void LogOutdatedPlugin(object data)
    {
        Log(ConfigManager.LogOutdatedPlugin.Value, data);
    }
    public void LogUpgradeMixtape(object data)
    {
        Log(ConfigManager.LogUpgradeMixtape.Value, data);
    }

    public void LogEditor(object data)
    {
        LogEditor(LogLevel.None, data);
    }
    public void LogEditor(LogLevel level, object data)
    {
        if (ErrorCanvas == null || TxtBody == null)
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
            var txtBodyTransform = ErrorCanvas.transform.Find("Prompt/Text Body");
            if (txtBodyTransform == null)
            {
                TxtBody = null;
            } 
            else
            {
                TxtBody = txtBodyTransform.gameObject.GetComponentInChildren<TMP_Text>();
            }
            if (TxtBody == null)
            {
                TxtBody = ErrorCanvas.GetComponentInChildren<TMP_Text>();
            }
            else
            {
                var txtTitleTransform = ErrorCanvas.transform.Find("Prompt/Text Title");
                if (txtTitleTransform != null)
                {
                    TxtTitle = txtTitleTransform.gameObject.GetComponentInChildren<TMP_Text>();
                }
            }
        }
        ErrorCanvas.SetActive(true);
        if (TxtTitle != null)
        {
            level &= ~LogLevel.MixtapeEditor;
            if (level != LogLevel.None)
            {
                TxtTitle.text = $"{MyPluginInfo.PLUGIN_NAME} - {string.Join(", ", LogLevelToStrings(level))}";
            } 
            else
            {
                TxtTitle.text = MyPluginInfo.PLUGIN_NAME;
            }
            
            TxtBody.text = data.ToString();
        } 
        else
        {
            TxtBody.text = $"[{Logger.SourceName}] {data}";
        }
    }

    public void Log(LogLevel level, object data)
    {
        if ((level & LogLevel.MixtapeEditor) == LogLevel.MixtapeEditor)
        {
            LogEditor(level, data);
        }
        Logger.Log((BepInEx.Logging.LogLevel)level & BepInEx.Logging.LogLevel.All, data);
    }

    public void LogFatal(object data)
    {
        Logger.LogFatal(data);
    }

    public void LogError(object data)
    {
        Logger.LogError(data);
    }

    public void LogWarning(object data)
    {
        Logger.LogWarning(data);
    }

    public void LogMessage(object data)
    {
        Logger.LogMessage(data);
    }

    public void LogInfo(object data)
    {
        Logger.LogInfo(data);
    }

    public void LogDebug(object data)
    {
        Logger.LogDebug(data);
    }

    public static IEnumerable<string> LogLevelToStrings(LogLevel level)
    {
        for (LogLevel i = (LogLevel)1; (i & LogLevel.All) != 0; i = (LogLevel)((int)i << 1))
        {
            if ((i & level) != 0)
            {
                yield return Enum.GetName(typeof(LogLevel), i);
            }
        }
    }
}