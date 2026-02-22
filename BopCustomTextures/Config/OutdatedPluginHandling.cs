using System.ComponentModel;

namespace BopCustomTextures.Config;

/// <summary>
/// How to handle a mixtape made for a newer version of the plugin.
/// </summary>
public enum OutdatedPluginHandling
{
    /// <summary>
    /// Load without custom assets
    /// </summary>
    [Description("Load without custom assets")]
    LoadVanilla,
    /// <summary>
    /// Attempt to load custom assets
    /// </summary>
    [Description("Attempt to load custom assets")]
    LoadModded,
    /// <summary>
    /// Show disclaimer screen
    /// </summary>
    [Description("Show disclaimer screen")]
    ShowDisclaimer
}