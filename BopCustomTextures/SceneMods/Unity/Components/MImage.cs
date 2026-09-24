using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Scene mod UI.<see cref="Image"/> definition
/// </summary>
[MComponent("Image")]
public class MImage : MBehaviour<Image>, IMRenderable
{
    public MMaterial mmaterial;
    public MMaterial MMaterial { get => mmaterial; set => mmaterial = value; }

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (!MRenderable.JsonParsePair(ctx, key, val, this))
        {
            base.JsonParsePair(ctx, key, val);
        }
    }

    public override Image ApplyInternal(Image component)
    {
        if (mmaterial != null) component.material = mmaterial.Apply(component.material);
        return base.ApplyInternal(component);
    }
}
