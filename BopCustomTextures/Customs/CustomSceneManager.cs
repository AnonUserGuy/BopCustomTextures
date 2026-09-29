using BopCustomTextures.Json;
using BopCustomTextures.SceneMods.Unity;
using BopCustomTextures.SceneMods.Unity.Components;
using BopCustomTextures.AccessExtensions;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static InputManager;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

using LoaderComponentContexts = Dictionary<Type, MLoaderComponentContext>;
using OnActionCallbacks = Dictionary<SceneKey, Dictionary<bool, Dictionary<Action, CustomSceneManager.OnActionCallback>>>;
using OnActionMissCallbacks = Dictionary<SceneKey, Dictionary<bool, Dictionary<Action, CustomSceneManager.OnActionMissCallback>>>;

/// <summary>
/// Manages scene mods, including loading them from the source file and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Passed to CustomJsonInitializer.</param>
/// <param name="mixtapeEventTemplate">Mixtape event template for applying scene mods.</param>
public class CustomSceneManager(ILogger logger, CustomVariantNameManager variantManager, MixtapeEventTemplate[] mixtapeEventTemplates) : BaseCustomManager(logger)
{
    // the only reason these are public is because they have to be for the type aliases above to work
    public class InputSyncedCustomScene(MGameObjectResolved mobj, Entity entity)
    {
        private int Count = entity.GetInt("count");

        private readonly MGameObjectResolved Mobj = mobj;

        private readonly Entity Entity = entity;
        private readonly Judgement MinJudgement = JudgementFromString(entity.GetString("minJudgement"));
        private readonly Judgement MaxJudgement = JudgementFromString(entity.GetString("maxJudgement"));
        private readonly bool Early = entity.GetBool("early");
        private readonly bool Late = entity.GetBool("late");
        private readonly float BeatOffset = entity.GetFloat("beatOffset");
        private readonly float Offset = entity.GetFloat("offset");

        private static Judgement JudgementFromString(string str) => str switch
        {
            "miss"   => Judgement.Miss,
            "bad"    => Judgement.Bad,
            "almost" => Judgement.Almost,
            "hit"    => Judgement.Hit,
            _        => Judgement.Perfect,
        };

        private bool SufficientJudgement(Judgement judgement, bool early)
            => judgement >= MinJudgement && judgement <= MaxJudgement && (judgement != Judgement.Almost || early ? Early : Late);

        private void ApplyInternal(MixtapeLoaderCustom __instance)
        {
            var jukebox = __instance.jukebox;
            if (BeatOffset == 0f && Offset == 0f)
            {
                Mobj.ApplyLoader(__instance, Entity, jukebox.CurrentBeat);
                Mobj.Apply();
            }
            else
            {
                float beat;
                if (Offset == 0f)
                {
                    beat = jukebox.CurrentBeat + BeatOffset;
                } 
                else
                {
                    beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(jukebox.CurrentBeat + BeatOffset) + Offset);
                }
                Mobj.ApplyLoader(__instance, Entity, beat);
                __instance.scheduler.Schedule(beat, Mobj.Apply);
            }
        }

        public bool Apply(MixtapeLoaderCustom __instance)
        {
            if (Count == 0)
            {
                return true;
            }
            ApplyInternal(__instance);
            return --Count == 0;
        }

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

    public abstract class OnActionCallbackEither(MixtapeLoaderCustom __instance)
    {
        protected readonly MixtapeLoaderCustom Loader = __instance;
        protected readonly List<InputSyncedCustomScene> Mobjs = [];

        public void Add(InputSyncedCustomScene mobj)
        {
            Mobjs.Add(mobj);
        }

        protected void Apply()
        {
            for (var i = 0; i < Mobjs.Count;)
            {
                if (Mobjs[i].Apply(Loader))
                {
                    Mobjs.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        protected void Apply(Judgement judgement, bool early)
        {
            for (var i = 0; i < Mobjs.Count;)
            {
                if (Mobjs[i].Apply(Loader, judgement, early))
                {
                    Mobjs.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }

    public class OnActionCallback : OnActionCallbackEither
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

            Apply(judgement, early);
        }
    }

    public class OnActionMissCallback : OnActionCallbackEither
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

            Apply();
        }
    }


    public static readonly Regex PathRegex = new Regex(@"[\\/](?:level|scene)s?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static readonly Regex FileRegex = new Regex(@"(\w+).jsonc?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static readonly Regex MixtapeEventRegex = new Regex(@"^" + MyPluginInfo.PLUGIN_GUID + @"/apply (offset )?scene mod( on input)?$", RegexOptions.Compiled);

    public MixtapeEventTemplate[] MixtapeEventTemplates = mixtapeEventTemplates;
    public CustomJsonInitializer JsonInitializer = new CustomJsonInitializer(logger, variantManager);

    public readonly Dictionary<SceneKey, Dictionary<string, MGameObject>> CustomScenes = [];
    public readonly Dictionary<SceneKey, Dictionary<string, MGameObjectResolved>> CustomScenesResolved = [];

    private MixtapeLoaderCustom LastMixtapeLoader = null;

    public static bool IsCustomSceneDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public IEnumerable<string> LocateCustomScenes(string path, int index, MixtapeInfo info)
    {
        var filepaths = Directory.EnumerateFiles(path);
        foreach (var filepath in filepaths)
        {
            if (CheckIsCustomScene(filepath, info))
            {
                string localPath = filepath.Substring(index);
                yield return localPath;
            }
        }
    }

    public bool CheckIsCustomScene(string path, MixtapeInfo info)
    {
        Match match = FileRegex.Match(path);
        if (match.Success)
        {
            SceneKey scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene != SceneKey.Invalid)
            {
                Logger.LogFileLoading($"Found custom scene: {scene}");

                LoadCustomScene(path, scene, info);
                return true;
            } 
        }
        return false;
    }

    public void LoadCustomScene(string path, SceneKey scene, MixtapeInfo info)
    {
        JObject jobj;
        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            MemoryStream memStream = new MemoryStream(bytes);
            using StreamReader reader = new StreamReader(memStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);

            jobj = JObject.Load(jsonReader);
        }
        catch (JsonReaderException e)
        {
            Logger.LogError(e);
            return;
        }

        JsonInitializer.Mixtape = info;

        if (CustomScenes.ContainsKey(scene))
        {
            Logger.LogWarning($"Duplicate custom scene definition for scene {scene}");
        }
        CustomScenes[scene] = [];
        bool isSimple = true;
        if (JsonInitializer.Mixtape.Release >= 2)
        {
            if (JsonInitializer.TryGetJObject(jobj, "init", out var jinit))
            {
                isSimple = false;
                var mobj = JsonInitializer.InitGameObject(jinit, scene);
                if (mobj != null)
                {
                    CustomScenes[scene][""] = mobj;
                }
                else 
                {
                    Logger.LogWarning($"Init in {scene} doesn't do anything.");
                }
            }
            if (JsonInitializer.TryGetJObject(jobj, "events", out var jevents))
            {
                isSimple = false;
                foreach (KeyValuePair<string, JToken> dict in jevents)
                {
                    if (dict.Value.Type == JTokenType.Object)
                    {
                        var mobj = JsonInitializer.InitGameObject((JObject)dict.Value, scene);
                        if (mobj != null)
                        {
                            CustomScenes[scene][dict.Key] = mobj;
                        }
                        else
                        {
                            Logger.LogWarning($"Event \"{dict.Key}\" in {scene} doesn't do anything.");
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Event \"{dict.Key}\" in {scene} is a {jinit.Type} when it should be an Object.");
                    }
                }
            }
        }
        if (isSimple)
        {
            var mobj = JsonInitializer.InitGameObject(jobj, scene);
            if (mobj != null)
            {
                CustomScenes[scene][""] = mobj;
            }
            else
            {
                Logger.LogWarning($"Init in {scene} doesn't do anything.");
            }
        }
    }

    public void UnloadCustomScenes()
    {
        if (CustomScenes.Count > 0)
        {
            Logger.LogUnloading("Unloading all custom scenes");
            CustomScenes.Clear();
            CustomScenesResolved.Clear();
            LastMixtapeLoader = null;
        }
    }

    public bool TryGetCustomSceneResolved(MixtapeLoaderCustom __instance, SceneKey scene, string key, out MGameObjectResolved mobjResolved)
    {
        // check if same mixtape loader, meaning root game objects haven't changed
        if (__instance != LastMixtapeLoader)
        {
            LastMixtapeLoader = __instance;
            CustomScenesResolved.Clear();
        }

        // check game has resolved some custom scenes
        if (!CustomScenesResolved.TryGetValue(scene, out var mobjsResolved))
        {
            mobjsResolved = [];
            CustomScenesResolved[scene] = mobjsResolved;
        }

        // check this custom scene has been resolved
        if (!mobjsResolved.TryGetValue(key, out mobjResolved))
        {
            // check if game present and game has custom scenes and game has custom scene of name key
            if (!CustomScenes.ContainsKey(scene) ||
                !CustomScenes[scene].TryGetValue(key, out var mobj) ||
                !__instance.RootObjects.TryGetValue(scene, out var rootObj))
            {
                return false;
            }

            mobjResolved = ResolveGameObject(rootObj, rootObj, mobj);
        }
        return true;
    }

    public void InitCustomScene(MixtapeLoaderCustom __instance, SceneKey scene, string key = "")
    {
        if (!TryGetCustomSceneResolved(__instance, scene, key, out var mobjResolved))
        {
            return;
        }
        Logger.LogInfo($"Applying custom scene: {scene}");
        mobjResolved.Apply();
    }

    public void InitCustomSceneDeferred(MixtapeLoaderCustom __instance, SceneKey scene, string key = "")
    {
        if (!TryGetCustomSceneResolved(__instance, scene, key, out var mobjResolved))
        {
            return;
        }
        Logger.LogInfo($"Applying custom scene (deferred): {scene}");
        mobjResolved.Apply();
    }

    public void PrepareEvents(MixtapeLoaderCustom __instance, Entity[] entities)
    {
        LoaderComponentContexts ctxs = [];
        OnActionCallbacks callbacks = [];
        OnActionMissCallbacks missCallbacks = [];
        foreach (Entity entity in entities)
        {
            PrepareEvent(__instance, ctxs, callbacks, missCallbacks, entity);
        }
        MGameObjectResolved.ApplyLoaderFinalize(__instance, ctxs);
    }

    public void PrepareEvent(MixtapeLoaderCustom __instance, 
        LoaderComponentContexts ctxs,
        OnActionCallbacks callbacks,
        OnActionMissCallbacks missCallbacks, Entity entity)
    {
        var match = MixtapeEventRegex.Match(entity.dataModel);
        if (!match.Success)
        {
            return;
        }
        if (match.Groups[2].Success)
        {
            PrepareInputSyncedEvent(__instance, callbacks, missCallbacks, entity);
            return;
        }

        float beat;
        if (!match.Groups[1].Success)
        {
            beat = entity.beat;
        }
        else if (!TryGetOffsetBeat(entity,  __instance.jukebox, out beat))
        {
            return;
        }

        if (!TryGetEventMObj(__instance, entity, out _, out _, out var mobjResolved))
        {
            return;
        }

        mobjResolved.ApplyLoader(__instance, ctxs, entity, beat);
        __instance.scheduler.Schedule(beat, mobjResolved.Apply);
    }

    public void PrepareInputSyncedEvent(MixtapeLoaderCustom __instance, 
        OnActionCallbacks callbacks,
        OnActionMissCallbacks missCallbacks, Entity entity)
    {
        if (!TryGetEventMObj(__instance, entity, out var scene, out var rootObj, out var mobjResolved))
        {
            return;
        }

        var inputManager = rootObj.GetComponentInChildren<GameplayScript>()?.inputManager;
        if (inputManager == null)
        {
            Logger.LogError("couldn't find InputManager");
            return;
        }

        var mobj = new InputSyncedCustomScene(mobjResolved, entity);

        Action action = entity.GetString("action") == "primary" ? Action.Primary : Action.Secondary;
        bool onUp = entity.GetBool("onUp");
        bool applyByMiss = entity.GetBool("applyByMiss");

        AddOnActionCallback(__instance, inputManager, callbacks, scene, onUp, action, entity.beat, mobj);
        if (applyByMiss)
        {
            AddOnActionMissCallback(__instance, inputManager, missCallbacks, scene, onUp, action, entity.beat, mobj);
        }

        bool useLength = entity.GetBool("useLength");
        bool applyByEnd = entity.GetBool("applyByEnd");

        if (useLength)
        {
            if (applyByEnd)
            {
                __instance.scheduler.Schedule(entity.beat + entity.length, () => mobj.Apply(__instance));
            }
            else
            {
                __instance.scheduler.Schedule(entity.beat + entity.length, mobj.Remove);
            }
        }
    }

    private void AddOnActionCallback(MixtapeLoaderCustom __instance, InputManager inputManager,
        OnActionCallbacks callbacks, SceneKey scene, bool onUp, Action action, float beat, InputSyncedCustomScene mobj)
    {
        if (!callbacks.TryGetValue(scene, out var callbacksUpDown))
        {
            callbacksUpDown = [];
            callbacks[scene] = callbacksUpDown;
        }
        if (!callbacksUpDown.TryGetValue(onUp, out var callbacksAction))
        {
            callbacksAction = [];
            callbacksUpDown[onUp] = callbacksAction;
        }
        if (!callbacksAction.TryGetValue(action, out var callback))
        {
            if (!onUp)
            {
                if (!InputManagerExtensions.OnActionDownCallbacksField.Exists())
                {
                    Logger.LogError("InputManager.onActionDownCallbacks doesn't exist");
                    return;
                }
                var originalCallbacks = inputManager.GetOnActionDownCallbacks();
                var originalCallback = originalCallbacks.TryGetValue(action, out var _original) ? _original : null;

                callback = new OnActionCallback(__instance, originalCallback);
                originalCallbacks[action] = callback.GetEnumerator;
            }
            else
            {
                if (!InputManagerExtensions.OnActionUpCallbacksField.Exists())
                {
                    Logger.LogError("InputManager.onActionUpCallbacks doesn't exist");
                    return;
                }
                var originalCallbacks = inputManager.GetOnActionUpCallbacks();
                var originalCallback = originalCallbacks.TryGetValue(action, out var _original) ? _original : null;

                callback = new OnActionCallback(__instance, originalCallback);
                originalCallbacks[action] = callback.GetEnumerator;
            }
        }
        __instance.scheduler.Schedule(beat, () => callback.Add(mobj));
    }

    private void AddOnActionMissCallback(MixtapeLoaderCustom __instance, InputManager inputManager,
        OnActionMissCallbacks missCallbacks, SceneKey scene, bool onUp, Action action, float beat, InputSyncedCustomScene mobj)
    {
        if (!missCallbacks.TryGetValue(scene, out var callbacksUpDown))
        {
            callbacksUpDown = [];
            missCallbacks[scene] = callbacksUpDown;
        }
        if (!callbacksUpDown.TryGetValue(onUp, out var callbacksAction))
        {
            callbacksAction = [];
            callbacksUpDown[onUp] = callbacksAction;
        }
        if (!callbacksAction.TryGetValue(action, out var callback))
        {
            if (!onUp)
            {
                if (!InputManagerExtensions.OnActionDownMissCallbacksField.Exists())
                {
                    Logger.LogError("InputManager.onActionDownMissCallbacks doesn't exist");
                    return;
                }
                var originalCallbacks = inputManager.GetOnActionDownMissCallbacks();
                var originalCallback = originalCallbacks.TryGetValue(action, out var _original) ? _original : null;

                callback = new OnActionMissCallback(__instance, originalCallback);
                originalCallbacks[action] = callback.GetEnumerator;
            }
            else
            {
                if (!InputManagerExtensions.OnActionUpMissCallbacksField.Exists())
                {
                    Logger.LogError("InputManager.onActionUpMissCallbacks doesn't exist");
                    return;
                }
                var originalCallbacks = inputManager.GetOnActionUpMissCallbacks();
                var originalCallback = originalCallbacks.TryGetValue(action, out var _original) ? _original : null;

                callback = new OnActionMissCallback(__instance, originalCallback);
                originalCallbacks[action] = callback.GetEnumerator;
            }
        }
        __instance.scheduler.Schedule(beat, () => callback.Add(mobj));
    }

    private bool TryGetEventMObj(MixtapeLoaderCustom __instance, Entity entity, out SceneKey scene, out GameObject rootObj, out MGameObjectResolved mobjResolved)
    {
        var key = entity.GetString("key");
        var sceneStr = entity.GetString("scene");
        scene = ToSceneKeyOrInvalid(sceneStr);
        if (scene == SceneKey.Invalid)
        {
            Logger.LogError($"Scene \"{sceneStr}\" is not a valid scene key");
            rootObj = null;
            mobjResolved = null;
            return false;
        }
        if (!CustomScenes.ContainsKey(scene))
        {
            Logger.LogError($"Cannot apply scene mod to vanilla scene {scene}");
            rootObj = null;
            mobjResolved = null;
            return false;
        }
        if (!__instance.RootObjects.TryGetValue(scene, out rootObj))
        {
            Logger.LogError($"Cannot apply scene mod to missing scene {scene}");
            mobjResolved = null;
            return false;
        }
        return TryGetCustomSceneResolved(__instance, scene, key, out mobjResolved);
    }

    private static bool TryGetOffsetBeat(Entity entity, JukeboxScript jukebox, out float beat)
    {
        var offsetSeconds = entity.GetFloat("offset");
        if (offsetSeconds == 0f)
        {
            beat = entity.beat;
            return true;
        }

        if (entity.GetBool("useLength"))
        {
            var max = entity.beat + entity.length;
            if (offsetSeconds > 0f)
            {
                
                beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(entity.beat) + offsetSeconds);
                if (beat > max)
                {
                    if (!entity.GetBool("applyByEnd"))
                    {
                        return false;
                    }
                    beat = max;
                }
            }
            else
            {
                beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(max) + offsetSeconds);
                if (beat < entity.beat)
                {
                    if (!entity.GetBool("applyByEnd"))
                    {
                        return false;
                    }
                    beat = entity.beat;
                }
            }
            return true;
        }

        beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(entity.beat) + offsetSeconds);
        return true;
    }

    public bool UpdateEventTemplates()
    {
        object scenes;
        bool result;
        if (CustomScenes.Count < 1)
        {
            scenes = "";
            result = false;
        }
        else
        {
            scenes = new MixtapeEventTemplates.ChoiceField<string>(
                CustomScenes.Keys.Select(FromSceneKeyOrInvalid).ToArray());
            result = true;
        }
        foreach (var mixtapeEventTemplate in MixtapeEventTemplates)
        {
            mixtapeEventTemplate.properties["scene"] = scenes;
        }
        return result;
    }


    public MGameObjectResolved ResolveGameObject(GameObject rootObj, GameObject obj, MGameObject mobj)
    {
        var mobjResolved = new MGameObjectResolved(mobj, obj);
        var mchildObjsResolved = new List<MGameObjectResolved>();
        foreach (var mchildObj in mobj.childObjs)
        {
            bool found = false;
            foreach (var childObj in FindGameObjectsInChildren(rootObj, obj, mchildObj.name))
            {
                found = true;
                var mchildObjResolved = ResolveGameObject(rootObj, childObj, mchildObj);
                mchildObjsResolved.Add(mchildObjResolved);
            }
            if (!found)
            {
                Logger.LogWarning($"Couldn't find gameObject \"{mchildObj.name}\" in \"{obj.name}\"");
            }
        }
        mobjResolved.childObjs = mchildObjsResolved.ToArray();
        return mobjResolved;
    }

    /// <summary>
    /// Find all children of a <see cref="GameObject"/> from search path.
    /// </summary>
    /// <param name="obj">Parent <see cref="GameObject"/> to search through.</param>
    /// <param name="path">Path to child <see cref="GameObject"/>. Either single name or path using standard glob syntax.</param>
    /// <returns>Iterator of <see cref="GameObject"/>s all matching search path.</returns>
    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject obj, string path)
    {
        return FindGameObjectsInChildren(obj, obj, path);
    }

    /// <summary>
    /// Find all children of a <see cref="GameObject"/> from search path.
    /// </summary>
    /// <param name="rootObj">Root object of game. ".." can't go higher than it.</param>
    /// <param name="obj">Parent <see cref="GameObject"/> to search through.</param>
    /// <param name="path">Path to child <see cref="GameObject"/>. Either single name or path using standard glob syntax.</param>
    /// <returns>Iterator of <see cref="GameObject"/>s all matching search path.</returns>
    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject rootObj, GameObject obj, string path)
    {
        string[] names = Regex.Split(path.TrimEnd(['\\','/']), @"[\\/]");
        return FindGameObjectsInChildren(rootObj, obj, names);
    }

    /// <summary>
    /// Recursively find all children of a <see cref="GameObject"/> from search path array.
    /// </summary>
    /// <param name="rootObj">Root object of search. ".." can't go any higher than it.</param>
    /// <param name="parentObj">Object to search through children of for this recursive call.</param>
    /// <param name="names">List of names shared by entire search. Isn't modified per recursion, instead i is.</param>
    /// <param name="i">Index in list of names.</param>
    /// <param name="doublestar">Last index that was "**". -1 if not performing double-star glob search.</param>
    /// <returns>Iterator of <see cref="GameObject"/>s all matching search path past i in parentObj.</returns>
    public static IEnumerable<GameObject> FindGameObjectsInChildren(GameObject rootObj, GameObject parentObj, string[] names, 
        int i = 0, int doublestar = -1)
    {
        bool hasMatch;
        do
        {
            hasMatch = false;
            if (names[i] == ".")
            {
                i++;
                if (i == names.Length)
                {
                    yield return parentObj;
                    yield break;
                }
                hasMatch = true;
                continue;
            }
            if (names[i] == "..")
            {
                if ((rootObj != null && parentObj == rootObj) || // prevent selecting objects outside of game
                    parentObj.transform.parent == null)
                {
                    yield break;
                }
                parentObj = parentObj.transform.parent.gameObject;
                i++;
                if (i == names.Length)
                {
                    yield return parentObj;
                    yield break;
                }
                hasMatch = true;
                continue;
            }
            if (names[i] == "**")
            {
                i++;
                if (i == names.Length)
                {
                    foreach (var childObj in ThisAndAllChildren(parentObj))
                    {
                        yield return childObj;
                    }
                    yield break;
                }
                doublestar = i;
                hasMatch = true;
                continue;
            }
        } while (hasMatch);
        
        Regex reg = new Regex(WildCardToRegex(names[i]));
        for (var j = 0; j < parentObj.transform.childCount; j++)
        {
            var obj = parentObj.transform.GetChild(j).gameObject;
            if (reg.IsMatch(obj.name))
            {
                if (i == names.Length - 1)
                {
                    yield return obj;
                }
                else
                {
                    foreach (var childObj in FindGameObjectsInChildren(rootObj, obj, names, i + 1))
                    {
                        yield return childObj;
                    }
                }
            }
            if (doublestar != -1)
            {
                foreach (var childObj in FindGameObjectsInChildren(rootObj, obj, names, doublestar, doublestar))
                {
                    yield return childObj;
                }
            }
        }
    }

    /// <summary>
    /// Get all children of a <see cref="GameObject"/>, plus said <see cref="GameObject"/> itself.
    /// </summary>
    /// <param name="parentObj">Parent <see cref="GameObject"/> to get all children of, plus it.</param>
    /// <returns>Iterator of first the given <see cref="GameObject"/>, then all children of said <see cref="GameObject"/>.</returns>
    public static IEnumerable<GameObject> ThisAndAllChildren(GameObject parentObj)
    {
        yield return parentObj;
        for (var j = 0; j < parentObj.transform.childCount; j++)
        {
            var obj = parentObj.transform.GetChild(j).gameObject;
            foreach (var childObj in ThisAndAllChildren(obj))
            {
                yield return childObj;
            }
        }
    }

    private static string WildCardToRegex(string value)
    {
        return "^" + Regex.Escape(value).Replace(@"\?", ".").Replace(@"\*", ".*") + "$";
    }
}