using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Unity.Components;
using UnityEngine;
using Newtonsoft.Json.Linq;
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

    public object ApplyLoader(MixtapeLoaderCustom loader, object ctx, Entity entity, float beat, GameObject gameObj)
    {
        if (!gameObj.TryGetComponent<TempoSound>(out var sound))
        {
            return ctx;
        }
        return ApplyLoader(loader, (Dictionary<TempoSound, float>)ctx, beat, sound);
    }

    public Dictionary<TempoSound, float> ApplyLoader(MixtapeLoaderCustom loader, Dictionary<TempoSound, float> ctx, float beat, TempoSound sound)
    {
        ctx ??= [];

        var jukebox = loader.jukebox;
        var currentBeat = jukebox.CurrentBeat;

        if (Stop)
        {
            if (ctx.TryGetValue(sound, out var lastBeat))
            {
                if (currentBeat != lastBeat)
                {
                    double lastSeconds = jukebox.BeatsToSecondsDouble(lastBeat);
                    double seconds = jukebox.BeatsToSecondsDouble(beat);
                    sound.Schedule(lastSeconds, seconds);
                }
                else if (currentBeat != beat)
                {
                    Context.Play(sound);
                    loader.scheduler.Schedule(beat, () => Context.Stop(sound));
                }
                ctx.Remove(sound);
            }
            else if (currentBeat != beat)
            {
                loader.scheduler.Schedule(beat, () => Context.Stop(sound));
            }
            else
            {
                Context.Stop(sound);
            }
        }

        if (Play)
        {
            if (ctx.TryGetValue(sound, out var lastBeat))
            {
                if (currentBeat != lastBeat)
                {
                    double lastSeconds = jukebox.BeatsToSecondsDouble(lastBeat);
                    sound.Schedule(lastSeconds);
                }
                else
                {
                    Context.Play(sound);
                }
            }
            ctx[sound] = beat;
        }

        return ctx;
    }

    public void ApplyLoaderFinalize(MixtapeLoaderCustom loader, object ctx)
    {
        ApplyLoaderFinalize(loader, (Dictionary<TempoSound, float>)ctx);
    }

    public void ApplyLoaderFinalize(MixtapeLoaderCustom loader, Dictionary<TempoSound, float> ctx)
    {
        foreach (var pair in ctx)
        {
            if (loader.jukebox.CurrentBeat == pair.Value)
            {
                Context.Play(pair.Key);
            }
            else
            {
                double lastSeconds = loader.jukebox.BeatsToSecondsDouble(pair.Value);
                pair.Key.Schedule(lastSeconds);
            }
        }
    }
}