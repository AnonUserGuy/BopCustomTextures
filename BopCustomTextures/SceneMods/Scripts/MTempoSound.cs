using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Unity.Components;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace BopCustomTextures.SceneMods.Scripts;

[MComponent("TempoSound")]
public class MTempoSound : MBehaviour<TempoSound>, IMLoaderComponentFinal
{
    public bool Play;
    public bool Stop;

    public override void JsonParsePair(CustomJsonInitializer ctx, string key, JToken jtoken)
    {
        if (KeyMatch(key, "Play")) Play = true;
        else if (KeyMatch(key, "Stop")) Stop = true;
        else base.JsonParsePair(ctx, key, jtoken);
    }

    public object ApplyLoader(object ctx, MixtapeLoaderCustom loader, Entity entity, float beat, GameObject gameObj)
    {
        if (!gameObj.TryGetComponent<TempoSound>(out var sound))
        {
            return ctx;
        }
        return ApplyLoader((Dictionary<TempoSound, double>)ctx, loader, beat, sound);
    }

    public Dictionary<TempoSound, double> ApplyLoader(Dictionary<TempoSound, double> ctx, MixtapeLoaderCustom loader, float beat, TempoSound sound)
    {
        ctx ??= [];
        double seconds = loader.jukebox.BeatsToSecondsDouble(beat);

        if (Stop)
        {
            if (ctx.TryGetValue(sound, out var lastSeconds))
            {
                sound.Schedule(lastSeconds, seconds);
                ctx.Remove(sound);
            }
        }
        if (Play)
        {
            if (ctx.TryGetValue(sound, out var lastSeconds))
            {
                sound.Schedule(lastSeconds);
            }
            ctx[sound] = seconds;
        }
        return ctx;
    }

    public void ApplyLoaderFinalize(object ctx)
    {
        ApplyLoaderFinalize((Dictionary<TempoSound, double>)ctx);
    }

    public void ApplyLoaderFinalize(Dictionary<TempoSound, double> ctx)
    {
        foreach (var pair in ctx)
        {
            pair.Key.Schedule(pair.Value);
        }
    }
}