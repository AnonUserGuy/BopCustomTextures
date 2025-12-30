using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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
    }

    // STRUCTS //
    public static Vector3 InitCustomVector3(JObject jobj, string key, Vector3 vector3)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JObject))
            {
                JObject jvector3 = (JObject)jobj[key];
                InitCustomFloat(jvector3, "x", ref vector3.x);
                InitCustomFloat(jvector3, "y", ref vector3.y);
                InitCustomFloat(jvector3, "z", ref vector3.z);
            }
            else 
            {
                Plugin.Logger.LogWarning($"JSON Vector \"{key}\" is a {jobj[key].GetType()} when it should be a JObject");
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
                InitCustomFloat(jvector3, "x", ref quaternion.x);
                InitCustomFloat(jvector3, "y", ref quaternion.y);
                InitCustomFloat(jvector3, "z", ref quaternion.z);
                InitCustomFloat(jvector3, "w", ref quaternion.w);
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
                InitCustomFloat(jcolor, "r", ref color.r);
                InitCustomFloat(jcolor, "g", ref color.g);
                InitCustomFloat(jcolor, "b", ref color.b);
                InitCustomFloat(jcolor, "a", ref color.a);
            }
            else
            {
                Plugin.Logger.LogWarning($"JSON Color \"{key}\" is a {jobj[key].GetType()} when it should be a JObject");
            }
        }
        return color;
    }

    // PRIMITIVES // 
    public static void InitCustomFloat(JObject jobj, string key, ref float num)
    {
        if (jobj.ContainsKey(key))
        {
            if (jobj[key].GetType() == typeof(JValue))
            {
                JValue jval = (JValue)jobj[key];
                if (jval.Type == JTokenType.Float)
                {
                    num = (float)jval;
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
