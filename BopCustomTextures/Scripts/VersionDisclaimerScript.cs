using BopCustomTextures.Customs;
using BopCustomTextures.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Canvas = UnityEngine.Canvas;

namespace BopCustomTextures.Scripts;

/// <summary>
/// Unity component for the version disclaimer screen. 
/// 
/// Acts like a pseudo-scene, as a single gameObject with it and necessary child objects can be instantiated with Create().
/// </summary>
public class VersionDisclaimerScript : MonoBehaviour
{
    private enum Options
    {
        Resume,
        ResumeVanilla,
        OpenModPage,
        Quit,
        Count
    }
    private static readonly string[] OptionStrings =
    {
        "Attempt load anyways!",
        "Proceed without custom assets",
        "Open mod page on GitHub",
        "Quit"
    };

    public CustomManager Manager;
    public RiqLoader Loader;
    public TMP_Text TitleText;
    public TMP_Text Text;

    public const int versionMaxLength = 50;
    public static int defaultPosition = 0;
    public int position = defaultPosition;
    private int cachedPosition = -1;
    private bool performLoad = false;

    /// <summary>
    /// Creates a single VersionDisclaimer gameObject.
    /// </summary>
    /// <param name="manager">CustomManager object being prevented from loading custom assets by disclaimer.</param>
    /// <param name="riqLoader">RiqLoader object being prevented from starting mixtape by disclaimer.</param>
    /// <returns>VersionDisclaimer gameObject.</returns>
    public static GameObject Create(CustomManager manager, RiqLoader riqLoader)
    {
        var obj = new GameObject("VersionDisclaimer");
        var script = obj.AddComponent<VersionDisclaimerScript>();
        script.Manager = manager;
        script.Loader = riqLoader;
        script.TitleText.text =
            $"Mixtape requires {MyPluginInfo.PLUGIN_GUID} <color=yellow>v<noparse>{script.SanitizeVersion(manager.version)}</noparse>+</color>, " +
            $"but you are on <color=yellow>v{MyPluginInfo.PLUGIN_VERSION}</color>. You may have to update {MyPluginInfo.PLUGIN_GUID} to play properly.";
        return obj;
    }

    private void Awake()
    {
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.parent = gameObject.transform;
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        var font = FindFontOrAny("TempoCurse SDF");
        TitleText = CreateText(canvas.transform, "Title", font, 70, new Vector2(1000, 200), new Vector2(0, 150));
        Text = CreateText(canvas.transform, "Text", font, 50, new Vector2(1000, 200), new Vector2(0, -200));
        GenerateText();

        TempoInput.SetActionMap(SettingsScript.menuActionMap);
    }

    private TMP_Text CreateText(Transform parent, string name, TMP_FontAsset Font, int fontSize, Vector2 size, Vector2 anchor)
    {
        var textObj = new GameObject(name);
        textObj.transform.parent = parent;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.font = Font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchor;
        return text;
    }

    private void Update()
    {
        if (performLoad)
        {
            TempoInput.SetActionMap(SettingsScript.gameActionMap);
            Manager.ReadLastPath();
            Manager.interruptLoad = false;
            Loader.StartMixtape();
            Destroy(gameObject);
            return;
        }
        if (TempoInput.GetActionDown(Action.Down))
        {
            position++;
        }
        if (TempoInput.GetActionDown(Action.Up))
        {
            position--;
        }
        position = Utils.Mod(position, (int)Options.Count);
        GenerateText();

        if (TempoInput.GetActionDown(Action.Confirm))
        {
            switch ((Options)position)
            {
                case Options.Resume:
                    performLoad = true;
                    TitleText.text = "Loading...";
                    Text.gameObject.SetActive(false);
                    break;
                case Options.ResumeVanilla:
                    TempoInput.SetActionMap(SettingsScript.gameActionMap);
                    Manager.interruptLoad = false;
                    Loader.StartMixtape();
                    Destroy(gameObject);
                    break;
                case Options.OpenModPage:
                    Application.OpenURL(BopCustomTexturesPlugin.PluginRepoUrl + "/releases");
                    break;
                case Options.Quit:
                    TempoInput.SetActionMap(SettingsScript.gameActionMap);
                    if (MixtapePlayer.QuitToPlayer)
                    {
                        SceneManager.LoadScene(SceneKey.MixtapePlayer.ToString());
                    }
                    else
                    {
                        SceneManager.LoadScene(SceneKey.TitleScreen.ToString());
                    }
                    Manager.lastModified = default;
                    Loader.MoveToActiveScene();
                    Destroy(gameObject);
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        defaultPosition = position;
    }

    private void GenerateText()
    {
        if (cachedPosition != position)
        {
            string[] strings = new string[(int)Options.Count];
            for (var i = 0; i < (int)Options.Count; i++)
            {
                strings[i] = position == i ? $"<color=yellow>{OptionStrings[i]}</color>" : OptionStrings[i];
            }
            this.Text.text = string.Join("\n", strings);
            cachedPosition = position;
        }
    }

    private string SanitizeVersion(string version)
    {
        if (version.Length > versionMaxLength)
        {
            version = version.Substring(0, versionMaxLength - 3) + "...";
        }
        return version;
    }

    private static TMP_FontAsset FindFontOrAny(string name)
    {
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        var fontIndex = Array.FindIndex(fonts, f => f.name == name);
        return fonts[fontIndex == -1 ? 0 : fontIndex];
    }
}
