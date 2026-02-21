using UnityEngine;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod material definition.
/// </summary>
public class MMaterial: MObject<Material>
{
    public Shader shader;
    public Color? color;
    public Dictionary<string, float> floats = [];
    public Dictionary<string, int> integers = [];
    public List<string> disableKeywords = [];
    public List<string> enableKeywords = [];

    public override Material Apply(Material material)
    {
        material = new Material(material); // TODO: this certainly isn't performant but whatever
        if (shader != null) material.shader = shader;
        if (color != null) material.color = ApplyColor((Color)color, material.color);
        foreach (var pair in integers)
        {
            material.SetInteger(pair.Key, pair.Value);
        }
        foreach (var pair in floats)
        {
            material.SetFloat(pair.Key, pair.Value);
        }
        foreach (var keyword in enableKeywords)
        {
            Console.WriteLine(keyword);
            material.EnableKeyword(keyword);
        }
        foreach (var keyword in disableKeywords)
        {
            material.DisableKeyword(keyword);
        }
        return material;
    }
}
