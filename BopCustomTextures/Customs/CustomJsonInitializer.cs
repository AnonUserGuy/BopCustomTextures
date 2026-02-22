using BopCustomTextures.SceneMods;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using ILogger = BopCustomTextures.Logging.ILogger;
using System.Globalization;

namespace BopCustomTextures.Customs;

/// <summary>
/// Used to parse JSON-defined scene mods.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Shared with CustomTextureManager.</param>
public class CustomJsonInitializer(ILogger logger, CustomVariantNameManager variantManager) : BaseCustomManager(logger)
{
    private readonly Dictionary<string, Material> Materials = [];
    private readonly Dictionary<string, Shader> Shaders = [];
    private readonly Dictionary<string, Material> ShaderMaterials = [];
    private readonly CustomVariantNameManager VariantManager = variantManager;

    public MGameObject InitGameObject(JObject jobj, SceneKey scene, string name = "", bool isDeferred = false)
    {
        var mobj = new MGameObject(name);
        var components = new List<MComponent>();
        var childObjs = new List<MGameObject>();
        var childObjsDeferred = new List<MGameObject>();
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
                bool isChildDeferred = isDeferred;
                if (childName.StartsWith("~")) {
                    isChildDeferred = true;
                    childName = childName.Substring(1);
                }

                var mchildObj = InitGameObject((JObject)dict.Value, scene, childName, isChildDeferred);
                if (isChildDeferred)
                {
                    childObjsDeferred.Add(mchildObj);
                } 
                else
                {
                    childObjs.Add(mchildObj);
                }
            }
        }
        mobj.components = components.ToArray();
        mobj.childObjs = childObjs.ToArray();
        mobj.childObjsDeferred = childObjsDeferred.ToArray();
        return mobj;
    }

    public MComponent InitComponent(JObject jcomponent, SceneKey scene, string name)
    {
        switch (name)
        {
            case "Transform":
                return InitTransform(jcomponent);
            case "Camera":
                return InitCamera(jcomponent);
            case "SpriteRenderer":
                return InitSpriteRenderer(jcomponent);
            case "Image":
                return InitImage(jcomponent);
            case "ParallaxObjectScript":
                return InitParallaxObjectScript(jcomponent);
            case "CustomSpriteSwapper":
                return InitCustomSpriteSwapper(jcomponent, scene);
            default:
                logger.LogWarning($"JSON Componnent \"{name}\" is an unknown/unsupported component");
                return null;
        }
    }

    public MMaterial InitMaterial(JObject jmaterial)
    {
        var mmaterial = new MMaterial();
        jmaterial.Remove("Name");
        jmaterial.Remove("Material");
        if (TryGetJShader(jmaterial, "Shader", out var shader)) 
        {
            mmaterial.shader = shader;
            jmaterial.Remove("Shader");
        };
        if (TryGetJColor(jmaterial, "Color", out var color))
        {
            mmaterial.color = color;
            jmaterial.Remove("Color");
        }
        foreach (var pair in jmaterial)
        {
            switch (pair.Value.Type)
            {
                case JTokenType.Integer:
                    mmaterial.integers.Add(pair.Key, (int)pair.Value);
                    break;
                case JTokenType.Float:
                    mmaterial.floats.Add(pair.Key, (float)pair.Value);
                    break;
                case JTokenType.Boolean:
                    if ((bool)pair.Value)
                    {
                        mmaterial.enableKeywords.Add(pair.Key);
                    }
                    else
                    {
                        mmaterial.disableKeywords.Add(pair.Key);
                    }
                    break;
            }
        }
        return mmaterial;
    }

    // UNITY COMPONENTS //
    public MTransform InitTransform(JObject jcomponent)
    {
        var mcomponent = new MTransform();
        if (TryGetJVector3(jcomponent, "LocalPosition", out var vector3)) mcomponent.localPosition = vector3;
        if (TryGetJQuaternion(jcomponent, "LocalRotation", out var quaternion)) mcomponent.localRotation = quaternion;
        if (TryGetJEulerAngles(jcomponent, "LocalEulerAngles", out vector3)) mcomponent.localEulerAngles = vector3;
        if (TryGetJVector3(jcomponent, "LocalEulerAngles", out vector3)) mcomponent.localEulerAngles = vector3;
        if (TryGetJVector3(jcomponent, "LocalScale", out vector3)) mcomponent.localScale = vector3;
        return mcomponent;
    }
    public MCamera InitCamera(JObject jcomponent)
    {
        var mcomponent = new MCamera();
        if (TryGetJValue(jcomponent, "Orthographic", JTokenType.Boolean, out var jval)) mcomponent.orthographic = (bool)jval;
        if (TryGetJFloat(jcomponent, "OrthographicSize", out var jfloat)) mcomponent.orthographicSize = jfloat;
        if (TryGetJFloat(jcomponent, "Aspect", out jfloat)) mcomponent.aspect = jfloat;
        if (TryGetJColor(jcomponent, "BackgroundColor", out var color)) mcomponent.backgroundColor = color;
        return mcomponent;
    }

    public IMRenderable InitRenderable(JObject jcomponent, IMRenderable mcomponent)
    {
        if (jcomponent.TryGetValue("Material", out var jmat))
        {
            switch (jmat.Type)
            {
                case JTokenType.String:
                    if (TryGetMaterial((string)jmat, out var mat))
                    {
                        mcomponent.Material = mat;
                    }
                    break;
                case JTokenType.Object:
                    var jmaterial = (JObject)jmat;
                    if (TryGetJMaterial(jmaterial, "Name", out mat) || 
                        TryGetJMaterial(jmaterial, "Material", out mat))
                    {
                        mcomponent.Material = mat;
                    }
                    mcomponent.MMaterial = InitMaterial(jmaterial);
                    break;
            }
        }
        else if (TryGetJShaderMaterial(jcomponent, "Shader", out var mat))
        {
            mcomponent.Material = mat;
        }
        return mcomponent;
    }

    public MSpriteRenderer InitSpriteRenderer(JObject jcomponent)
    {
        var mcomponent = new MSpriteRenderer();
        if (TryGetJColor(jcomponent, "Color", out var color)) mcomponent.color = color;
        if (TryGetJVector2(jcomponent, "Size", out var vector2)) mcomponent.size = vector2;
        JValue jval;
        if (TryGetJValue(jcomponent, "FlipX", JTokenType.Boolean, out jval)) mcomponent.flipX = (bool)jval;
        if (TryGetJValue(jcomponent, "FlipY", JTokenType.Boolean, out jval)) mcomponent.flipY = (bool)jval;
        InitRenderable(jcomponent, mcomponent);
        return mcomponent;
    }

    public MImage InitImage(JObject jcomponent)
    {
        var mcomponent = new MImage();
        InitRenderable(jcomponent, mcomponent);
        return mcomponent;
    }

    // MONOBEHAVIOURS //
    public MBehaviour<T> InitBehaviour<T>(JObject jcomponent, MBehaviour<T> mcomponent) where T: Behaviour
    {
        if (TryGetJValue(jcomponent, "Enabled", JTokenType.Boolean, out var jbool)) mcomponent.enabled = (bool)jbool;
        return mcomponent;
    }

    public MParallaxObjectScript InitParallaxObjectScript(JObject jcomponent)
    {
        var mcomponent = new MParallaxObjectScript();
        InitBehaviour(jcomponent, mcomponent);
        float jfloat;
        if (TryGetJFloat(jcomponent, "ParallaxScale", out jfloat)) mcomponent.parallaxScale = jfloat;
        if (TryGetJFloat(jcomponent, "LoopDistance", out jfloat)) mcomponent.loopDistance = jfloat;
        return mcomponent;
    }

    public MCustomSpriteSwapper InitCustomSpriteSwapper(JObject jcomponent, SceneKey scene)
    {
        var mcomponent = new MCustomSpriteSwapper();
        InitBehaviour(jcomponent, mcomponent);
        if (jcomponent.TryGetValue("Variants", out var jvariants))
        {
            switch (jvariants.Type)
            {
                case JTokenType.Array:
                    mcomponent.variants = [];
                    foreach (var jel in (JArray)jvariants)
                    {
                        if (TryGetVariant(jel, scene, out var variant))
                        {
                            mcomponent.variants.Add(variant);
                        }
                    }
                    break;
                case JTokenType.Object:
                    mcomponent.variantsIndexed = [];
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
                            mcomponent.variantsIndexed[index] = variant;
                        }
                    }
                    break;
                case JTokenType.String:
                case JTokenType.Integer:
                    if (TryGetVariant(jvariants, scene, out var variant2))
                    {
                        mcomponent.variants = [variant2];
                    }
                    break;
                default:
                    logger.LogWarning($"JSON variants is a {jvariants.Type} when it should be an array, object, string, or integer");
                    break;
            }
        }
        return mcomponent;
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
        float jfloat;
        return new Vector2(
            TryGetJFloat(jvector2, "x", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector2, "y", out jfloat) ? jfloat : float.NaN
        );
    }
    public Vector2 InitCustomVector2(JArray jvector2)
    {
        float jfloat;
        return new Vector2(
            TryGetJFloat(jvector2, 0, out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector2, 1, out jfloat) ? jfloat : float.NaN
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
        float jfloat;
        return new Vector3(
            TryGetJFloat(jvector3, "x", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector3, "y", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector3, "z", out jfloat) ? jfloat : float.NaN
        );
    }
    public Vector3 InitCustomVector3(JArray jvector3)
    {
        float jfloat;
        return new Vector3(
            TryGetJFloat(jvector3, 0, out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector3, 1, out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector3, 2, out jfloat) ? jfloat : float.NaN
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
            case JTokenType.Integer:
                eulerAngles = new Vector3(float.NaN, (float)jvector3, float.NaN);
                return true;
        }
        logger.LogWarning($"JSON eulerAngles \"{key}\" is a {jvector3.Type} when it should be an object, array, float, or integer");
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
        float jfloat;
        return new Quaternion(
            TryGetJFloat(jquaternion, "x", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, "y", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, "z", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, "w", out jfloat) ? jfloat : float.NaN
        );
    }
    public Quaternion InitCustomQuaternion(JArray jquaternion)
    {
        float jfloat;
        return new Quaternion(
            TryGetJFloat(jquaternion, 0, out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, 1, out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, 2, out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, 3, out jfloat) ? jfloat : float.NaN
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
        float jfloat;
        return new Color(
            TryGetJColorChannel(jcolor, "r", out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, "g", out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, "b", out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, "a", out jfloat) ? jfloat : float.NaN
        );
    }
    public Color InitCustomColor(JArray jcolor)
    {
        float jfloat;
        return new Color(
            TryGetJColorChannel(jcolor, 0, out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, 1, out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, 2, out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, 3, out jfloat) ? jfloat : float.NaN
        );
    }
    public Color InitCustomColor(string str)
    {
        str = str.TrimStart('#');
        Color jcolor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
        if (!int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            logger.LogWarning($"JSON color string \"{str}\" couldn't be parsed as as color");
        }
        else
        {
            for (int i = 0; i < str.Length / 2 && i < 4; i++)
            {
                jcolor[i] = (rgb & 0xFF) / 255.0f;
                rgb >>= 8;
            }
        }
        return jcolor;
    }

    public bool TryGetJMaterial(JObject jobj, string key, out Material material)
    {
        if (!TryGetJValue(jobj, key, JTokenType.String, out var jmatName))
        {
            material = null;
            return false;
        }
        string matName = (string)jmatName;
        return TryGetMaterial(matName, out material);
    }

    public bool TryGetMaterial(string name, out Material material)
    {
        if (!Materials.ContainsKey(name))
        {
            Material found = Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(s => s.name == name) ??
                Resources.Load<Material>($"Materials/{name}");
            if (!found)
            {
                logger.LogWarning($"JSON material \"{name}\" could not be found");
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

    public bool TryGetJShaderMaterial(JObject jobj, string key, out Material mat)
    {
        if (!TryGetJValue(jobj, key, JTokenType.String, out var jshaderName))
        {
            mat = null;
            return false;
        }
        string shaderName = (string)jshaderName;
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

    public bool TryGetJShader(JObject jobj, string key, out Shader shader)
    {
        if (!TryGetJValue(jobj, key, JTokenType.String, out var jshaderName))
        {
            shader = null;
            return false;
        }
        string shaderName = (string)jshaderName;
        return TryGetShader(shaderName, out shader);
    }

    public bool TryGetShader(string name, out Shader shader)
    {
        if (!Shaders.ContainsKey(name))
        {
            Shader found = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s.name == name) ??
                   Shader.Find(name);
            if (!found)
            {
                logger.LogWarning($"JSON shader \"{name}\" could not be found");
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

    public bool TryGetJFloat(JObject jobj, string key, out float jfloat)
    {
        if (!jobj.TryGetValue(key, out var jtoken2))
        {
            jfloat = default;
            return false;
        }
        if (jtoken2.Type != JTokenType.Float && jtoken2.Type != JTokenType.Integer)
        {
            logger.LogWarning($"JSON key \"{key}\" is a {jtoken2.Type} when it should be a float or integer");
            jfloat = default;
            return false;
        }
        jfloat = (float)jtoken2;
        return true;
    }
    public bool TryGetJFloat(JArray jarray, int index, out float jfloat)
    {
        if (jarray.Count <= index)
        {
            jfloat = default;
            return false;
        }
        var jtoken2 = jarray[index];
        if (jtoken2.Type != JTokenType.Float && jtoken2.Type != JTokenType.Integer)
        {
            logger.LogWarning($"JSON index \"{index}\" is a {jtoken2.Type} when it should be a float or integer");
            jfloat = default;
            return false;
        }
        jfloat = (float)jtoken2;
        return true;
    }

    public bool TryGetJColorChannel(JObject jobj, string key, out float jfloat)
    {
        if (!jobj.TryGetValue(key, out var jtoken2))
        {
            jfloat = default;
            return false;
        }
        switch (jtoken2.Type)
        {
            case JTokenType.Float:
                jfloat = (float)jtoken2;
                return true;
            case JTokenType.Integer:
                jfloat = (float)jtoken2 / 255;
                return true;
        }
        logger.LogWarning($"JSON key \"{key}\" is a {jtoken2.Type} when it should be a float or integer");
        jfloat = default;
        return true;
    }
    public bool TryGetJColorChannel(JArray jarray, int index, out float jfloat)
    {
        if (jarray.Count <= index)
        {
            jfloat = default;
            return false;
        }
        var jtoken2 = jarray[index];
        switch (jtoken2.Type)
        {
            case JTokenType.Float:
                jfloat = (float)jtoken2;
                return true;
            case JTokenType.Integer:
                jfloat = (float)jtoken2 / 255;
                return true;
        }
        logger.LogWarning($"JSON index \"{index}\" is a {jtoken2.Type} when it should be a float or integer");
        jfloat = default;
        return true;
    }


}
