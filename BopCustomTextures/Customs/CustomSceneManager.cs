using BopCustomTextures.Json;
using BopCustomTextures.Customs.Scenes;
using BopCustomTextures.SceneMods.Unity;
using BopCustomTextures.SceneMods.Unity.Components;
using BopCustomTextures.SceneMods.System;
using BopCustomTextures.AccessExtensions;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

using LoaderComponentContexts = Dictionary<Type, MLoaderComponentContext>;
using OnActionCallbacks = Dictionary<SceneKey, Dictionary<bool, Dictionary<Action, OnActionCallback>>>;
using OnActionMissCallbacks = Dictionary<SceneKey, Dictionary<bool, Dictionary<Action, OnActionMissCallback>>>;

/// <summary>
/// Manages scene mods, including loading them from the source file and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Passed to CustomJsonInitializer.</param>
/// <param name="mixtapeEventTemplate">Mixtape event template for applying scene mods.</param>
public class CustomSceneManager(ILogger logger, CustomVariantNameManager variantManager, MixtapeEventTemplate[] mixtapeEventTemplates) : BaseCustomManager(logger)
{
    public static readonly Regex PathRegex = new Regex(@"[\\/](?:level|scene)s?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static readonly Regex FileRegex = new Regex(@"([a-z]+)[^\\/]*\.jsonc?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static readonly Regex MixtapeEventRegex = new Regex(@"^" + MyPluginInfo.PLUGIN_GUID + @"/apply (offset )?scene mod( on input)?$", RegexOptions.Compiled);

    public MixtapeEventTemplate[] MixtapeEventTemplates = mixtapeEventTemplates;
    public CustomJsonInitializer JsonInitializer = new CustomJsonInitializer(logger, variantManager);

    public readonly Dictionary<SceneKey, Dictionary<string, IScene>> CustomScenes = [];
    public readonly Dictionary<SceneKey, Dictionary<string, ISceneResolved>> CustomScenesResolved = [];
    public readonly Dictionary<string, Dictionary<SceneKey, List<string>>> CustomSceneSetDataModels = [];

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

        bool isSimple = true;
        if (JsonInitializer.Mixtape.Release >= 2)
        {
            if (JsonInitializer.TryGetJObject(jobj, "init", out var jinit))
            {
                isSimple = false;
                JsonParseScene(scene, "", jinit);
            }
            if (JsonInitializer.TryGetJObject(jobj, "events", out var jevents))
            {
                isSimple = false;
                foreach (KeyValuePair<string, JToken> dict in jevents)
                {
                    if (dict.Value is JObject jeventObj)
                    {
                        JsonParseScene(scene, dict.Key, jeventObj);
                    }
                    else
                    {
                        Logger.LogJsonParseError(jevents.Path, "event scene", "must be object");
                    }
                }
            }
            if (JsonInitializer.TryGetJObject(jobj, "eventSets", out var jeventSets))
            {
                isSimple = false;
                JsonParseSceneSets(scene, jeventSets);
            }
        }
        if (isSimple)
        {
            JsonParseScene(scene, "", jobj);
        }
    }

    private void AddCustomScene(SceneKey scene, string key, IScene mobj)
    {
        if (!CustomScenes.TryGetValue(scene, out var customScenes))
        {
            customScenes = [];
            CustomScenes[scene] = customScenes;
        }
        else if(customScenes.TryGetValue(key, out IScene oldMobj))
        {
            mobj = SceneMultiple.Add(oldMobj, mobj);
        }
        CustomScenes[scene][key] = mobj;
    }

    private void AddCustomScene(SceneKey scene, string key, SceneSetElement[] elements)
    {
        SceneSet set = new(elements, scene);
        AddCustomScene(scene, key, set);
    }

    private void JsonParseScene(SceneKey scene, string key, JObject jobj)
    {
        if (JsonInitializer.TryGetMGameObject(jobj, scene, out var mobj))
        {
            AddCustomScene(scene, key, mobj);
        }
        else
        {
            Logger.LogJsonParseError($"{scene}/{jobj.Path}", "event", "doesn't do anything");
        }
    }

    private void JsonParseSceneSets(SceneKey scene, JObject jeventSets)
    {
        foreach (var pair in jeventSets)
        {
            if (pair.Value is JObject jobj)
            {
                string key = pair.Key;
                if (jobj.TryGetValue("events", out var jevents) || jobj.TryGetValue("event", out jevents))
                {
                    SceneSetElement[] list;
                    if (jevents is JArray jeventsArray)
                    {
                        list = JsonParseSceneSet(jeventsArray);
                    }
                    else if (jevents is JObject jeventsObj)
                    {
                        list = JsonParseSceneSet(jeventsObj);
                    }
                    else
                    {
                        Logger.LogJsonParseError($"{scene}/{jobj.Path}", "event set object events", "must be object or array");
                        continue;
                    }

                    float? loop = JsonInitializer.TryGetJFloat(jobj, "loop", out float _float) ? _float : null;
                    SceneSet set = new(list, scene, loop);

                    AddCustomScene(scene, key, set);

                    if (jobj.TryGetValue("dataModel", out var jdataModel))
                    {
                        foreach (var dataModel0 in JsonInitializer.GetJStrings(jdataModel))
                        {
                            var dataModel = dataModel0[0] == '/' ? FromSceneKeyOrInvalid(scene) + dataModel0 : dataModel0;
                            if (!CustomSceneSetDataModels.TryGetValue(dataModel, out var eventSetsScene))
                            {
                                eventSetsScene = [];
                                CustomSceneSetDataModels[dataModel] = eventSetsScene;
                            }
                            if (!eventSetsScene.TryGetValue(scene, out var eventSets))
                            {
                                eventSets = [];
                                eventSetsScene[scene] = eventSets;
                            }
                            eventSets.Add(key);
                        }
                    }
                }
                else
                {
                    Logger.LogJsonParseError($"{scene}/{jobj.Path}", "event set object", "missing \"events\"");
                }
            }
            else if (pair.Value is JArray jarray)
            {
                AddCustomScene(scene, pair.Key, JsonParseSceneSet(jarray));
            }
            else
            {
                Logger.LogJsonParseError($"{scene}/{pair.Value.Path}", "event set", "must be object or array");
            }
        }
    }

    private SceneSetElement[] JsonParseSceneSet(JObject jobj)
    {
        List<SceneSetElement> list = [];
        foreach (var pair in jobj)
        {
            if (!MFloat.TryJsonParseKey(JsonInitializer, pair.Key, out float beat))
            {
                Logger.LogJsonParseError(pair.Value.Path, "event set event key", "must be parseable to float");
                continue;
            }
            var jel = pair.Value;

            if (jel is JObject jelObj)
            {
                SceneSetAddElement(list, jelObj, beat);
            }
            else if (JsonInitializer.TryGetJStrings(jel, out var jstrings))
            {
                list.Add(new(beat));
                foreach (var jstring in jstrings)
                {
                    SceneSetAddElement(list, jstring);
                }
            }
            else
            {
                Logger.LogJsonParseError(jel.Path, "event set", "must be object, array of strings, or string");
            }
        }
        return list.ToArray();
    }

    private SceneSetElement[] JsonParseSceneSet(JArray jarray)
    {
        List<SceneSetElement> list = [];
        foreach (var jel in jarray)
        {
            if (jel is JObject jelObj)
            {
                SceneSetAddElement(list, jelObj);
            }
            else if (JsonInitializer.TryGetJStrings(jel, out var jstrings))
            {
                foreach (var jstring in jstrings)
                {
                    SceneSetAddElement(list, jstring);
                }
            }
            else
            {
                Logger.LogJsonParseError(jel.Path, "event set", "must be object, array of strings, or string");
            }
        }
        return list.ToArray();
    }

    private void SceneSetAddElement(List<SceneSetElement> list, JObject jobj, float beat = 0f)
    {
        if (JsonInitializer.TryGetJFloat(jobj, "beat", out var _float)) beat = _float;
        float? length = JsonInitializer.TryGetJFloat(jobj, "length", out _float) ? _float : null;
        float? ratio = JsonInitializer.TryGetJFloat(jobj, "ratio", out _float) ? _float : null;
        float? offset = JsonInitializer.TryGetJFloat(jobj, "offset", out _float) ? _float : null;
        bool useLength = JsonInitializer.TryGetJBool(jobj, "useLength");
        bool applyByEnd = JsonInitializer.TryGetJBool(jobj, "applyByEnd");

        SceneSetElement el = new(beat, length, ratio, offset, useLength, applyByEnd);

        if (jobj.TryGetValue("key", out var jnames)
            || jobj.TryGetValue("keys", out jnames)
            || jobj.TryGetValue("name", out jnames)
            || jobj.TryGetValue("names", out jnames)) 
        {
            foreach (var name in JsonInitializer.GetJStrings(jnames))
            {
                el.Names.Add(name);
            }
        }
        list.Add(el);
    }

    private void SceneSetAddElement(List<SceneSetElement> list, string str)
    {
        if (list.Count <= 0)
        {
            SceneSetElement el = new(0f);
            el.Names.Add(str);
            list.Add(el);
        }
        else
        {
            list[list.Count - 1].Names.Add(str);
        }
    }

    public void UnloadCustomScenes()
    {
        if (CustomScenes.Count > 0)
        {
            Logger.LogUnloading("Unloading all custom scenes");
            CustomScenes.Clear();
            CustomScenesResolved.Clear();
            CustomSceneSetDataModels.Clear();
            LastMixtapeLoader = null;
        }
    }

    public bool TryResolveCustomScene(MixtapeLoaderCustom __instance, SceneKey scene, string key, out ISceneResolved mobjResolved)
    {
        // check if same mixtape loader, meaning root game objects haven't changed
        if (__instance != LastMixtapeLoader)
        {
            LastMixtapeLoader = __instance;
            CustomScenesResolved.Clear();
        }

        if (!CustomScenesResolved.TryGetValue(scene, out var mobjsResolved))
        {
            mobjsResolved = [];
            CustomScenesResolved[scene] = mobjsResolved;
        }

        if (!mobjsResolved.TryGetValue(key, out mobjResolved))
        {
            if (!__instance.RootObjects.TryGetValue(scene, out var rootObj))
            {
                return false;
            }
            return TryResolveCustomScene(rootObj, scene, key, out mobjResolved);
        }
        return true;
    }

    public bool TryResolveCustomScene(GameObject rootObj, SceneKey scene, string key, out ISceneResolved mobjResolved)
    {
        if (!CustomScenesResolved.TryGetValue(scene, out var mobjsResolved))
        {
            mobjsResolved = [];
            CustomScenesResolved[scene] = mobjsResolved;
        }

        if (!mobjsResolved.TryGetValue(key, out mobjResolved))
        {
            if (CustomScenes.ContainsKey(scene)
                && CustomScenes[scene].TryGetValue(key, out var mobj)
                && mobj.TryResolve(this, rootObj, scene, key, out mobjResolved))
            {
                mobjsResolved[key] = mobjResolved;
                return true;
            }
            return false;
        }
        return true;
    }

    public void InitCustomScene(MixtapeLoaderCustom __instance, SceneKey scene, string key = "")
    {
        if (!TryResolveCustomScene(__instance, scene, key, out var mobjResolved))
        {
            return;
        }
        Logger.LogInfo($"Applying custom scene: {scene}");
        mobjResolved.Apply();
    }

    public void InitCustomSceneDeferred(MixtapeLoaderCustom __instance, SceneKey scene, string key = "")
    {
        if (!TryResolveCustomScene(__instance, scene, key, out var mobjResolved))
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
        if (CustomSceneSetDataModels.TryGetValue(entity.dataModel, out var eventSets))
        {
            foreach (var pair in eventSets)
            {
                var scene0 = pair.Key;
                foreach (var key0 in pair.Value)
                {
                    PrepareEventInternal(__instance, ctxs, scene0, key0, entity, entity.beat, entity.length);
                }
            }
        }

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
        if (!TryGetEventScene(__instance, entity, out var scene, out _))
        {
            return;
        }

        var key = entity.GetString("key");

        float beat = entity.beat;
        if (match.Groups[1].Success && !TryGetOffsetBeat(__instance.jukebox, ref beat, entity.length,
            entity.GetFloat("offset"), 
            entity.GetBool("useLength"), 
            entity.GetBool("applyByEnd")))
        {
            return;
        }

        PrepareEventInternal(__instance, ctxs, scene, key, entity, beat, entity.length);
    }

    private void PrepareEventInternal(MixtapeLoaderCustom __instance, LoaderComponentContexts ctxs, SceneKey scene, string key,
    Entity entity, float beat, float length)
    {
        if (!TryResolveCustomScene(__instance, scene, key, out var sceneResolved))
        {
            Logger.LogError($"Attempt to apply noexistent event \"{key}\" in {scene} at beat {entity.beat}, track {entity.track}");
            return;
        }
        sceneResolved.Apply(__instance, ctxs, beat, length);
    }

    public void PrepareInputSyncedEvent(MixtapeLoaderCustom __instance, 
        OnActionCallbacks callbacks,
        OnActionMissCallbacks missCallbacks, Entity entity)
    {
        var key = entity.GetString("key");
        if (!TryGetEventScene(__instance, entity, out var scene, out var rootObj))
        {
            return;
        }

        if (!TryResolveCustomScene(__instance, scene, key, out var sceneResolved))
        {
            Logger.LogError($"Attempt to apply noexistent event \"{key}\" in {scene} at beat {entity.beat}, track {entity.track}");
            return;
        }

        var inputManager = rootObj.GetComponentInChildren<GameplayScript>()?.inputManager;
        if (inputManager == null)
        {
            Logger.LogError("couldn't find InputManager");
            return;
        }

        var mobj = new InputSyncedScene(sceneResolved, entity);

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
        OnActionCallbacks callbacks, SceneKey scene, bool onUp, Action action, float beat, InputSyncedScene mobj)
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
        OnActionMissCallbacks missCallbacks, SceneKey scene, bool onUp, Action action, float beat, InputSyncedScene mobj)
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

    private bool TryGetEventScene(MixtapeLoaderCustom __instance, Entity entity, out SceneKey scene, out GameObject rootObj)
    {
        var sceneStr = entity.GetString("scene");
        scene = ToSceneKeyOrInvalid(sceneStr);
        if (scene == SceneKey.Invalid)
        {
            Logger.LogError($"Scene \"{sceneStr}\" is not a valid scene key");
            rootObj = null;
            return false;
        }
        if (!CustomScenes.ContainsKey(scene))
        {
            Logger.LogError($"Cannot apply scene mod to vanilla scene {scene}");
            rootObj = null;
            return false;
        }
        if (!__instance.RootObjects.TryGetValue(scene, out rootObj))
        {
            Logger.LogError($"Cannot apply scene mod to missing scene {scene}");
            return false;
        }
        return true;
    }

    public static bool TryGetOffsetBeat(JukeboxScript jukebox, ref float beat,
        float length, float offset, bool useLength, bool applyByEnd)
    {
        if (offset == 0f)
        {
            return true;
        }

        if (useLength)
        {
            var max = beat + length;
            if (offset > 0f)
            {
                
                beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(beat) + offset);
                if (beat > max)
                {
                    if (!applyByEnd)
                    {
                        return false;
                    }
                    beat = max;
                }
            }
            else
            {
                var min = beat;
                beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(max) + offset);
                if (beat < min)
                {
                    if (!applyByEnd)
                    {
                        return false;
                    }
                    beat = min;
                }
            }
            return true;
        }

        beat = jukebox.SecondsToBeats(jukebox.BeatsToSeconds(beat) + offset);
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