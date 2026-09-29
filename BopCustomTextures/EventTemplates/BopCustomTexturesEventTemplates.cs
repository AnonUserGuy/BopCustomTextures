using System.Collections.Generic;
using static MixtapeEventTemplates;

namespace BopCustomTextures.EventTemplates;

/// <summary>
/// Static class of BopCustomTexture mixtape event templates.
/// </summary>
public static class BopCustomTexturesEventTemplates
{
    public static readonly string[] PropertyCopyOptions = [
        "copy from file",
        "copy from folder",
    ];
    public static readonly string[] PropertyReloadOptions = [
        "reload"
    ];

    public static readonly Dictionary<string, object> EditorPropertiesTemplatePropertiesBase = new()
    {
        ["last path"] = ""
    };

    public static readonly MixtapeEventTemplate EditorPropertiesTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/editor properties",
        length = 0.5f,
        properties = EditorPropertiesTemplatePropertiesBase
    };

    public static readonly MixtapeEventTemplate MixtapePropertiesTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/mixtape properties",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["version"] = "",
            ["release"] = 0,
            ["unsafe"] = false
        }
    };

    public static readonly MixtapeEventTemplate SceneModTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/apply scene mod",
        length = 0.5f,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["key"] = "",
        }
    };

    public static readonly MixtapeEventTemplate OffsetSceneModTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/apply offset scene mod",
        length = 0.5f,
        resizable = true,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["key"] = "",
            ["offset"] = 0.0,
            ["useLength"] = false,
            ["applyByEnd"] = false,
        }
    };

    public static readonly MixtapeEventTemplate InputSyncedSceneModTemplate = new()
    {
        dataModel = $"{MyPluginInfo.PLUGIN_GUID}/apply scene mod on input",
        length = 0.5f,
        resizable = true,
        properties = new Dictionary<string, object>
        {
            ["scene"] = "",
            ["key"] = "",
            ["beatOffset"] = 0.0,
            ["offset"] = 0.0,
            ["count"] = 1,
            ["action"] = new ChoiceField<string>(["primary", "secondary"]),
            ["onUp"] = false,
            ["minJudgement"] = new ChoiceField<string>(["miss", "bad", "almost", "hit", "perfect"]),
            ["maxJudgement"] = new ChoiceField<string>(["perfect", "miss", "bad", "almost", "hit"]),
            ["early"] = true,
            ["late"] = true,
            ["applyByMiss"] = false,
            ["useLength"] = false,
            ["applyByEnd"] = false,
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

    public static readonly MixtapeEventTemplate[] SceneModTemplates =
    [
        SceneModTemplate,
        OffsetSceneModTemplate,
        InputSyncedSceneModTemplate
    ];

    public static readonly MixtapeEventTemplate[] Templates =
    [
        EditorPropertiesTemplate,
        MixtapePropertiesTemplate,
        ToggleCustomTexturesTemplate,
        SetTextureVariantTemplate,
        AddTextureVariantTemplate,
        RemoveTextureVariantTemplate,
        SceneModTemplate,
        OffsetSceneModTemplate,
        InputSyncedSceneModTemplate
    ];
}
