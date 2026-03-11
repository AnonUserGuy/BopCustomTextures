using BopCustomTextures.Json;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods;

/// <summary>
/// Scene mod <see cref="ParallaxObjectScript"/> definition
/// </summary>
public class MParallaxObjectScript : MBehaviour<ParallaxObjectScript>
{
    public float? parallaxScale;
    public float? loopDistance;

    public static void Register()
    {
        MComponentParserRegistry.Register("ParallaxObjectScript", JsonParse);
    }

    public static MParallaxObjectScript JsonParse(CustomJsonInitializer ctx, JObject jcomponent)
    {
        var mcomponent = new MParallaxObjectScript();
        JsonParse(ctx, jcomponent, mcomponent);
        float jfloat;
        if (ctx.TryGetJFloat(jcomponent, "ParallaxScale", out jfloat)) mcomponent.parallaxScale = jfloat;
        if (ctx.TryGetJFloat(jcomponent, "LoopDistance", out jfloat)) mcomponent.loopDistance = jfloat;
        return mcomponent;
    }

    public override ParallaxObjectScript Apply(ParallaxObjectScript component)
    {
        base.Apply(component);
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
        return component;
    }
}
