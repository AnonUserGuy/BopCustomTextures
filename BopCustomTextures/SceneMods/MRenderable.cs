using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;

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
    public static bool JsonParsePair(CustomJsonInitializer ctx, string key, JToken val, IMRenderable mcomponent)
    {
        if (key.Equals("material", StringComparison.OrdinalIgnoreCase))
        {
            switch (val.Type)
            {
                case JTokenType.String:
                    if (ctx.TryGetMaterial((string)val, out var mat))
                    {
                        mcomponent.Material = mat;
                    }
                    break;
                case JTokenType.Object:
                    var jmaterial = (JObject)val;
                    mcomponent.MMaterial = ctx.InitMaterial(jmaterial);
                    mcomponent.Material = mcomponent.MMaterial.material;
                    break;
            }
            return true;
        }
        else if (ctx.TryGetJShaderMaterial(key, val, "shader", out Material mat))
        {
            mcomponent.Material = mat;
            return true;
        }
        return false;
    }

    [Obsolete("MRenderable.JsonParse is case sensitive, use MRenderable.JsonParsePair instead.")]
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
                    mcomponent.MMaterial = ctx.InitMaterial(jmaterial);
                    mcomponent.Material = mcomponent.MMaterial.material;
                    break;
            }
        }
        else if (ctx.TryGetJShaderMaterial(jcomponent, "Shader", out var mat))
        {
            mcomponent.Material = mat;
        }
    }
}
