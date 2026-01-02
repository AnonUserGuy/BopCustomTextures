using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Color = UnityEngine.Color;

namespace BopCustomTextures;
public class CustomInitializer
{
    public static void InitCustomGameObject(JToken jobj, string path, GameObject rootObj)
    {
        if (jobj.GetType() != typeof(JObject))
        {
            Plugin.Logger.LogWarning($"JSON GameObject\"{path}\" is a {jobj.GetType()} when it should be a JObject");
            return;
        }
        var jgameObj = (JObject)jobj;
        GameObject obj = FindGameObjectInChildren(rootObj, path);
        if (obj == null)
        {
            Plugin.Logger.LogWarning($"JSON GameObject\"{path}\" does not correspond to a gameObject in the scene");
            return;
        }
        foreach (KeyValuePair<string, JToken> dict in jgameObj)
        {
            if (dict.Key.StartsWith("!"))
            {
                InitCustomComponent(dict.Value, dict.Key.Substring(1), obj);
            }
            else
            {
                InitCustomGameObject(dict.Value, dict.Key, obj);
            }
        }
    }

    public static void InitCustomComponent(JToken jobj, string name, GameObject obj)
    {
        if (jobj.GetType() != typeof(JObject))
        {
            Plugin.Logger.LogWarning($"JSON Componnent\"{name}\" is a {jobj.GetType()} when it should be a JObject");
            return;
        }
        JObject jcomponent = (JObject)jobj;

        switch (name)
        {
            case "Transform":
                InitCustomTransform(jcomponent, obj.transform);
                break;
            case "SpriteRenderer":
                InitCustomSpriteRenderer(jcomponent, obj);
                break;
            default:
                Plugin.Logger.LogWarning($"JSON Componnent \"{name}\" is an unknown/unsupported component");
                break;
        }
    }

    // COMPONENTS //
    public static void InitCustomTransform(JObject jtransform, Transform transform)
    {
        transform.localPosition = InitCustomVector3(jtransform, "LocalPosition", transform.localPosition);
        transform.localRotation = InitCustomQuaternion(jtransform, "LocalRotation", transform.localRotation);
        transform.localScale = InitCustomVector3(jtransform, "LocalScale", transform.localScale);
    }
    public static void InitCustomSpriteRenderer(JObject jspriteRenderer, GameObject obj)
    {
        var spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Plugin.Logger.LogWarning($"GameObject \"{obj.name}\" does not have a spriteRenderer");
            return;
        }
        spriteRenderer.color = InitCustomColor(jspriteRenderer, "Color", spriteRenderer.color);
        spriteRenderer.size = InitCustomVector2(jspriteRenderer, "Size", spriteRenderer.size);
        spriteRenderer.flipX = InitCustomBool(jspriteRenderer, "FlipX", spriteRenderer.flipX);
        spriteRenderer.flipY = InitCustomBool(jspriteRenderer, "FlipY", spriteRenderer.flipY);
    }

    // STRUCTS //
    public static Vector3 InitCustomVector2(JObject jobj, string key, Vector2 vector2)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JObject))
            {
                JObject jvector2 = (JObject)jobj[key];
                vector2.x = InitCustomFloat(jvector2, "x", vector2.x);
                vector2.y = InitCustomFloat(jvector2, "y", vector2.y);
            }
            else
            {
                Plugin.Logger.LogWarning($"JSON Vector2 \"{key}\" is a {jobj[key].GetType()} when it should be a JObject");
            }
        }
        return vector2;
    }
    public static Vector3 InitCustomVector3(JObject jobj, string key, Vector3 vector3)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JObject))
            {
                JObject jvector3 = (JObject)jobj[key];
                vector3.x = InitCustomFloat(jvector3, "x", vector3.x);
                vector3.y = InitCustomFloat(jvector3, "y", vector3.y);
                vector3.z = InitCustomFloat(jvector3, "z", vector3.z);
            }
            else 
            {
                Plugin.Logger.LogWarning($"JSON Vector3 \"{key}\" is a {jobj[key].GetType()} when it should be a JObject");
            }
        } 
        return vector3;
    }

    public static Quaternion InitCustomQuaternion(JObject jobj, string key, Quaternion quaternion)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JObject))
            {
                JObject jvector3 = (JObject)jobj[key];
                quaternion.x = InitCustomFloat(jvector3, "x", quaternion.x);
                quaternion.y = InitCustomFloat(jvector3, "y", quaternion.y);
                quaternion.z = InitCustomFloat(jvector3, "z", quaternion.z);
                quaternion.w = InitCustomFloat(jvector3, "w", quaternion.w);
            }
            else
            {
                Plugin.Logger.LogWarning($"JSON Quaternion \"{key}\" is a {jobj[key].GetType()} when it should be a JObject");
            }
        }
        return quaternion;
    }

    public static Color InitCustomColor(JObject jobj, string key, Color color)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JObject))
            {
                JObject jcolor = (JObject)jobj[key];
                color.r = InitCustomFloat(jcolor, "r", color.r);
                color.g = InitCustomFloat(jcolor, "g", color.g);
                color.b = InitCustomFloat(jcolor, "b", color.b);
                color.a = InitCustomFloat(jcolor, "a", color.a);
            }
            else
            {
                Plugin.Logger.LogWarning($"JSON Color \"{key}\" is a {jobj[key].GetType()} when it should be a JObject");
            }
        }
        return color;
    }

    // PRIMITIVES // 
    public static float InitCustomFloat(JObject jobj, string key, float num)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JValue))
            {
                JValue jval = (JValue)jobj[key];
                if (jval.Type == JTokenType.Float)
                {
                    return (float)jval;
                }
                else
                {
                    Plugin.Logger.LogWarning($"JSON float \"{key}\" is a {jval.Type} when it should be a float");
                }
            }
            else
            {
                Plugin.Logger.LogWarning($"JSON float \"{key}\" is a {jobj[key].GetType()} when it should be a float");
            }
        }
        return num;
    }

    public static bool InitCustomBool(JObject jobj, string key, bool val)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JValue))
            {
                JValue jval = (JValue)jobj[key];
                if (jval.Type == JTokenType.Boolean)
                {
                    return (bool)jval;
                }
                else
                {
                    Plugin.Logger.LogWarning($"JSON float \"{key}\" is a {jval.Type} when it should be a boolean");
                }
            }
            else
            {
                Plugin.Logger.LogWarning($"JSON float \"{key}\" is a {jobj[key].GetType()} when it should be a boolean");
            }
        }
        return val;
    }


    // UTILITY // 
    public static GameObject FindGameObjectInChildren(GameObject obj, string path)
    {
        string[] names = path.Split('/');
        for (var i = 0; i < names.Length; i++)
        {
            bool success = false;
            for (var j = 0; j < obj.transform.childCount; j++)
            {
                var newObj = obj.transform.GetChild(j).gameObject;
                if (newObj.name == names[i])
                {
                    obj = newObj;
                    success = true;
                    break;
                }
            }
            if (!success)
            {
                return null;
            }
        }
        return obj;
    }
}
