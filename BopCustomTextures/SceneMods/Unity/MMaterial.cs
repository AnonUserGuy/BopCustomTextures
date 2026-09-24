using UnityEngine;
using System.Collections.Generic;
using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;
using BopCustomTextures.SceneMods.Unity.Structs;

namespace BopCustomTextures.SceneMods.Unity;

/// <summary>
/// Scene mod <see cref="Material"/> definition.
/// </summary>
public class MMaterial : MUnityObject<Material>
{
    public bool needsNew;
    public MShader shader;
    public Color? color;

    public Dictionary<string, float> floats = [];
    public Dictionary<string, int> integers = [];
    public Dictionary<string, bool> enable = [];

    public override bool JsonParse(CustomJsonInitializer ctx, JToken val)
    {
        if (val.Type == JTokenType.String)
        {
            needsNew = false;
            if (ctx.TryGetMaterial((string)val, out var mmaterial))
            {
                Ref = mmaterial;
                HasRef = true;
                return true;
            }
            return false;
        }
        else
        {
            needsNew = true;
            return base.JsonParse(ctx, val);
        }
    }

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        if (val.Type == JTokenType.String 
            && (KeyMatch(key, "Name") || KeyMatch(key, "Material")) 
            && ctx.TryGetMaterial((string)val, out Material mat))
        {
            Ref = mat;
            HasRef = true;
        }
        else if (KeyMatch(key, "Color") && MColor.TryJsonParse(ctx, val, out var mcolor)) color = mcolor;
        else if (KeyMatch(key, "Shader") && TryJsonParse<MShader, Shader>(ctx, val, out var mshader)) shader = mshader;
        else if (key.StartsWith("m_"))
        {
            switch (val.Type)
            {
                case JTokenType.Integer:
                    integers.Add(key, (int)val);
                    break;
                case JTokenType.Float:
                    floats.Add(key, (float)val);
                    break;
                case JTokenType.Boolean:
                    enable.Add(key, (bool)val);
                    break;
            }
        } 
        else base.JsonParsePair(ctx, key, val);
    }

    public override Material ApplyInternal(Material mat)
    {
        if (needsNew)
        {
            mat = new Material(mat); // TODO: this certainly isn't performant but whatever
            if (shader != null) mat.shader = shader.Apply(mat.shader);
            if (color != null) mat.color = MColor.Apply(color, mat.color);

            foreach (var pair in integers)
            {
                mat.SetInteger(pair.Key, pair.Value);
            }
            foreach (var pair in floats)
            {
                mat.SetFloat(pair.Key, pair.Value);
            }
            foreach (var pair in enable)
            {
                if (pair.Value)
                {
                    mat.EnableKeyword(pair.Key);
                }
                else
                {
                    mat.DisableKeyword(pair.Key);
                }
            }
        }
        return mat;
    }
}
