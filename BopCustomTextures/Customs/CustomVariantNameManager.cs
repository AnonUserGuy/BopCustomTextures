using BopCustomTextures.Logging;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manager of mapping custom texture variant external names to internal indices.
/// Needed so CustomTextureManager and CustomJsoninitializer can work together.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
public class CustomVariantNameManager(ILogger logger) : BaseCustomManager(logger)
{
    public readonly Dictionary<SceneKey, Dictionary<string, int>> VariantMaps = [];
    public static readonly Regex VariantRegex = new Regex(@"(?:^\s*""?\s*(\w+)[^\w\\/]+|^\s*""?\s*)(\w+)\s*""?\s*$", RegexOptions.RightToLeft | RegexOptions.Compiled);
    public static readonly Regex VariantsRegex = new Regex(@"\s*""?(\w*)""?\s*(?:,|$)", RegexOptions.Compiled);
    
    public void UnloadCustomTextureVariants()
    {
        VariantMaps.Clear();
    }

    public bool TryGetVariant(SceneKey scene, string name, out int variant)
    {
        if (string.IsNullOrEmpty(name))
        {
            variant = 0;
            return true;
        }
        var match = VariantRegex.Match(name);
        if (!match.Success)
        {
            logger.LogError($"Variant \"{name}\" couldn't be parsed");
            variant = -2;
            return false;
        }
        if (!string.IsNullOrEmpty(match.Groups[1].Value))
        {
            scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene == SceneKey.Invalid)
            {
                logger.LogError($"Variant \"{name}\" has an invalid scene \"{match.Groups[1].Value}\"");
                variant = -3;
                return false;
            }
        }
        if (!VariantMaps.TryGetValue(scene, out var variantMap) ||
            !variantMap.TryGetValue(match.Groups[2].Value, out variant))
        {
            logger.LogError($"Variant \"{match.Groups[2].Value}\" doesn't exist in scene {scene}");
            variant = -1;
            return false;
        }
        return true;

    }

    public bool TryGetVariants(SceneKey scene, string names, out List<int> variants)
    {
        var matches = VariantsRegex.Matches(names);
        if (matches.Count < 1)
        {
            logger.LogError($"Variants \"{names}\" couldn't be parsed");
            variants = null;
            return false;
        }
        else if (matches.Count == 1)
        {
            if (!TryGetVariant(scene, matches[0].Groups[1].Value.Trim(), out var variant))
            {
                variants = null;
                return false;
            }
            variants = [variant];
            return true;
        } 
        else
        {
            List<int> result = [];
            for (int i = 0; i < matches.Count - 1; i++)
            {
                if (TryGetVariant(scene, matches[i].Groups[1].Value.Trim(), out var variant))
                {
                    result.Add(variant);
                }
            }
            if (result.Count < 1)
            {
                logger.LogError($"Variants \"{names}\" contained no valid variants");
                variants = null;
                return false;
            }
            variants = result;
            return true;
        }
    }

    public bool TryGetVariantAll(string name, out Dictionary<SceneKey, int> variants)
    {
        if (string.IsNullOrEmpty(name))
        {
            variants = [];
            foreach (var pair in VariantMaps)
            {
                variants[pair.Key] = 0;
            }
            return true;
        }
        var match = VariantRegex.Match(name);
        if (!match.Success)
        {
            logger.LogError($"Variant \"{name}\" couldn't be parsed");
            variants = null;
            return false;
        }
        if (!string.IsNullOrEmpty(match.Groups[1].Value))
        {
            var scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene == SceneKey.Invalid)
            {
                logger.LogError($"Variant \"{name}\" has an invalid scene \"{match.Groups[1].Value}\"");
                variants = null;
                return false;
            }
            if (!VariantMaps.TryGetValue(scene, out var variantMap) ||
                !variantMap.TryGetValue(match.Groups[2].Value, out var variant3))
            {
                logger.LogError($"Variant \"{match.Groups[2].Value}\" doesn't exist in scene {scene}");
                variants = null;
                return false;
            }
            variants = [];
            foreach (var pair in VariantMaps)
            {
                variants[pair.Key] = variant3;
            }
            return true;
        }
        Dictionary<SceneKey, int> result = [];
        foreach (var pair in VariantMaps)
        {
            if (pair.Value.TryGetValue(match.Groups[2].Value, out var variant))
            {
                result[pair.Key] = variant;
            }
        }
        if (result.Count < 1)
        {
            logger.LogError($"Variant \"{match.Groups[2].Value}\" doesn't exist in any scene");
            variants = null;
            return false;
        }
        variants = result;
        return true;
    }

    public bool TryGetVariantsAll(string names, out Dictionary<SceneKey, List<int>> variants)
    {
        var matches = VariantsRegex.Matches(names);
        if (matches.Count < 1)
        {
            logger.LogError($"Variants \"{names}\" couldn't be parsed");
            variants = null;
            return false;
        }
        else if (matches.Count == 1)
        {
            if (!TryGetVariantAll(matches[0].Groups[1].Value.Trim(), out var variants2))
            {
                variants = null;
                return false;
            }
            variants = [];
            foreach (var pair in variants2)
            {
                variants[pair.Key] = [pair.Value];
            }
            return true;
        }
        else
        {
            Dictionary<SceneKey, List<int>> result = [];
            for (int i = 0; i < matches.Count - 1; i++)
            {
                if (TryGetVariantAll(matches[i].Groups[1].Value.Trim(), out var variant))
                {
                    foreach (var key in variant)
                    {
                        if (!result.ContainsKey(key.Key))
                        {
                            result[key.Key] = [key.Value];
                        }
                        else
                        {
                            result[key.Key].Add(key.Value);
                        }
                    }
                }
            }
            if (result.Count < 1)
            {
                logger.LogError($"Variants \"{names}\" contained no valid variants");
                variants = null;
                return false;
            }
            variants = result;
            return true;
        }
    }

    public int GetOrAddVariant(SceneKey scene, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return 0;
        }
        int value;
        if (!VariantMaps.TryGetValue(scene, out var variantMap))
        {
            variantMap = [];
            VariantMaps[scene] = variantMap;
        }
        else if (variantMap.TryGetValue(name, out value))
        {
            return value;
        }
        value = variantMap.Count + 1;
        variantMap[name] = value;
        return value;
    }
}
