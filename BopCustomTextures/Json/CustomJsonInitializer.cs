using BopCustomTextures.Customs;
using BopCustomTextures.SceneMods;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Json;

/// <summary>
/// Used to parse JSON-defined scene mods.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Shared with CustomTextureManager.</param>
public class CustomJsonInitializer(ILogger logger, CustomVariantNameManager variantManager) : BaseCustomManager(logger)
{
    private SceneKey lastScene = default;
    private readonly Dictionary<string, Material> Materials = [];
    private readonly Dictionary<string, Shader> Shaders = [];
    private readonly Dictionary<string, Material> ShaderMaterials = [];
    private readonly CustomVariantNameManager VariantManager = variantManager;

    private static readonly Regex TerminalComponentRegex = new Regex(@"^(.*)[\\/]!([^\\/]*)$", RegexOptions.Compiled);
    private static readonly Regex InfinityRegex = new Regex(@"^\s*(\+|-)?\s*inf(?:inity)?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MGameObject InitGameObject(JObject jobj, SceneKey scene, string name = "", bool isDeferred = false)
    {
        lastScene = scene;
        return InitGameObject(jobj, name, isDeferred);
    }

    public MGameObject InitGameObject(JObject jobj, string name = "", bool isDeferred = false)
    {
        /*
        // attempt to simplify name
        while (jobj.Count == 1)
        {
            var dict = jobj.Properties().First();
            logger.LogWarning($"{name} - {dict.Name}");
            if (dict.Value.Type != JTokenType.Object ||
                dict.Name.StartsWith("!") ||
                dict.Name.StartsWith("~"))
            {
                break;
            }
            if (name.EndsWith("/") || name.EndsWith("\\"))
            {
                name += dict.Name;
            } 
            else
            {
                name += "/" + dict.Name;
            }
            logger.LogError(name);
            jobj = (JObject)dict.Value;
        }
        */

        // check if is a single component on a gameobject
        var match = TerminalComponentRegex.Match(name);
        if (match.Success)
        {
            var mobj2 = new MGameObject(match.Groups[1].Value);
            mobj2.childObjs = [];
            mobj2.childObjsDeferred = [];

            if (MComponentParserRegistry.Instance.TryParse(this, match.Groups[2].Value, jobj, out var mcomponent))
            {
                mobj2.components = [mcomponent];
            }
            else
            {
                logger.LogWarning($"JSON Component \"{match.Groups[2].Value}\" in \"{name}\" failed to parse.");
                mobj2.components = [];
                // you could also just not add the mobj because it doesn't do anything?
                // TODO: don't add mobjs if they don't do anything
                // but also why would someone have empty mobjs? wouldn't parsing for that be kind of pointless?
            }
            return mobj2;
        }

        var mobj = new MGameObject(name);
        var components = new List<IMComponent>();
        var childObjs = new List<MGameObject>();
        var childObjsDeferred = new List<MGameObject>();

        foreach (KeyValuePair<string, JToken> dict in jobj)
        {
            if (dict.Key.StartsWith("!"))
            {
                string componentName = dict.Key.Substring(1);
                if (MComponentParserRegistry.Instance.TryParse(this, componentName, dict.Value, out var mcomponent))
                {
                    components.Add(mcomponent);
                }
                else
                {
                    logger.LogWarning($"JSON Component \"{componentName}\" in \"{name}\" failed to parse.");
                }
            }
            else
            {
                if (dict.Value.Type != JTokenType.Object)
                {
                    logger.LogWarning($"JSON GameObject \"{dict.Key}\" in \"{name}\" is a {dict.Value.Type} when it should be a Object");
                    continue;
                }

                string childName = dict.Key;
                bool isChildDeferred = isDeferred;
                if (childName.StartsWith("~"))
                {
                    isChildDeferred = true;
                    childName = childName.Substring(1);
                }

                var mchildObj = InitGameObject((JObject)dict.Value, childName, isChildDeferred);
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


    public bool TryGetVariant(JToken jtoken, out int variant)
    {
        switch (jtoken.Type)
        {
            case JTokenType.String:
                if (!VariantManager.TryGetVariant(lastScene, (string)jtoken, out variant))
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
                vector2 = InitJVector2(jobj2);
                return true;
            case JArray jarray2:
                vector2 = InitJVector2(jarray2);
                return true;
        }
        logger.LogWarning($"JSON vector2 \"{key}\" is a {jvector2.Type} when it should be an object or array");
        vector2 = default;
        return false;
    }
    public Vector2 InitJVector2(JObject jvector2)
    {
        float jfloat;
        return new Vector2(
            TryGetJFloat(jvector2, "x", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector2, "y", out jfloat) ? jfloat : float.NaN
        );
    }
    public Vector2 InitJVector2(JArray jvector2)
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
                vector3 = InitJVector3(jobj2);
                return true;
            case JArray jarray2:
                vector3 = InitJVector3(jarray2);
                return true;
        }
        logger.LogWarning($"JSON vector3 \"{key}\" is a {jvector3.Type} when it should be an object or array");
        vector3 = default;
        return false;
    }
    public Vector3 InitJVector3(JObject jvector3)
    {
        float jfloat;
        return new Vector3(
            TryGetJFloat(jvector3, "x", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector3, "y", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jvector3, "z", out jfloat) ? jfloat : float.NaN
        );
    }
    public Vector3 InitJVector3(JArray jvector3)
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
                eulerAngles = InitJVector3((JObject)jvector3);
                return true;
            case JTokenType.Array:
                eulerAngles = InitJVector3((JArray)jvector3);
                return true;
            case JTokenType.Float:
            case JTokenType.Integer:
                eulerAngles = new Vector3(float.NaN, float.NaN, (float)jvector3);
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
                quaternion = InitJQuaternion(jobj2);
                return true;
            case JArray jarray2:
                quaternion = InitJQuaternion(jarray2);
                return true;
        }
        logger.LogWarning($"JSON quaternion \"{key}\" is a {jquaternion.Type} when it should be an object or array");
        quaternion = default;
        return false;
    }
    public Quaternion InitJQuaternion(JObject jquaternion)
    {
        float jfloat;
        return new Quaternion(
            TryGetJFloat(jquaternion, "x", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, "y", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, "z", out jfloat) ? jfloat : float.NaN,
            TryGetJFloat(jquaternion, "w", out jfloat) ? jfloat : float.NaN
        );
    }
    public Quaternion InitJQuaternion(JArray jquaternion)
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
                color = InitJColor((JObject)jcolor);
                return true;
            case JTokenType.Array:
                color = InitJColor((JArray)jcolor);
                return true;
            case JTokenType.String:
                color = InitJColor((string)jcolor);
                return true;
        }
        logger.LogWarning($"JSON color \"{key}\" is a {jcolor.Type} when it should be an object, array, or string");
        color = default;
        return false;
    }
    public Color InitJColor(JObject jcolor)
    {
        float jfloat;
        return new Color(
            TryGetJColorChannel(jcolor, "r", out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, "g", out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, "b", out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, "a", out jfloat) ? jfloat : float.NaN
        );
    }
    public Color InitJColor(JArray jcolor)
    {
        float jfloat;
        return new Color(
            TryGetJColorChannel(jcolor, 0, out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, 1, out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, 2, out jfloat) ? jfloat : float.NaN,
            TryGetJColorChannel(jcolor, 3, out jfloat) ? jfloat : float.NaN
        );
    }
    public Color InitJColor(string str)
    {
        str = str.TrimStart('#');
        if (str.Length > 8)
        {
            str = str.Substring(0, 8);
        }
        Color jcolor = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
        if (!int.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            logger.LogWarning($"JSON color string \"{str}\" couldn't be parsed as as color");
        }
        else
        {
            for (int i = str.Length / 2 - 1; i >= 0; i--)
            {
                jcolor[i] = (rgb & 0xFF) / 255.0f;
                rgb >>= 8;
            }
        }
        return jcolor;
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
        if (TryGetJFloat(jtoken2, out jfloat))
        {
            return true;
        }
        logger.LogWarning($"JSON key \"{key}\" is a {jtoken2.Type} when it should be a float, integer, \"Infinity\", or \"-Infinity\"");
        return false;
    }
    public bool TryGetJFloat(JArray jarray, int index, out float jfloat)
    {
        if (jarray.Count <= index)
        {
            jfloat = default;
            return false;
        }
        var jtoken2 = jarray[index];
        if (TryGetJFloat(jtoken2, out jfloat))
        {
            return true;
        }
        logger.LogWarning($"JSON index \"{index}\" is a {jtoken2.Type} when it should be a float, integer, \"Infinity\", or \"-Infinity\"");
        return false;
    }
    public bool TryGetJFloat(JToken jtoken, out float jfloat)
    {
        if (jtoken.Type == JTokenType.String)
        {
            Match match = InfinityRegex.Match((string)jtoken);
            if (match.Success)
            {
                if (match.Groups[1].Length > 0 && match.Groups[1].Value[0] == '-')
                {
                    jfloat = float.NegativeInfinity;
                }
                else
                {
                    jfloat = float.PositiveInfinity;
                }
                return true;
            }
        }
        else if (jtoken.Type == JTokenType.Float || jtoken.Type == JTokenType.Integer)
        {
            jfloat = (float)jtoken;
            return true;
        }
        jfloat = default;
        return false;
    }

    public bool TryGetJColorChannel(JObject jobj, string key, out float jfloat)
    {
        if (!jobj.TryGetValue(key, out var jtoken2))
        {
            jfloat = default;
            return false;
        }
        if (TryGetJColorChannel(jtoken2, out jfloat))
        {
            return true;
        }
        logger.LogWarning($"JSON key \"{key}\" is a {jtoken2.Type} when it should be a float or integer");
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
        if (TryGetJColorChannel(jtoken2, out jfloat))
        {
            return true;
        }
        logger.LogWarning($"JSON index \"{index}\" is a {jtoken2.Type} when it should be a float or integer");
        return true;
    }
    public bool TryGetJColorChannel(JToken jtoken, out float jfloat)
    {
        switch (jtoken.Type)
        {
            case JTokenType.Float:
                jfloat = (float)jtoken;
                return true;
            case JTokenType.Integer:
                jfloat = (float)jtoken / 255;
                return true;
        }
        jfloat = default;
        return false;
    }


}
