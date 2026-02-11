using BopCustomTextures.SceneMods;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Linq;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Used to parse JSON-defined scene mods.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Shared with CustomTextureManager.</param>
public class CustomJsonInitializer(ILogger logger, CustomVariantNameManager variantManager) : BaseCustomManager(logger)
{
    private readonly Dictionary<string, Material> Materials = [];
    private readonly Dictionary<string, Material> ShaderMaterials = [];
    private CustomVariantNameManager VariantManager = variantManager;

    public MGameObject InitGameObject(JObject jobj, SceneKey scene, string name = "", bool isVolatile = false)
    {
        var mobj = new MGameObject(name);
        var components = new List<MComponent>();
        var childObjs = new List<MGameObject>();
        var childObjsVolatile = new List<MGameObject>();
        foreach (KeyValuePair<string, JToken> dict in jobj)
        {
            if (dict.Key.StartsWith("!"))
            {
                if (dict.Key == "!Active")
                {
                    if (dict.Value.Type != JTokenType.Boolean) {
                        logger.LogWarning($"JSON Active \"{dict.Key}\" is a {dict.Value.Type} when it should be a Boolean");
                        continue;
                    }
                    mobj.active = (bool)dict.Value;
                } 
                else
                {
                    if (dict.Value.Type != JTokenType.Object)
                    {
                        logger.LogWarning($"JSON Componnent \"{dict.Key}\" is a {dict.Value.Type} when it should be a Object");
                        continue;
                    }
                    var mcomponent = InitComponent((JObject)dict.Value, scene, dict.Key.Substring(1));
                    if (mcomponent != null)
                    {
                        components.Add(mcomponent);
                    }
                }
            }
            else
            {
                if (dict.Value.Type != JTokenType.Object)
                {
                    logger.LogWarning($"JSON GameObject \"{dict.Key}\" is a {dict.Value.Type} when it should be a Object");
                    continue;
                }

                string childName = dict.Key;
                bool isChildVolatile = isVolatile;
                if (childName.StartsWith("~")) {
                    isChildVolatile = true;
                    childName = childName.Substring(1);
                }

                var mchildObj = InitGameObject((JObject)dict.Value, scene, childName, isChildVolatile);
                if (isChildVolatile)
                {
                    childObjsVolatile.Add(mchildObj);
                } 
                else
                {
                    childObjs.Add(mchildObj);
                }
            }
        }
        mobj.components = components.ToArray();
        mobj.childObjs = childObjs.ToArray();
        mobj.childObjsVolatile = childObjsVolatile.ToArray();
        return mobj;
    }

    public MComponent InitComponent(JObject jcomponent, SceneKey scene, string name)
    {
        switch (name)
        {
            case "Transform":
                return InitTransform(jcomponent);
            case "SpriteRenderer":
                return InitSpriteRenderer(jcomponent);
            case "ParallaxObjectScript":
                return InitParallaxObjectScript(jcomponent);
            case "CustomSpriteSwapper":
                return InitCustomSpriteSwapper(jcomponent, scene);
            default:
                logger.LogWarning($"JSON Componnent \"{name}\" is an unknown/unsupported component");
                return null;
        }
    }

    // UNITY COMPONENTS //
    public MTransform InitTransform(JObject jtransform)
    {
        var mtransform = new MTransform();
        if (TryGetJVector3(jtransform, "LocalPosition", out var vector3)) mtransform.localPosition = vector3;
        if (TryGetJQuaternion(jtransform, "LocalRotation", out var quaternion)) mtransform.localRotation = quaternion;
        if (TryGetJEulerAngles(jtransform, "LocalEulerAngles", out vector3)) mtransform.localEulerAngles = vector3;
        if (TryGetJVector3(jtransform, "LocalEulerAngles", out vector3)) mtransform.localEulerAngles = vector3;
        if (TryGetJVector3(jtransform, "LocalScale", out vector3)) mtransform.localScale = vector3;
        return mtransform;
    }

    public MSpriteRenderer InitSpriteRenderer(JObject jspriteRenderer)
    {
        var mspriteRenderer = new MSpriteRenderer();
        if (TryGetJColor(jspriteRenderer, "Color", out var color)) mspriteRenderer.color = color;
        if (TryGetJVector2(jspriteRenderer, "Size", out var vector2)) mspriteRenderer.size = vector2;
        JValue jval;
        if (TryGetJValue(jspriteRenderer, "FlipX", JTokenType.Boolean, out jval)) mspriteRenderer.flipX = (bool)jval;
        if (TryGetJValue(jspriteRenderer, "FlipY", JTokenType.Boolean, out jval)) mspriteRenderer.flipY = (bool)jval;
        Material mat;
        if (TryGetJMaterial(jspriteRenderer, "Material", out mat) || 
            TryGetJShader(jspriteRenderer, "Shader", out mat))
            mspriteRenderer.material = mat;
        return mspriteRenderer;
    }

    // BITS & BOPS SCRIPTS //
    public MParallaxObjectScript InitParallaxObjectScript(JObject jparallaxObjectScript)
    {
        var mparallaxObjectScript = new MParallaxObjectScript();
        JValue jval;
        if (TryGetJValue(jparallaxObjectScript, "ParallaxScale", JTokenType.Float, out jval)) mparallaxObjectScript.parallaxScale = (float)jval;
        if (TryGetJValue(jparallaxObjectScript, "LoopDistance", JTokenType.Float, out jval)) mparallaxObjectScript.loopDistance = (float)jval;
        return mparallaxObjectScript;
    }

    // THERE I AM GARY, THERE I AM! //

    public MCustomSpriteSwapper InitCustomSpriteSwapper(JObject jcustomSpriteSwapper, SceneKey scene)
    {
        var mcustomSpriteSwapper = new MCustomSpriteSwapper();
        if (jcustomSpriteSwapper.TryGetValue("Variants", out var jvariants))
        {
            switch (jvariants.Type)
            {
                case JTokenType.Array:
                    mcustomSpriteSwapper.variants = [];
                    foreach (var jel in (JArray)jvariants)
                    {
                        if (TryGetVariant(jel, scene, out var variant))
                        {
                            mcustomSpriteSwapper.variants.Add(variant);
                        }
                    }
                    break;
                case JTokenType.Object:
                    mcustomSpriteSwapper.variantsIndexed = [];
                    var jobj = (JObject)jvariants;
                    foreach (var pair in jobj)
                    {
                        if (!int.TryParse(pair.Key, out var index))
                        {
                            logger.LogWarning($"JSON variant \"{pair.Key}\" does not have an integer key");
                            continue;
                        }
                        if (TryGetVariant(pair.Value, scene, out var variant))
                        {
                            mcustomSpriteSwapper.variantsIndexed[index] = variant;
                        }
                    }
                    break;
                case JTokenType.String:
                case JTokenType.Integer:
                    if (TryGetVariant(jvariants, scene, out var variant2))
                    {
                        mcustomSpriteSwapper.variants = [variant2];
                    }
                    break;
                default:
                    logger.LogWarning($"JSON variants is a {jvariants.Type} when it should be an array, object, string, or integer");
                    break;
            }
        }
        return mcustomSpriteSwapper;
    }

    public bool TryGetVariant(JToken jtoken, SceneKey scene, out int variant)
    {
        switch (jtoken.Type)
        {
            case JTokenType.String:
                if (!VariantManager.TryGetVariant(scene, (string)jtoken, out variant))
                {
                    return false;
                }
                return true;
            case JTokenType.Integer:
                variant = (int)jtoken;
                return true;
        }
        logger.LogWarning($"JSON variant \"{jtoken}\" is a {jtoken.Type} when it should be a string or integer");
        variant = -1;
        return false;
    }

    // STRUCTS //
    public bool TryGetJVector2(JObject jobj, string key, out Vector2 vector2)
    {
        if (!jobj.TryGetValue(key, out var jvector2))
        {
            vector2 = default;
            return false;
        }
        switch (jvector2)
        {
            case JObject jobj2:
                vector2 = InitCustomVector2(jobj2);
                return true;
            case JArray jarray2:
                vector2 = InitCustomVector2(jarray2);
                return true;
        }
        logger.LogWarning($"JSON vector2 \"{key}\" is a {jvector2.Type} when it should be an object or array");
        vector2 = default;
        return false;
    }
    public Vector2 InitCustomVector2(JObject jvector2)
    {
        JValue jval;
        return new Vector2(
            TryGetJValue(jvector2, "x", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector2, "y", JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }
    public Vector2 InitCustomVector2(JArray jvector2)
    {
        JValue jval;
        return new Vector2(
            TryGetJValue(jvector2, 0, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector2, 1, JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }

    public bool TryGetJVector3(JObject jobj, string key, out Vector3 vector3)
    {
        if (!jobj.TryGetValue(key, out var jvector3))
        {
            vector3 = default;
            return false;
        }
        switch (jvector3)
        {
            case JObject jobj2:
                vector3 = InitCustomVector3(jobj2);
                return true;
            case JArray jarray2:
                vector3 = InitCustomVector3(jarray2);
                return true;
        }
        logger.LogWarning($"JSON vector3 \"{key}\" is a {jvector3.Type} when it should be an object or array");
        vector3 = default;
        return false;
    }
    public Vector3 InitCustomVector3(JObject jvector3)
    {
        JValue jval;
        return new Vector3(
            TryGetJValue(jvector3, "x", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector3, "y", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector3, "z", JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }
    public Vector3 InitCustomVector3(JArray jvector3)
    {
        JValue jval;
        return new Vector3(
            TryGetJValue(jvector3, 0, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector3, 1, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector3, 2, JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }

    public bool TryGetJEulerAngles(JObject jobj, string key, out Vector3 eulerAngles)
    {
        if (!jobj.TryGetValue(key, out var jvector3))
        {
            eulerAngles = default;
            return false;
        }
        switch (jvector3.Type)
        {
            case JTokenType.Object:
                eulerAngles = InitCustomVector3((JObject)jvector3);
                return true;
            case JTokenType.Array:
                eulerAngles = InitCustomVector3((JArray)jvector3);
                return true;
            case JTokenType.Float:
                eulerAngles = new Vector3(float.NaN, (float)jvector3, float.NaN);
                return true;
        }
        logger.LogWarning($"JSON eulerAngles \"{key}\" is a {jvector3.Type} when it should be an object, array, or float");
        eulerAngles = default;
        return false;
    }

    public bool TryGetJQuaternion(JObject jobj, string key, out Quaternion quaternion)
    {
        if (!jobj.TryGetValue(key, out var jquaternion))
        {
            quaternion = default;
            return false;
        }
        switch (jquaternion)
        {
            case JObject jobj2:
                quaternion = InitCustomQuaternion(jobj2);
                return true;
            case JArray jarray2:
                quaternion = InitCustomQuaternion(jarray2);
                return true;
        }
        logger.LogWarning($"JSON quaternion \"{key}\" is a {jquaternion.Type} when it should be an object or array");
        quaternion = default;
        return false;
    }
    public Quaternion InitCustomQuaternion(JObject jquaternion)
    {
        JValue jval;
        return new Quaternion(
            TryGetJValue(jquaternion, "x", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jquaternion, "y", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jquaternion, "z", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jquaternion, "w", JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }
    public Quaternion InitCustomQuaternion(JArray jquaternion)
    {
        JValue jval;
        return new Quaternion(
            TryGetJValue(jquaternion, 0, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jquaternion, 1, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jquaternion, 2, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jquaternion, 3, JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }

    public bool TryGetJColor(JObject jobj, string key, out Color color)
    {
        if (!jobj.TryGetValue(key, out var jcolor))
        {
            color = default;
            return false;
        }
        switch (jcolor.Type)
        {
            case JTokenType.Object:
                color = InitCustomColor((JObject)jcolor);
                return true;
            case JTokenType.Array:
                color = InitCustomColor((JArray)jcolor);
                return true;
            case JTokenType.String:
                color = InitCustomColor((string)jcolor);
                return true;
        }
        logger.LogWarning($"JSON color \"{key}\" is a {jcolor.Type} when it should be an object, array, or string");
        color = default;
        return false;
    }
    public Color InitCustomColor(JObject jcolor)
    {
        JValue jval;
        return new Color(
            TryGetJValue(jcolor, "r", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jcolor, "g", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jcolor, "b", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jcolor, "a", JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }
    public Color InitCustomColor(JArray jcolor)
    {
        JValue jval;
        return new Color(
            TryGetJValue(jcolor, 0, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jcolor, 1, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jcolor, 2, JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jcolor, 3, JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
    }
    public Color InitCustomColor(string str)
    {
        str = str.TrimStart('#');
        int rgb = Convert.ToInt32(str, 16);
        Color jcolor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
        for (int i = 0; i < str.Length / 2 && i < 4; i++)
        {
            jcolor[i] = (rgb & 0xFF) / 255.0f;
            rgb >>= 8;
        }
        return jcolor;
    }


    // UTILITY // 
    public bool TryGetJToken<T>(JObject jobj, string key, JTokenType type, out T jtoken) where T: JToken
    {
        if (!jobj.TryGetValue(key, out var jtoken2))
        {
            jtoken = null;
            return false;
        }
        if (jtoken2.Type != type)
        {
            logger.LogWarning($"JSON key \"{key}\" is a {jtoken2.Type} when it should be a {type}");
            jtoken = null;
            return false;
        }
        jtoken = (T)jtoken2;
        return true;
    }
    public bool TryGetJToken<T>(JArray jarray, int index, JTokenType type, out T jtoken) where T : JToken
    {
        if (jarray.Count <= index)
        {
            jtoken = null;
            return false;
        }
        var jtoken2 = jarray[index];
        if (jtoken2.Type != type)
        {
            logger.LogWarning($"JSON index \"{index}\" is a {jtoken2.Type} when it should be a {type}");
            jtoken = null;
            return false;
        }
        jtoken = (T)jtoken2;
        return true;
    }

    public bool TryGetJValue(JObject jobj, string key, JTokenType type, out JValue jvalue)
    {
        return TryGetJToken(jobj, key, type, out jvalue);
    }
    public bool TryGetJValue(JArray Jarray, int index, JTokenType type, out JValue jvalue)
    {
        return TryGetJToken(Jarray, index, type, out jvalue);
    }
    public bool TryGetJObject(JObject jobj, string key, out JObject jvalue)
    {
        return TryGetJToken(jobj, key, JTokenType.Object, out jvalue);
    }

    public bool TryGetJMaterial(JObject jobj, string key, out Material mat)
    {
        if (!TryGetJValue(jobj, key, JTokenType.String, out var jmatName))
        {
            mat = null;
            return false;
        }
        string matName = (string)jmatName;
        if (!Materials.ContainsKey(matName))
        {
            Material found = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(s => s.name == matName);
            if (!found)
            {
                found = Resources.Load<Material>($"Materials/{matName}");
            }
            if (!found)
            {
                logger.LogWarning($"JSON material \"{matName}\" could not be found");
                Materials[matName] = null;
            }
            else
            {
                Materials[matName] = found;
            }
        }

        mat = Materials[matName];
        if (!mat)
        {
            return false;
        }
        return true;
        
    }
    public bool TryGetJShader(JObject jobj, string key, out Material mat)
    {
        if (!TryGetJValue(jobj, key, JTokenType.String, out var jshaderName))
        {
            mat = null;
            return false;
        }
        string shaderName = (string)jshaderName;
        if (!ShaderMaterials.ContainsKey(shaderName))
        {
            Shader found = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == shaderName) ?? 
                           Resources.Load<Shader>($"Shaders/{shaderName}");
            if (!found)
            {
                logger.LogWarning($"JSON shader \"{shaderName}\" could not be found");
                ShaderMaterials[shaderName] = null;
            }
            else
            {
                ShaderMaterials[shaderName] = new Material(found);
            }
        }

        mat = ShaderMaterials[shaderName];
        if (!mat)
        {
            return false;
        }
        return true;
    }
}
