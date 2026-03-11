using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod UI.<see cref="Image"/> definition
/// </summary>
[MComponent("Image")]
public class MImage : MComponent<Image>, IMRenderable
{
    public Material material;
    public MMaterial mmaterial;

    public Material Material { get => material; set => material = value; }
    public MMaterial MMaterial { get => mmaterial; set => mmaterial = value; }

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        MRenderable.JsonParse(ctx, jcomponent, this);
        return true;
    }

    public override Image Apply(Image component)
    {
        if (material != null) component.material = material;
        if (mmaterial != null) component.material = mmaterial.Apply(component.material);
        return component;
    }
}
