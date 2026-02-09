using BopCustomTextures.SceneMods;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ILogger = BopCustomTextures.Logging.ILogger;
using System.Linq;

namespace BopCustomTextures.Customs;

/// <summary>
/// Class of methods used to apply scene mods.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
public class CustomJsonInitializer(ILogger logger) : BaseCustomManager(logger)
{
    private readonly Dictionary<string, Material> materials = [];
    private readonly Dictionary<string, Material> shaderMaterials = [];

    public MGameObject InitCustomGameObject(JObject jobj, string name = "", bool isVolatile = false)
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
                    var mcomponent = InitCustomComponent((JObject)dict.Value, dict.Key.Substring(1));
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

                var mchildObj = InitCustomGameObject((JObject)dict.Value, childName, isChildVolatile);
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

    public MComponent InitCustomComponent(JObject jcomponent, string name)
    {
        switch (name)
        {
            case "Transform":
                return InitCustomTransform(jcomponent);
            case "SpriteRenderer":
                return InitCustomSpriteRenderer(jcomponent);
            case "ParallaxObjectScript":
                return InitCustomParallaxObjectScript(jcomponent);
            default:
                logger.LogWarning($"JSON Componnent \"{name}\" is an unknown/unsupported component");
                return null;
        }
    }

    // UNITY COMPONENTS //
    public MTransform InitCustomTransform(JObject jtransform)
    {
        var mtransform = new MTransform();
        JObject jobj;
        if (TryGetJObject(jtransform, "LocalPosition", out jobj)) mtransform.localPosition = InitCustomVector3(jobj);
        if (TryGetJObject(jtransform, "LocalRotation", out jobj)) mtransform.localRotation = InitCustomQuaternion(jobj);
        if (TryGetJObject(jtransform, "LocalEulerAngles", out jobj)) mtransform.localEulerAngles = InitCustomVector3(jobj);
        if (TryGetJObject(jtransform, "LocalScale", out jobj)) mtransform.localScale = InitCustomVector3(jobj);
        return mtransform;
    }

    public MSpriteRenderer InitCustomSpriteRenderer(JObject jspriteRenderer)
    {
        var mspriteRenderer = new MSpriteRenderer();
        JObject jobj;
        if (TryGetJObject(jspriteRenderer, "Color", out jobj)) mspriteRenderer.color = InitCustomColor(jobj);
        if (TryGetJObject(jspriteRenderer, "Size", out jobj)) mspriteRenderer.size = InitCustomVector2(jobj);
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
    public MParallaxObjectScript InitCustomParallaxObjectScript(JObject jparallaxObjectScript)
    {
        var mparallaxObjectScript = new MParallaxObjectScript();
        JValue jval;
        if (TryGetJValue(jparallaxObjectScript, "ParallaxScale", JTokenType.Float, out jval)) mparallaxObjectScript.parallaxScale = (float)jval;
        if (TryGetJValue(jparallaxObjectScript, "LoopDistance", JTokenType.Float, out jval)) mparallaxObjectScript.loopDistance = (float)jval;
        return mparallaxObjectScript;
    }

    // STRUCTS //
    public Vector2 InitCustomVector2(JObject jvector2)
    {
        JValue jval;
        return new Vector2(
            TryGetJValue(jvector2, "x", JTokenType.Float, out jval) ? (float)jval : float.NaN,
            TryGetJValue(jvector2, "y", JTokenType.Float, out jval) ? (float)jval : float.NaN
        );
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
    public bool TryGetJValue(JObject jobj, string key, JTokenType type, out JValue jvalue)
    {
        return TryGetJToken(jobj, key, type, out jvalue);
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
        if (!materials.ContainsKey(matName))
        {
            Material found = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(s => s.name == matName);
            if (!found)
            {
                found = Resources.Load<Material>($"Materials/{matName}");
            }
            if (!found)
            {
                logger.LogWarning($"JSON material \"{matName}\" could not be found");
                materials[matName] = null;
            }
            else
            {
                materials[matName] = found;
            }
        }

        mat = materials[matName];
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
        if (!shaderMaterials.ContainsKey(shaderName))
        {
            Shader found = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == shaderName) ?? 
                           Resources.Load<Shader>($"Shaders/{shaderName}");
            if (!found)
            {
                logger.LogWarning($"JSON shader \"{shaderName}\" could not be found");
                shaderMaterials[shaderName] = null;
            }
            else
            {
                shaderMaterials[shaderName] = new Material(found);
            }
        }

        mat = shaderMaterials[shaderName];
        if (!mat)
        {
            return false;
        }
        return true;
    }
}
