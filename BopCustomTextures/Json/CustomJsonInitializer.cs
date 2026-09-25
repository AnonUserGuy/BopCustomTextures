using BopCustomTextures.Customs;
using BopCustomTextures.SceneMods.Unity;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Json;

/// <summary>
/// Used to parse JSON-defined scene mods.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Shared with CustomTextureManager.</param>
public class CustomJsonInitializer(ILogger logger, CustomVariantNameManager variantManager) : BaseCustomManager(logger)
{
    public MixtapeInfo Mixtape;

    private SceneKey LastScene = default;
    private readonly Dictionary<string, Material> Materials = [];
    private readonly Dictionary<string, Shader> Shaders = [];
    private readonly Dictionary<string, Material> ShaderMaterials = [];
    private readonly CustomVariantNameManager VariantManager = variantManager;

    public bool TryGetVariant(string name, out int variant)
    {
        return VariantManager.TryGetVariant(LastScene, name, out variant);
    }

    public MGameObject InitGameObject(JToken jtoken, SceneKey scene, string name = "", bool isDeferred = false)
    {
        LastScene = scene;
        return InitGameObject(jtoken, name, isDeferred);
    }

    public MGameObject InitGameObject(JToken jtoken, string name = "", bool isDeferred = false)
    {
        var mobj = new MGameObject { name = name, isDeferred = isDeferred };
        return mobj.JsonParse(this, jtoken) ? mobj : null;
    }

    public bool TryGetMaterial(string name, out Material material)
    {
        if (!Materials.ContainsKey(name))
        {
            Material found = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(s => s.name == name) ??
                Resources.Load<Material>($"Materials/{name}");
            if (!found)
            {
                Logger.LogWarning($"JSON material \"{name}\" could not be found");
                Materials[name] = null;
            }
            else
            {
                Materials[name] = found;
            }
        }

        material = Materials[name];
        if (!material)
        {
            return false;
        }
        return true;
    }

    public bool TryGetShaderMaterial(string shaderName, out Material mat)
    {
        if (!ShaderMaterials.ContainsKey(shaderName))
        {
            if (TryGetShader(shaderName, out var shader))
            {
                ShaderMaterials[shaderName] = new Material(shader);
            }
            else
            {
                ShaderMaterials[shaderName] = null;
            }
        }
        mat = ShaderMaterials[shaderName];
        if (!mat)
        {
            return false;
        }
        return true;
    }

    public bool TryGetShader(string name, out Shader shader)
    {
        if (!Shaders.ContainsKey(name))
        {
            Shader found = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == name) ??
                   Shader.Find(name);
            if (!found)
            {
                Logger.LogWarning($"JSON shader \"{name}\" could not be found");
                Shaders[name] = null;
            }
            else
            {
                Shaders[name] = found;
            }
        }
        shader = Shaders[name];
        if (!shader)
        {
            return false;
        }
        return true;
    }

    public bool TryGetJToken<T>(JObject jobj, string key, JTokenType type, out T jtoken) where T : JToken
    {
        if (!jobj.TryGetValue(key, out var jtoken2))
        {
            jtoken = null;
            return false;
        }
        if (jtoken2.Type != type)
        {
            Logger.LogWarning($"JSON key \"{key}\" is a {jtoken2.Type} when it should be a {type}");
            jtoken = null;
            return false;
        }
        jtoken = (T)jtoken2;
        return true;
    }

    public bool TryGetJObject(JObject jobj, string key, out JObject jvalue)
    {
        return TryGetJToken(jobj, key, JTokenType.Object, out jvalue);
    }
}
