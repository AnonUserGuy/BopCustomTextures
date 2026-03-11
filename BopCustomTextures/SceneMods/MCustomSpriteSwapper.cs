using BopCustomTextures.Json;
using BopCustomTextures.Scripts;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="CustomSpriteSwapper"/> definition.
/// </summary>
[MComponent("CustomSpriteSwapper")]
public class MCustomSpriteSwapper : MBehaviour<CustomSpriteSwapper>
{
    public List<int> variants;
    public Dictionary<int, int> variantsIndexed;

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        base.JsonParse(ctx, jcomponent);
        if (jcomponent.TryGetValue("Variants", out var jvariants))
        {
            switch (jvariants.Type)
            {
                case JTokenType.Array:
                    variants = [];
                    foreach (var jel in (JArray)jvariants)
                    {
                        if (ctx.TryGetVariant(jel, out var variant))
                        {
                            variants.Add(variant);
                        }
                    }
                    break;
                case JTokenType.Object:
                    variantsIndexed = [];
                    var jobj = (JObject)jvariants;
                    foreach (var pair in jobj)
                    {
                        if (!int.TryParse(pair.Key, out var index))
                        {
                            ctx.logger.LogWarning($"JSON variant \"{pair.Key}\" does not have an integer key");
                            continue;
                        }
                        if (ctx.TryGetVariant(pair.Value, out var variant))
                        {
                            variantsIndexed[index] = variant;
                        }
                    }
                    break;
                case JTokenType.String:
                case JTokenType.Integer:
                    if (ctx.TryGetVariant(jvariants, out var variant2))
                    {
                        variants = [variant2];
                    }
                    break;
                default:
                    ctx.logger.LogWarning($"JSON variants is a {jvariants.Type} when it should be an array, object, string, or integer");
                    break;
            }
        }
        return true;
    }

    public override CustomSpriteSwapper Apply(CustomSpriteSwapper component)
    {
        base.Apply(component);
        if (variants != null) component.ApplyVariants(variants);
        else if (variantsIndexed != null) component.ApplyVariants(variantsIndexed);
        return component;
    }
}
