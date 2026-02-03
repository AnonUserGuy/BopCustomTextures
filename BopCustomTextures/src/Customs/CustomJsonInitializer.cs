using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ILogger = BopCustomTextures.Logging.ILogger;
using System.Text.RegularExpressions;
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

    public void InitCustomGameObject(JObject jobj, GameObject obj)
    {
        foreach (KeyValuePair<string, JToken> dict in jobj)
        {
            if (dict.Key.StartsWith("!"))
            {
                if (dict.Value.Type != JTokenType.Object)
                {
                    logger.LogWarning($"JSON Componnent \"{dict.Key}\" is a {dict.Value.Type} when it should be a Object");
                    return;
                }
                InitCustomComponent((JObject)dict.Value, dict.Key.Substring(1), obj);
            }
            else
            {
                if (dict.Value.Type != JTokenType.Object)
                {
                    logger.LogWarning($"JSON GameObject \"{dict.Key}\" is a {dict.Value.Type} when it should be a Object");
                    return;
                }
                bool matched = false;
                var jchildObj = (JObject)dict.Value;
                foreach (var childObj in FindGameObjectsInChildren(obj, dict.Key))
                {
                    matched = true;
                    InitCustomGameObject(jchildObj, childObj);
                }
                if (!matched)
                {
                    logger.LogWarning($"JSON GameObject \"{dict.Key}\" didn't correspond to any existent GameObjects");
                }
            }
        }
    }

    public void InitCustomComponent(JObject jcomponent, string name, GameObject obj)
    {
        switch (name)
        {
            case "Transform":
                InitCustomTransform(jcomponent, obj);
                break;
            case "SpriteRenderer":
                InitCustomSpriteRenderer(jcomponent, obj);
                break;
            case "ParallaxObjectScript":
                InitCustomParallaxObjectScript(jcomponent, obj);
                break;
            default:
                logger.LogWarning($"JSON Componnent \"{name}\" is an unknown/unsupported component");
                break;
        }
    }

    // UNITY COMPONENTS //
    public void InitCustomTransform(JObject jtransform, GameObject obj)
    {
        var transform = obj.transform;
        JObject jobj;
        if (TryGetJObject(jtransform, "LocalPosition", out jobj)) transform.localPosition = InitCustomVector3(jobj, transform.localPosition);
        if (TryGetJObject(jtransform, "LocalRotation", out jobj)) transform.localRotation = InitCustomQuaternion(jobj, transform.localRotation);
        if (TryGetJObject(jtransform, "LocalScale", out jobj)) transform.localScale = InitCustomVector3(jobj, transform.localScale);
    }
    public void InitCustomSpriteRenderer(JObject jspriteRenderer, GameObject obj)
    {
        var spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
        {
            logger.LogWarning($"GameObject \"{obj.name}\" does not have a spriteRenderer");
            return;
        }
        JObject jobj;
        if (TryGetJObject(jspriteRenderer, "Color", out jobj)) spriteRenderer.color = InitCustomColor(jobj, spriteRenderer.color);
        if (TryGetJObject(jspriteRenderer, "Size", out jobj)) spriteRenderer.size = InitCustomVector2(jobj, spriteRenderer.size);
        JValue jval;
        if (TryGetJValue(jspriteRenderer, "FlipX", JTokenType.Boolean, out jval)) spriteRenderer.flipX = (bool)jval;
        if (TryGetJValue(jspriteRenderer, "FlipY", JTokenType.Boolean, out jval)) spriteRenderer.flipY = (bool)jval;
        Material mat;
        if (TryGetJMaterial(jspriteRenderer, "Material", out mat) || 
            TryGetJShader(jspriteRenderer, "Shader", out mat)) 
            spriteRenderer.material = mat;
    }

    // BITS & BOPS SCRIPTS //
    public void InitCustomParallaxObjectScript(JObject jparallaxObjectScript, GameObject obj)
    {
        var parallaxObjectScript = obj.GetComponent<ParallaxObjectScript>();
        if (!parallaxObjectScript)
        {
            logger.LogWarning($"GameObject \"{obj.name}\" does not have a parallaxObjectScript");
            return;
        }
        JValue jval;
        if (TryGetJValue(jparallaxObjectScript, "ParallaxScale", JTokenType.Float, out jval)) parallaxObjectScript.parallaxScale = (float)jval;
        if (TryGetJValue(jparallaxObjectScript, "LoopDistance", JTokenType.Float, out jval)) parallaxObjectScript.loopDistance = (float)jval;
    }

    // STRUCTS //
    public Vector2 InitCustomVector2(JObject jvector2, Vector2 vector2)
    {
        JValue jval;
        if (TryGetJValue(jvector2, "x", JTokenType.Float, out jval)) vector2.x = (float)jval;
        if (TryGetJValue(jvector2, "y", JTokenType.Float, out jval)) vector2.y = (float)jval;
        return vector2;
    }
    public Vector3 InitCustomVector3(JObject jvector3, Vector3 vector3)
    {
        JValue jval;
        if (TryGetJValue(jvector3, "x", JTokenType.Float, out jval)) vector3.x = (float)jval;
        if (TryGetJValue(jvector3, "y", JTokenType.Float, out jval)) vector3.y = (float)jval;
        if (TryGetJValue(jvector3, "z", JTokenType.Float, out jval)) vector3.z = (float)jval;
        return vector3;
    }

    public Quaternion InitCustomQuaternion(JObject jquaternion, Quaternion quaternion)
    {
        JValue jval;
        if (TryGetJValue(jquaternion, "x", JTokenType.Float, out jval)) quaternion.x = (float)jval;
        if (TryGetJValue(jquaternion, "y", JTokenType.Float, out jval)) quaternion.y = (float)jval;
        if (TryGetJValue(jquaternion, "z", JTokenType.Float, out jval)) quaternion.z = (float)jval;
        if (TryGetJValue(jquaternion, "w", JTokenType.Float, out jval)) quaternion.w = (float)jval;
        return quaternion;
    }

    public Color InitCustomColor(JObject jcolor, Color color)
    {
        JValue jval;
        if (TryGetJValue(jcolor, "r", JTokenType.Float, out jval)) color.r = (float)jval;
        if (TryGetJValue(jcolor, "g", JTokenType.Float, out jval)) color.g = (float)jval;
        if (TryGetJValue(jcolor, "b", JTokenType.Float, out jval)) color.b = (float)jval;
        if (TryGetJValue(jcolor, "a", JTokenType.Float, out jval)) color.a = (float)jval;
        return color;
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
            Shader found = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == shaderName);
            if (!found)
            {
                found = Resources.Load<Shader>($"Shaders/{shaderName}");
            }
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


    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject obj, string path)
    {
        string[] names = Regex.Split(path, @"[\\/]");
        return FindGameObjectsInChildren(obj, names);
    }
    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject rootObj, string[] names, int i = 0)
    {
        for (var j = 0; j < rootObj.transform.childCount; j++)
        {
            var obj = rootObj.transform.GetChild(j).gameObject;
            if (Regex.IsMatch(obj.name, WildCardToRegex(names[i])))
            {
                if (i == names.Length - 1)
                {
                    yield return obj;
                }
                else
                {
                    foreach (var childObj in FindGameObjectsInChildren(obj, names, i + 1))
                    {
                        yield return childObj;
                    }
                }
            }
        }
    }


    private static string WildCardToRegex(string value)
    {
        return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }
}
