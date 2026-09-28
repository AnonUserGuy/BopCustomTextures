using BopCustomTextures.AccessExtensions.TypedInfo;
using System.Collections.Generic;
using static InputManager;

namespace BopCustomTextures.AccessExtensions;

public static class InputManagerExtensions
{
    public static TypedFieldInfo<InputManager, Dictionary<Action, OnActionDownCallback>> OnActionDownCallbacksField = new("onActionDownCallbacks");

    public static Dictionary<Action, OnActionDownCallback> GetOnActionDownCallbacks(this InputManager __instance) 
        => OnActionDownCallbacksField.GetValue(__instance);
    public static bool SetOnActionDownCallbacks(this InputManager __instance, Dictionary<Action, OnActionDownCallback> val)
        => OnActionDownCallbacksField.SetValue(__instance, val);
}
