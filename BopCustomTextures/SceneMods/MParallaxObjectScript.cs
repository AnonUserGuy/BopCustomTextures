using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="ParallaxObjectScript"/> definition
/// </summary>
[MComponent("ParallaxObjectScript")]
public class MParallaxObjectScript : MBehaviour<ParallaxObjectScript>
{
    public float? parallaxScale;
    public float? loopDistance;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        float jfloat;
        if (ctx.TryGetJFloat(key, val, "ParallaxScale", out jfloat)) parallaxScale = jfloat;
        if (ctx.TryGetJFloat(key, val, "LoopDistance", out jfloat)) loopDistance = jfloat;
        else base.JsonParsePair(ctx, key, val);
    }

    public override ParallaxObjectScript Apply(ParallaxObjectScript component)
    {
        base.Apply(component);
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
        return component;
    }
}
