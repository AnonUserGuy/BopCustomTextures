using BopCustomTextures.Json;
using BopCustomTextures.Scripts;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using BopCustomTextures.SceneMods.System.Generic;
using BopCustomTextures.SceneMods.Unity.Components;

namespace BopCustomTextures.SceneMods.Scripts;

/// <summary>
/// Scene mod <see cref="CustomSpriteSwapper"/> definition.
/// </summary>
[MComponent("CustomSpriteSwapper")]
public class MCustomSpriteSwapper : MBehaviour<CustomSpriteSwapper>
{
    public MIList<List<int>, MVariant, int> variants;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken jvariants)
    {
        if (KeyMatch(key, "Variants"))
        {
            if (TryJsonParse<MIList<List<int>, MVariant, int>, List<int>>(ctx, jvariants, out var mvariants)) variants = mvariants;
        }
        else base.JsonParsePair(ctx, key, jvariants);
    }

    public override CustomSpriteSwapper ApplyInternal(CustomSpriteSwapper component)
    {
        if (variants != null)
        {
            variants.Apply(component.variants);
            component.Replace();
        }
        return base.ApplyInternal(component);
    }
}
