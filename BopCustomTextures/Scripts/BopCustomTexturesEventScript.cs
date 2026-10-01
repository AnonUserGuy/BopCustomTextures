using BopCustomTextures.Config;
using UnityEngine;
using static BopCustomTextures.BopCustomTexturesPlugin;

namespace BopCustomTextures.Scripts;

public class BopCustomTexturesEventScript : MonoBehaviour
{
    public MixtapeEventScript eventScript;

    public string eventName;

    public string lastScene;
    
    public string lastKey;

    public bool hasKey;

    public bool isVariant;

    public Color accentColor;

    public static BopCustomTexturesEventScript ApplyIfNecessary(MixtapeEventScript eventScript)
    {
        if (!eventScript.Datamodel.StartsWith(MyPluginInfo.PLUGIN_GUID) 
            || !eventScript.Entity.dynamicData.TryGetValue("scene", out var sceneObj))
        {
            return null;
        }
        
        var isVariant = false;
        var hasKey = eventScript.Entity.dynamicData.TryGetValue("key", out var keyObj);
        if (!hasKey)
        {
            hasKey = isVariant = eventScript.Entity.dynamicData.TryGetValue("variant", out keyObj);
        }

        var component = eventScript.gameObject.AddComponent<BopCustomTexturesEventScript>();
        component.eventScript = eventScript;
        component.eventName = eventScript.label.text;
        component.lastScene = (string)sceneObj;
        component.hasKey = hasKey;
        if (hasKey)
        {
            component.lastKey = (string)keyObj;
        }
        component.isVariant = isVariant;
        component.UpdateDisplay();

        return component;
    }

    public static void FixAccentColorIfNecessary(MixtapeEventScript eventScript)
    {
        if (eventScript.gameObject.TryGetComponent<BopCustomTexturesEventScript>(out var component))
        {
            component.UpdateColor();
        }
    }

    private void Update()
    {
        var key = hasKey ? eventScript.Entity.GetString(isVariant ? "variant" : "key") : null;
        var scene = eventScript.Entity.GetString("scene");
        if (key != lastKey || scene != lastScene)
        {
            lastKey = key;
            lastScene = scene;
            UpdateDisplay();
        }
    }

    public void UpdateDisplay()
    {
        var entity = eventScript.Entity;
        if (hasKey)
        {
            eventScript.label.text = $"{lastKey} <alpha=#C0>({eventName})";
        }
        accentColor = GetColor(lastScene);
        UpdateColor();
    }

    public void UpdateColor()
    {
        if (Instance.ConfigManager.CustomEventAppearance.Value == CustomEventAppearance.Inverted)
        {
            eventScript.spriteRenderer.color = accentColor;
        }
        else
        {
            eventScript.accentRenderer.color = accentColor;
        }
    }

    public static Color GetColor(string scene) => scene.ToLowerInvariant() switch
    {
        "_"                 => MixtapeEditorColorManager.Instance.GetColor(MixtapeEditorColorManager.ColorKey.EventDefault),
        "flippersnapper"    => MixtapeEditorColorManager.Instance.flipperSnapperColor,
        "sweettooth"        => MixtapeEditorColorManager.Instance.sweetToothColor,
        "rockpapershowdown" => MixtapeEditorColorManager.Instance.rockPaperShowdownColor,
        "pantryparade"      => MixtapeEditorColorManager.Instance.pantryParadeColor,
        "bbot"              => MixtapeEditorColorManager.Instance.bBotColor,
        "flowworms"         => MixtapeEditorColorManager.Instance.flowWormsColor,
        "meetandtweet"      => MixtapeEditorColorManager.Instance.meetAndTweetColor,
        "steadybears"       => MixtapeEditorColorManager.Instance.steadyBearsColor,
        "popupkitchen"      => MixtapeEditorColorManager.Instance.popUpKitchenColor,
        "fireworkfestival"  => MixtapeEditorColorManager.Instance.fireworkFestivalColor,
        "hammertime"        => MixtapeEditorColorManager.Instance.hammerTimeColor,
        "molecano"          => MixtapeEditorColorManager.Instance.molecanoColor,
        "presidentbird"     => MixtapeEditorColorManager.Instance.presidentBirdColor,
        "snakedown"         => MixtapeEditorColorManager.Instance.snakedownColor,
        "octeaparty"        => MixtapeEditorColorManager.Instance.octeapartyColor,
        "globetrotters"     => MixtapeEditorColorManager.Instance.globetrottersColor, // this is typoed in the original?
        _ => new Color(0.5f, 0.5f, 0.5f, 0.8f),
    };
}
