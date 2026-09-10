using System.Collections.Generic;

namespace BopCustomTextures.AccessExtensions;

/// <summary>
/// Extension methods for <see cref="MixtapeEditorScript"/> exposing private fields and methods.
/// </summary>
public static class MixtapeEditorScriptExtensions
{
    public static readonly TypedMethodInfo<MixtapeEditorScript> FormatMenuMethod = new("FormatMenu", []);
    public static void FormatMenu(this MixtapeEditorScript obj) => FormatMenuMethod.Invoke(obj);


    public static readonly TypedPropertyInfo<MixtapeEditorScript, int> LevelIndexField = new("levelIndex");
    public static int GetLevelIndex(this MixtapeEditorScript instance) =>
        LevelIndexField.GetValue(instance);
    public static bool SetLevelIndex(this MixtapeEditorScript instance, int value) =>
        LevelIndexField.SetValue(instance, value);


    public static readonly TypedFieldInfo<MixtapeEditorScript, Dictionary<string, MixtapeEventScript>> SingletonEventsField = new("singletonEvents");
    public static Dictionary<string, MixtapeEventScript> GetSingletonEvents(this MixtapeEditorScript instance) =>
        SingletonEventsField.GetValue(instance);
    public static bool SetSingletonEvents(this MixtapeEditorScript instance, Dictionary<string, MixtapeEventScript> value) =>
        SingletonEventsField.SetValue(instance, value);


    public static readonly TypedFieldInfo<MixtapeEditorScript, HashList<MixtapeEventScript>> SelectedEventsField = new("selectedEvents");
    public static HashList<MixtapeEventScript> GetSelectedEvents(this MixtapeEditorScript instance) =>
        SelectedEventsField.GetValue(instance);
    public static bool SetSelectedEvents(this MixtapeEditorScript instance, HashList<MixtapeEventScript> value) =>
        SelectedEventsField.SetValue(instance, value);


    public static readonly TypedFieldInfo<MixtapeEditorScript, MixtapeEditorMinigameButton[]> MinigameButtonsField = new("minigameButtons");
    public static MixtapeEditorMinigameButton[] GetMinigameButtons(this MixtapeEditorScript instance) =>
        MinigameButtonsField.GetValue(instance);
    public static bool SetMinigameButtons(this MixtapeEditorScript instance, MixtapeEditorMinigameButton[] value) =>
        MinigameButtonsField.SetValue(instance, value);
}
