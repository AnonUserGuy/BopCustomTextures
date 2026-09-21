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
    public MIListSingleable<MVariant, int> variants;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken jvariants)
    {
        if (KeyMatch(key, "Variants"))
        {
            if (TryJsonParse<MIListSingleable<MVariant, int>, IList<int>>(ctx, jvariants, out var mvariants)) variants = mvariants;
        }
        else base.JsonParsePair(ctx, key, jvariants);
    }

    public override CustomSpriteSwapper Apply(CustomSpriteSwapper component)
    {
        if (variants != null)
        {
            variants.Apply(component.variants);
            component.Replace();
        }
        return base.Apply(component);
    }
}
