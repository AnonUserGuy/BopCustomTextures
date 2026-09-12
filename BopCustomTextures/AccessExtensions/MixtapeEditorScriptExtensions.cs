using BopCustomTextures.AccessExtensions.TypedInfo;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="MixtapeEditorScript"/> exposing private fields and methods.
/// </summary>
public static class MixtapeEditorScriptExtensions
{
    public static readonly TypedMethodInfo<MixtapeEditorScript> FormatMenuMethod = new("FormatMenu", []);
    public static void FormatMenu(this MixtapeEditorScript obj) => FormatMenuMethod.Invoke(obj);


    public static readonly TypedMethodInfo<MixtapeEditorScript> ResetAllAndReformatMethod = new("ResetAllAndReformat", []);
    public static void ResetAllAndReformat(this MixtapeEditorScript obj) => ResetAllAndReformatMethod.Invoke(obj);


    // These only exist pre editor UI update, last: -app 1929290 -depot 1929291 -manifest 2700963706022908388 -beta beta
    public static readonly TypedFieldInfo<MixtapeEditorScript, SpriteRenderer> MenuField = new();
    public static SpriteRenderer GetMenu(this MixtapeEditorScript instance) =>
        MenuField.GetValue(instance);

    public static readonly TypedFieldInfo<MixtapeEditorScript, TMP_Text> MenuTextField = new();
    public static TMP_Text GetMenuText(this MixtapeEditorScript instance) =>
        MenuTextField.GetValue(instance);


    // These only exist post editor UI update, first: -app 1929290 -depot 1929291 -manifest 2259048567631053773 -beta beta
    public static readonly TypedMethodInfo<MixtapeEditorScript> OnSelectCategoryMethod = new("OnSelectCategory", [typeof(string)]);
    public static void OnSelectCategory_safe(this MixtapeEditorScript obj, string category) => OnSelectCategoryMethod.Invoke(obj, [category]);


    public static readonly TypedPropertyInfo<MixtapeEditorScript, int> LevelIndexField = new("levelIndex");
    public static int GetLevelIndex(this MixtapeEditorScript instance) =>
        LevelIndexField.GetValue(instance);
    public static bool SetLevelIndex(this MixtapeEditorScript instance, int value) =>
        LevelIndexField.SetValue(instance, value);


    // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    public static readonly TypedPropertyInfo<MixtapeEditorScript, int> EventIndexField = new();
    public static int GetEventIndex(this MixtapeEditorScript instance) =>
        EventIndexField.GetValue(instance);
    public static bool SetEventIndex(this MixtapeEditorScript instance, int value) =>
        EventIndexField.SetValue(instance, value);


    public static readonly TypedFieldInfo<MixtapeEditorScript, Dictionary<string, MixtapeEventScript>> SingletonEventsField = new("singletonEvents");
    public static Dictionary<string, MixtapeEventScript> GetSingletonEvents(this MixtapeEditorScript instance) =>
        SingletonEventsField.GetValue(instance);
    public static bool SetSingletonEvents(this MixtapeEditorScript instance, Dictionary<string, MixtapeEventScript> value) =>
        SingletonEventsField.SetValue(instance, value);


    public static readonly TypedFieldInfo<MixtapeEditorScript, HashList<MixtapeEventScript>> SelectedEventsField = new("selectedEvents");
    // TODO: this can be removed after release is updated to have HashList<T> class
    public static readonly TypedFieldInfo<MixtapeEditorScript, List<MixtapeEventScript>> SelectedEventsFieldList = new();
    public static int GetSelectedEventsCount(this MixtapeEditorScript instance) =>
        SelectedEventsField.Exists() ? SelectedEventsField.GetValue(instance).Count : SelectedEventsFieldList.GetValue(instance).Count;
    public static MixtapeEventScript GetSelectedEventsIndex(this MixtapeEditorScript instance, int index) =>
        SelectedEventsField.Exists() ? GetSelectedEventsIndexInternal(instance, index) : SelectedEventsFieldList.GetValue(instance)[index];
    private static MixtapeEventScript GetSelectedEventsIndexInternal(MixtapeEditorScript instance, int index) =>
        SelectedEventsField.GetValue(instance)[index]; // has to be wrapped because nonexistent this[] functions aren't handled well by .net


    // TODO: these can be removed after release is updated to have MixtapeEditorScript.singletonEvents
    public static readonly TypedMethodInfo<MixtapeEditorScript> SetSelectedEventMethod = new();
    public static void SetSelectedEvent(this MixtapeEditorScript obj, MixtapeEventScript event0, bool forceUpdate = false, bool forceClear = false) => 
        SetSelectedEventMethod.Invoke(obj, [event0, forceUpdate, forceClear]);

    public static readonly TypedMethodInfo<MixtapeEditorScript> ZSortEventsMethod = new();
    public static void ZSortEvents(this MixtapeEditorScript obj) => ZSortEventsMethod.Invoke(obj);
    public static readonly TypedMethodInfo<MixtapeEditorScript> FormatLevelsMethod = new();
    public static void FormatLevels(this MixtapeEditorScript obj) => FormatLevelsMethod.Invoke(obj);
    public static readonly TypedMethodInfo<MixtapeEditorScript> FormatEventsMethod = new();
    public static void FormatEvents(this MixtapeEditorScript obj) => FormatEventsMethod.Invoke(obj);
    public static readonly TypedMethodInfo<MixtapeEditorScript> FormatPropertiesMethod = new();
    public static void FormatProperties(this MixtapeEditorScript obj) => FormatPropertiesMethod?.Invoke(obj);
    public static readonly TypedMethodInfo<MixtapeEditorScript> FormatValuesMethod = new();
    public static void FormatValues(this MixtapeEditorScript obj) => FormatValuesMethod.Invoke(obj);


    static MixtapeEditorScriptExtensions()
    {
        // TODO: this can be removed after release is updated to have HashList<T> class
        if (!SelectedEventsField.Exists())
        {
            BopCustomTexturesPlugin.LogWarning("SelectedEvents don't exist as HashList, trying List instead");
            SelectedEventsFieldList.Find("selectedEvents");
        }

        // TODO: this can be removed after release is updated to have MixtapeEditorScript.singletonEvents
        if (!SingletonEventsField.Exists())
        {
            EventIndexField.Find("eventIndex");
            SetSelectedEventMethod.Find("SetSelectedEvent", [typeof(MixtapeEventScript), typeof(bool), typeof(bool)]);
            ZSortEventsMethod.Find("ZSortEvents", []);
            FormatLevelsMethod.Find("FormatLevels", []);
            FormatEventsMethod.Find("FormatEvents", []);
            FormatPropertiesMethod.Find("FormatProperties", []);
            FormatValuesMethod.Find("FormatValues", []);
        }

        if (!OnSelectCategoryMethod.Exists())
        {
            MenuField.Find("menu");
            MenuTextField.Find("menuText");
        }
    }
}
