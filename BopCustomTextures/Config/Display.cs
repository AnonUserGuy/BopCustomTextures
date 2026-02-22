using System.ComponentModel;

namespace BopCustomTextures.Config;

/// <summary>
/// When an editor menu option should be displayed.
/// </summary>
public enum Display
{
    Never,
    /// <summary>
    /// When a modded mixtape is opened
    /// </summary>
    [Description("When a modded mixtape is opened")]
    WhenActive,
    Always
}