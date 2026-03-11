using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod component definition for a "renderable" component, I.E. has a <see cref="UnityEngine.Material"/> attached to it.
/// </summary>
public interface IMRenderable
{
    MMaterial MMaterial { get; set; }
    Material Material { get; set; }
}

/// <summary>
/// Static class providing JSON parsing method for <see cref="IMComponent"/>s implementing <see cref="IMRenderable"/>.
/// </summary>
public static class MRenderable
{
    public static void JsonParse(CustomJsonInitializer ctx, JObject jcomponent, IMRenderable mcomponent)
    {
        if (jcomponent.TryGetValue("Material", out var jmat))
        {
            switch (jmat.Type)
            {
                case JTokenType.String:
                    if (ctx.TryGetMaterial((string)jmat, out var mat))
                    {
                        mcomponent.Material = mat;
                    }
                    break;
                case JTokenType.Object:
                    var jmaterial = (JObject)jmat;
                    if (ctx.TryGetJMaterial(jmaterial, "Name", out mat) ||
                        ctx.TryGetJMaterial(jmaterial, "Material", out mat))
                    {
                        mcomponent.Material = mat;
                    }
                    mcomponent.MMaterial = ctx.InitMaterial(jmaterial);
                    break;
            }
        }
        else if (ctx.TryGetJShaderMaterial(jcomponent, "Shader", out var mat))
        {
            mcomponent.Material = mat;
        }
    }
}
