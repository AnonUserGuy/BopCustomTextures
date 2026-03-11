namespace BopCustomTextures.Logging;

/// <summary>
/// Logging interface specific to BopCustomTextures. Includes special methods for logging messages with configurable log levels
/// and outputing messages to the mixtape editor's dialogue box.
/// </summary>
public interface ILogger
{
    public void LogFileLoading(object data);
    public void LogUnloading(object data);
    public void LogSeperateTextureSprites(object data);
    public void LogAtlasTextureSprites(object data);
    public void LogMComponentRegistering(object data);

    public void LogOutdatedPlugin(object data);
    public void LogUpgradeMixtape(object data);

    public void Log(LogLevel level, object data);
    public void LogFatal(object data);
    public void LogError(object data);
    public void LogWarning(object data);
    public void LogMessage(object data);
    public void LogInfo(object data);
    public void LogDebug(object data);
}
