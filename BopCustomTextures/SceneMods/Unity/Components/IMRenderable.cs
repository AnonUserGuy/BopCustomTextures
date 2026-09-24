using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
using BopCustomTextures.SceneMods.System;

namespace BopCustomTextures.SceneMods.Unity.Components;

/// <summary>
/// Scene mod component definition for a "renderable" component, I.E. has a <see cref="UnityEngine.Material"/> attached to it.
/// </summary>
public interface IMRenderable
{
    MMaterial MMaterial { get; set; }
}

/// <summary>
/// Static class providing JSON parsing method for <see cref="IMComponent"/>s implementing <see cref="MRenderable"/>.
/// </summary>
public static class MRenderable
{
    public static bool JsonParsePair(CustomJsonInitializer ctx, string key, JToken val, IMRenderable mcomponent)
    {
        if (key.Equals("Material", StringComparison.OrdinalIgnoreCase)
            && MBase.TryJsonParse<MMaterial, Material>(ctx, val, out var mmaterial)) mcomponent.MMaterial = mmaterial;
        else if (key.Equals("Shader", StringComparison.OrdinalIgnoreCase)
            && MBase.TryJsonParse<MShaderMaterial, Material>(ctx, val, out var mmaterial0)) mcomponent.MMaterial = mmaterial0;
        else return false;
        return true;
    }

    [Obsolete("Use MRenderable.JsonParsePair instead.")]
    public static void JsonParse(CustomJsonInitializer ctx, JObject jcomponent, IMRenderable mcomponent)
    {
        foreach (var pair in jcomponent)
        {
            JsonParsePair(ctx, pair.Key, pair.Value, mcomponent);
        }
    }
}
