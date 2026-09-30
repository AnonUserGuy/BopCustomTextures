using System;
using System.Collections;
using System.Collections.Generic;
using static InputManager;

namespace BopCustomTextures.Customs.Scenes;

public class InputSyncedScene(ISceneResolved mobj, Entity entity)
{
    private int Count = entity.GetInt("count");

    private readonly ISceneResolved Mobj = mobj;

    private readonly float Length = entity.length;
    private readonly Judgement MinJudgement = JudgementFromString(entity.GetString("minJudgement"));
    private readonly Judgement MaxJudgement = JudgementFromString(entity.GetString("maxJudgement"));
    private readonly bool Early = entity.GetBool("early");
    private readonly bool Late = entity.GetBool("late");
    private readonly float BeatOffset = entity.GetFloat("beatOffset");
    private readonly float Offset = entity.GetFloat("offset");

    private static Judgement JudgementFromString(string str) => str switch
    {
        "miss" => Judgement.Miss,
        "bad" => Judgement.Bad,
        "almost" => Judgement.Almost,
        "hit" => Judgement.Hit,
        _ => Judgement.Perfect,
    };

    private bool SufficientJudgement(Judgement judgement, bool early)
        => judgement >= MinJudgement && judgement <= MaxJudgement && (judgement != Judgement.Almost || early ? Early : Late);

    public bool HasApplyInstant() => BeatOffset == 0 && Offset == 0 && Mobj.HasApplyInstant();

    /// <returns><see langword="true"/> if mobj is done being applied and should be removed from callbacks.</returns>
    public bool ApplyInstant(MixtapeLoaderCustom __instance)
    {
        if (Count == 0)
        {
            return true;
        }
        Mobj.ApplyInstant();
        return false;
    }

    /// <returns><see langword="true"/> if mobj is done being applied and should be removed from callbacks.</returns>
    public bool ApplyInstant(MixtapeLoaderCustom __instance, Judgement judgement, bool early)
    {
        if (Count == 0)
        {
            return true;
        }
        if (!SufficientJudgement(judgement, early))
        {
            return false;
        }
        Mobj.ApplyInstant();
        return false;
    }

    private void ApplyInternal(MixtapeLoaderCustom __instance)
    {
        var jukebox = __instance.jukebox;
        var beat = jukebox.CurrentBeat + BeatOffset;
        if (Offset != 0f)
        {
            beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(beat) + Offset);
        }
        Mobj.Apply(__instance, beat, Length);
    }

    /// <returns><see langword="true"/> if mobj is done being applied and should be removed from callbacks.</returns>
    public bool Apply(MixtapeLoaderCustom __instance)
    {
        if (Count == 0)
        {
            return true;
        }
        ApplyInternal(__instance);
        return --Count == 0;
    }

    /// <returns><see langword="true"/> if mobj is done being applied and should be removed from callbacks.</returns>
    public bool Apply(MixtapeLoaderCustom __instance, Judgement judgement, bool early)
    {
        if (Count == 0)
        {
            return true;
        }

        if (!SufficientJudgement(judgement, early))
        {
            return false;
        }
        ApplyInternal(__instance);
        return --Count == 0;
    }

    public void Remove() => Count = 0;
}

public abstract class OnActionCallbackBase(MixtapeLoaderCustom __instance)
{
    protected readonly MixtapeLoaderCustom Loader = __instance;
    protected readonly List<InputSyncedScene> MobjsPre = [];
    protected readonly List<InputSyncedScene> MobjsPost = [];

    public void Add(InputSyncedScene mobj)
    {
        if (mobj.HasApplyInstant())
        {
            MobjsPre.Add(mobj);
        }
        MobjsPost.Add(mobj);
    }
}

public class OnActionCallback : OnActionCallbackBase
{
    private readonly Func<float, Judgement, bool, bool, uint, IEnumerator> Original;

    public OnActionCallback(MixtapeLoaderCustom __instance, OnActionDownCallback original) : base(__instance)
    {
        Original = original != null ? new Func<float, Judgement, bool, bool, uint, IEnumerator>(original) : null;
    }

    public OnActionCallback(MixtapeLoaderCustom __instance, OnActionUpCallback original) : base(__instance)
    {
        Original = original != null ? new Func<float, Judgement, bool, bool, uint, IEnumerator>(original) : null;
    }

    public IEnumerator GetEnumerator(float target, Judgement judgement, bool early, bool taken, uint vkey)
    {
        ApplyPre(judgement, early);

        if (Original != null)
        {
            var enumerator = Original(target, judgement, early, taken, vkey);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
        else
        {
            yield return null;
        }

        ApplyPost(judgement, early);
    }

    private void ApplyPre(Judgement judgement, bool early)
    {
        for (var i = 0; i < MobjsPre.Count;)
        {
            if (MobjsPre[i].ApplyInstant(Loader, judgement, early))
            {
                MobjsPre.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    private void ApplyPost(Judgement judgement, bool early)
    {
        for (var i = 0; i < MobjsPost.Count;)
        {
            if (MobjsPost[i].Apply(Loader, judgement, early))
            {
                MobjsPost.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }
}

public class OnActionMissCallback : OnActionCallbackBase
{
    private readonly Func<float, IEnumerator> Original;

    public OnActionMissCallback(MixtapeLoaderCustom __instance, OnActionDownMissCallback original) : base(__instance)
    {
        Original = original != null ? new Func<float, IEnumerator>(original) : null;
    }

    public OnActionMissCallback(MixtapeLoaderCustom __instance, OnActionUpMissCallback original) : base(__instance)
    {
        Original = original != null ? new Func<float, IEnumerator>(original) : null;
    }

    public IEnumerator GetEnumerator(float target)
    {
        ApplyPre();

        if (Original != null)
        {
            var enumerator = Original(target);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
        else
        {
            yield return null;
        }

        ApplyPost();
    }

    private void ApplyPre()
    {
        for (var i = 0; i < MobjsPre.Count;)
        {
            if (MobjsPre[i].ApplyInstant(Loader))
            {
                MobjsPre.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    private void ApplyPost()
    {
        for (var i = 0; i < MobjsPost.Count;)
        {
            if (MobjsPost[i].Apply(Loader))
            {
                MobjsPost.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }
}

