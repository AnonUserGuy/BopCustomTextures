using System.Collections.Generic;

namespace BopCustomTextures.EventTemplates;

public class BopCustomTexturesEventTemplates
{
    public static readonly MixtapeEventTemplate sceneModTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/apply scene mod",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["key"] = "",
        }
    };

    public static readonly MixtapeEventTemplate[] templates =
    [
        sceneModTemplate
    ];
}
