using BopCustomTextures.AccessExtensions.TypedInfo;
using System.Collections.Generic;
using static InputManager;

namespace BopCustomTextures.AccessExtensions;

public static class InputManagerExtensions
{
    public static TypedFieldInfo<InputManager, Dictionary<Action, OnActionDownCallback>> OnActionDownCallbacksField = new("onActionDownCallbacks");
    public static Dictionary<Action, OnActionDownCallback> GetOnActionDownCallbacks(this InputManager __instance) 
        => OnActionDownCallbacksField.GetValue(__instance);


    public static TypedFieldInfo<InputManager, Dictionary<Action, OnActionUpCallback>> OnActionUpCallbacksField = new("onActionUpCallbacks");
    public static Dictionary<Action, OnActionUpCallback> GetOnActionUpCallbacks(this InputManager __instance)
        => OnActionUpCallbacksField.GetValue(__instance);


    public static TypedFieldInfo<InputManager, Dictionary<Action, OnActionDownMissCallback>> OnActionDownMissCallbacksField = new("onActionDownMissCallbacks");
    public static Dictionary<Action, OnActionDownMissCallback> GetOnActionDownMissCallbacks(this InputManager __instance)
        => OnActionDownMissCallbacksField.GetValue(__instance);


    public static TypedFieldInfo<InputManager, Dictionary<Action, OnActionUpMissCallback>> OnActionUpMissCallbacksField = new("onActionUpMissCallbacks");
    public static Dictionary<Action, OnActionUpMissCallback> GetOnActionUpMissCallbacks(this InputManager __instance)
        => OnActionUpMissCallbacksField.GetValue(__instance);
}
