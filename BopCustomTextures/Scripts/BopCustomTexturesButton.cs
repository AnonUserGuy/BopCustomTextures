using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.IO;
using System.Reflection;

namespace BopCustomTextures.Scripts;
public class BopCustomTexturesButton : MonoBehaviour
{
    public const string MetaButtonsPath = "Canvas/MinigamesMeta/Buttons";
    public const string IconPath = "BopCustomTextures.Resources.icon_bct.png";
    
    public static Texture2D Icon = null;
    private static bool TriedLoadIcon = false;

    public Button Button = null;
    public MixtapeEditorScript Editor;

    public static BopCustomTexturesButton Create(MixtapeEditorScript __instance)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "MixtapeEditor")
        {
            return null;
        }

        var root = GameObject.Find(MetaButtonsPath);
        if (root == null)
        {
            return null;
        }

        var srcObj = root.transform.GetChild(0)?.gameObject;
        if (srcObj == null)
        {
            return null;
        }

        var destObj = Instantiate(srcObj.gameObject, root.transform);
        destObj.name = "BopCustomTexturesButton";

        var bctButton = destObj.AddComponent<BopCustomTexturesButton>();
        bctButton.Editor = __instance;

        return bctButton;
    }

    private void Awake()
    {
        var button = gameObject.GetComponent<Button>();
        if (button != null)
        {
            Button = button;
            Destroy(button);
        }
        
        ApplyIcon();
    }

    private void Update()
    {
        if (Button == null)
        {
            Button = gameObject.AddComponent<Button>();
            Button.onClick.AddListener(OnClick);
        }
    }

    public void OnClick()
    {
        Editor?.OnSelectCategory(MyPluginInfo.PLUGIN_GUID);
    }

    public void UpdateDisplay(bool show, int index)
    {
        gameObject.SetActive(show);
        if (show)
        {
            transform.SetSiblingIndex(index);
        }
    }

    public void ApplyIcon()
    {
        if (!TriedLoadIcon)
        {
            TriedLoadIcon = TryLoadIcon(out Icon);
        }
        if (Icon == null)
        {
            return;
        }

        var image = gameObject.transform.Find("Icon")?.gameObject.GetComponent<Image>();
        if (image == null || image.sprite == null)
        {
            return;
        }
        var original = image.sprite;
        var replacement = Sprite.Create(
            Icon, 
            new Rect(0, 0, Icon.width, Icon.height),
            original.pivot / original.rect.size,
            original.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            original.border
        );
        replacement.name = Icon.name;

        var positionsSlice = original.GetVertexAttribute<Vector3>(VertexAttribute.Position);
        var positions = new NativeArray<Vector3>(positionsSlice.Length, Allocator.Temp);
        positionsSlice.CopyTo(positions);

        var uvsSlice = original.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
        var uvs = new NativeArray<Vector2>(uvsSlice.Length, Allocator.Temp);
        uvsSlice.CopyTo(uvs);

        replacement.SetVertexAttribute(VertexAttribute.Position, positions);
        replacement.SetVertexAttribute(VertexAttribute.TexCoord0, uvs);

        positions.Dispose();
        uvs.Dispose();

        image.sprite = replacement;
    }

    public static bool TryLoadIcon(out Texture2D tex)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream stream = assembly.GetManifestResourceStream(IconPath);
        if (stream == null)
        {
            tex = null;
            return false;
        }
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        byte[] bytes = memoryStream.ToArray();

        tex = new Texture2D(2, 2);
        if (!tex.LoadImage(bytes))
        {
            tex = null;
            return false;
        }
        return true;
    }
}
