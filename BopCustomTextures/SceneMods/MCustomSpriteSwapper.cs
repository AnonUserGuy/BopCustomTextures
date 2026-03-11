using BopCustomTextures.Json;
using BopCustomTextures.Scripts;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="CustomSpriteSwapper"/> definition.
/// </summary>
public class MCustomSpriteSwapper : MBehaviour<CustomSpriteSwapper>
{
    public List<int> variants;
    public Dictionary<int, int> variantsIndexed;

    public static void Register()
    {
        MComponentParserRegistry.Register("CustomSpriteSwapper", JsonParse);
    }

    public static MCustomSpriteSwapper JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        var mcomponent = new MCustomSpriteSwapper();
        JsonParse(ctx, jcomponent, mcomponent);
        if (jcomponent.TryGetValue("Variants", out var jvariants))
        {
            switch (jvariants.Type)
            {
                case JTokenType.Array:
                    mcomponent.variants = [];
                    foreach (var jel in (JArray)jvariants)
                    {
                        if (ctx.TryGetVariant(jel, out var variant))
                        {
                            mcomponent.variants.Add(variant);
                        }
                    }
                    break;
                case JTokenType.Object:
                    mcomponent.variantsIndexed = [];
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
                            mcomponent.variantsIndexed[index] = variant;
                        }
                    }
                    break;
                case JTokenType.String:
                case JTokenType.Integer:
                    if (ctx.TryGetVariant(jvariants, out var variant2))
                    {
                        mcomponent.variants = [variant2];
                    }
                    break;
                default:
                    ctx.logger.LogWarning($"JSON variants is a {jvariants.Type} when it should be an array, object, string, or integer");
                    break;
            }
        }
        return mcomponent;
    }

    public override CustomSpriteSwapper Apply(CustomSpriteSwapper component)
    {
        base.Apply(component);
        if (variants != null) component.ApplyVariants(variants);
        else if (variantsIndexed != null) component.ApplyVariants(variantsIndexed);
        return component;
    }
}
