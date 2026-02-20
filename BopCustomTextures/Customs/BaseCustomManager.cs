using System;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Base class for all custom asset managers.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
public class BaseCustomManager(ILogger logger)
{
    public ILogger logger = logger;

    private static readonly Regex SceneKeyRegex = new Regex("^(.*?)(?:Custom|Mixtape)?$", RegexOptions.Compiled);

    protected static SceneKey ToSceneKeyOrInvalid(string name)
    {
        string[] namesAffixed =
        [
        name,
        name + "Custom",
        name + "Mixtape"
        ];
        foreach (string name2 in namesAffixed)
        {
            SceneKey[] sceneKeys = MixtapeLoaderCustom.allSceneKeys;
            for (int j = 0; j < sceneKeys.Length; j++)
            {
                SceneKey result = sceneKeys[j];
                string keyName = result.ToString();
                if (string.Equals(name2, keyName, StringComparison.OrdinalIgnoreCase))
                {
                    return result;
                }
            }
        }
        return SceneKey.Invalid;
    }

    protected static string FromSceneKeyOrInvalid(SceneKey scene)
    {
        var sceneStr = scene.ToString();
        var match = SceneKeyRegex.Match(sceneStr);
        if (match.Success)
        {
            return match.Groups[1].Value;
        } 
        else
        {
            return sceneStr;
        }
    }
}
