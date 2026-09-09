using System.Collections.Generic;

namespace BopCustomTextures.EventTemplates;

/// <summary>
/// Static class of BopCustomTexture mixtape event templates.
/// </summary>
public class BopCustomTexturesEventTemplates
{
    public static readonly MixtapeEventTemplate SceneModTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/apply scene mod",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["key"] = ""
        }
    };

    public static readonly MixtapeEventTemplate AddTextureVariantTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/add texture variant",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["variant"] = ""
        }
    };

    public static readonly MixtapeEventTemplate RemoveTextureVariantTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/remove texture variant",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["variant"] = ""
        }
    };

    public static readonly MixtapeEventTemplate SetTextureVariantTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/set texture variant",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["variant"] = ""
        }
    };

    public static readonly MixtapeEventTemplate ToggleCustomTexturesTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/toggle custom textures",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["toggle"] = true
        }
    };

    public static readonly MixtapeEventTemplate[] TextureVariantTemplates =
    [
        ToggleCustomTexturesTemplate,
        SetTextureVariantTemplate,
        AddTextureVariantTemplate,
        RemoveTextureVariantTemplate
    ];

    public static readonly MixtapeEventTemplate[] Templates =
    [
        ToggleCustomTexturesTemplate,
        SetTextureVariantTemplate,
        AddTextureVariantTemplate,
        RemoveTextureVariantTemplate,
        SceneModTemplate
    ];
}
