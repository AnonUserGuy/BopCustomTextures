using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.System;
using BopCustomTextures.SceneMods.Unity.Components;
using Newtonsoft.Json.Linq;

namespace BopCustomTextures.SceneMods.Scripts;

/// <summary>
/// Scene mod <see cref="ParallaxObjectScript"/> definition.
/// </summary>
[MComponent("ParallaxObjectScript")]
public class MParallaxObjectScript : MBehaviour<ParallaxObjectScript>
{
    public float? parallaxScale;
    public float? loopDistance;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken val)
    {
        float jfloat;
        if      (KeyMatch(key, "ParallaxScale") && MFloat.TryJsonParse(ctx, val, out jfloat)) parallaxScale = jfloat;
        else if (KeyMatch(key, "LoopDistance") && MFloat.TryJsonParse(ctx, val, out jfloat)) loopDistance = jfloat;
        else base.JsonParsePair(ctx, key, val);
    }

    public override ParallaxObjectScript ApplyInternal(ParallaxObjectScript component)
    {
        if (parallaxScale != null) component.parallaxScale = (float)parallaxScale;
        if (loopDistance != null) component.loopDistance = (float)loopDistance;
        return base.ApplyInternal(component);
    }
}
