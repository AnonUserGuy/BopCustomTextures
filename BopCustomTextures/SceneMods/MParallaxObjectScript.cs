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

    public override bool JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        base.JsonParse(ctx, jcomponent);
        float jfloat;
        if (ctx.TryGetJFloat(jcomponent, "ParallaxScale", out jfloat)) parallaxScale = jfloat;
        if (ctx.TryGetJFloat(jcomponent, "LoopDistance", out jfloat)) loopDistance = jfloat;
        return true;
    }

    public override ParallaxObjectScript Apply(ParallaxObjectScript component)
    {
        base.Apply(component);
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
        return component;
    }
}
