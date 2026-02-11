using System.ComponentModel;

namespace BopCustomTextures.EventTemplates;

public enum DisplayEventTemplates
{
    Never,
    [Description("When a modded mixtape is opened")]
    WhenActive,
    Always
}