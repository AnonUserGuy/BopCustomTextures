using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Base;
using BopCustomTextures.SceneMods.Unity;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Scripts;

/// <summary>
/// Scene mod <see cref="ParallaxObjectScript"/> definition
/// </summary>
[MComponent("ParallaxObjectScript")]
public class MParallaxObjectScript : MBehaviour<ParallaxObjectScript>
{
    public float? parallaxScale;
    public float? loopDistance;

    public override void JsonParsePair(string key, JToken val)
    {
        float jfloat;
        if (KeyMatch(key, "ParallaxScale") && MFloat.TryJsonParse(val, out jfloat)) parallaxScale = jfloat;
        if (KeyMatch(key, "LoopDistance") && MFloat.TryJsonParse(val, out jfloat)) loopDistance = jfloat;
        else base.JsonParsePair(key, val);
    }

    public override ParallaxObjectScript Apply(ParallaxObjectScript component)
    {
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
        base.Apply(component);
        return component;
    }
}
