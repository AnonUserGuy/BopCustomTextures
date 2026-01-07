using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BopCustomTextures;

public class CustomTextureManagement : CustomManagement
{
    public static readonly Dictionary<SceneKey, Dictionary<int, Texture2D>> CustomAtlasTextures = [];
    public static readonly Dictionary<SceneKey, Dictionary<string, Texture2D>> CustomSeperateTextures = [];
    public static readonly HashSet<SceneKey> CustomSpritesInited = [];
    public static readonly Dictionary<Texture2D, Dictionary<string, Sprite>> SpriteMaps = [];
    public static readonly Dictionary<Texture2D, (SceneKey, int)> TextureMaps = [];
    public static readonly Regex PathRegex = new Regex(@"\\text?u?r?e?s?$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegex = new Regex(@"^text?u?r?e?s?\\(\w+)\\.*?([^\\]*\.(?:png|j(?:pe?g|pe|f?if|fi)))$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegexAtlas = new Regex(@"^sactx-(\d+)", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegexSeperate = new Regex(@"^(\w+)");
    public static readonly Regex SceneAndSpriteAtlasIndexRegex = new Regex(@"^sactx-(\d+)-\d+x\d+-DXT5\|BC3-_(\w+)Atlas");


    public static bool IsCustomTextureDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public static int LocateCustomTextures(string path, string parentPath)
    {
        int filesLoaded = 0;
        var fullFilepaths = Directory.EnumerateFiles(path);
        foreach (var fullFilepath in fullFilepaths)
        {
            var localFilepath = fullFilepath.Substring(parentPath.Length + 1);
            if (CheckIsCustomTexture(fullFilepath, localFilepath))
            {
                filesLoaded++;
            }
        }
        var fullSubpaths = Directory.EnumerateDirectories(path);
        foreach (var fullSubpath in fullSubpaths)
        {
            filesLoaded += LocateCustomTextures(fullSubpath, parentPath);
        }
        return filesLoaded;
    }

    public static bool CheckIsCustomTexture(string path, string localPath)
    {
        Match match = FileRegex.Match(localPath);
        if (match.Success)
        {
            SceneKey scene = ToSceneKeyOrInvalid(match.Groups[1].Value);
            if (scene != SceneKey.Invalid)
            {
                string filename = match.Groups[2].Value;
                Match match2 = FileRegexAtlas.Match(filename);
                if (match2.Success)
                {
                    Plugin.LogFileLoading($"Found custom atlas texture: {scene} ~ {filename}");
                    LoadCustomAtlasTexture(path, localPath, filename, scene, int.Parse(match2.Groups[1].Value));
                    return true;
                }
                Match match3 = FileRegexSeperate.Match(filename);
                if (match3.Success)
                {
                    Plugin.LogFileLoading($"Found custom seperate texture: {scene} ~ {filename}");
                    LoadCustomSeperateTexture(path, localPath, filename, scene, match3.Groups[1].Value);
                    return true;
                }
                
            }
        }
        return false;
    }

    public static void LoadCustomAtlasTexture(string path, string localPath, string filename, SceneKey scene, int spriteAtlasIndex)
    {
        Texture2D tex = LoadImage(path, localPath, filename);
        if (tex == null)
        {
            return;
        }
        if (!CustomAtlasTextures.ContainsKey(scene))
        {
            CustomAtlasTextures[scene] = new Dictionary<int, Texture2D>();
        }
        else if (CustomAtlasTextures[scene].ContainsKey(spriteAtlasIndex))
        {
            Plugin.Logger.LogWarning($"Duplicate atlas texture for {scene}, index {spriteAtlasIndex}");
            Object.Destroy(CustomAtlasTextures[scene][spriteAtlasIndex]);
        }   
        CustomAtlasTextures[scene][spriteAtlasIndex] = tex;
    }

    public static void LoadCustomSeperateTexture(string path, string localPath, string filename, SceneKey scene, string name)
    {
        Texture2D tex = LoadImage(path, localPath, filename);
        if (tex == null)
        {
            return;
        }
        if (!CustomSeperateTextures.ContainsKey(scene))
        {
            CustomSeperateTextures[scene] = new Dictionary<string, Texture2D>();
        }
        else if (CustomSeperateTextures[scene].ContainsKey(name))
        {
            Plugin.Logger.LogWarning($"Duplicate seperate texture for {scene}/{name}");
            Object.Destroy(CustomSeperateTextures[scene][name]);
        }
        CustomSeperateTextures[scene][name] = tex;
    }

    public static Texture2D LoadImage(string path, string localPath, string filename)
    {
        byte[] bytes = ReadFile(path, localPath);
        Texture2D tex = new Texture2D(2, 2);
        bool success = ImageConversion.LoadImage(tex, bytes);
        if (!success)
        {
            Plugin.Logger.LogWarning($"Couldn't load custom texture: {localPath} (is it a PNG/JPG?)");
            Object.Destroy(tex);
            return null;
        }
        tex.name = filename;
        return tex;
    }

    public static void UnloadCustomTextures()
    {
        foreach (var q in SpriteMaps)
        {
            Texture2D tex = q.Key;
            var spriteMap = q.Value;
            Plugin.LogUnloading($"Unloading custom sprites: {tex.name}");
            foreach (var w in spriteMap)
            {
                var sprite = w.Value;
                if (!sprite.packed)
                {
                    Object.Destroy(sprite);
                }   
            }
        }
        SpriteMaps.Clear();
        CustomSpritesInited.Clear();
        foreach (var q in CustomAtlasTextures)
        {
            SceneKey scene = q.Key;
            var textures = q.Value;
            Plugin.LogUnloading($"Unloading custom atlas textures: {scene}");
            foreach (var w in textures)
            {
                Object.Destroy(w.Value);
            }
        }
        CustomAtlasTextures.Clear();
        foreach (var q in CustomSeperateTextures)
        {
            SceneKey scene = q.Key;
            var textures = q.Value;
            Plugin.LogUnloading($"Unloading custom seperate textures: {scene}");
            foreach (var w in textures)
            {
                Object.Destroy(w.Value);
            }
        }
        CustomSeperateTextures.Clear();
        TextureMaps.Clear();
    }

    public static void InitCustomTextures(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        if (!(CustomAtlasTextures.ContainsKey(sceneKey) || CustomSeperateTextures.ContainsKey(sceneKey)))
        {
            return;
        }
        if (!CustomSpritesInited.Contains(sceneKey))
        {
            Plugin.Logger.LogInfo($"Initializing all custom sprites (invoked by {sceneKey})");
            InitAllCustomSprites();
        }
        Plugin.Logger.LogInfo($"Applying custom sprites: {sceneKey}");
        GameObject rootObj = rootObjectsRef(__instance)[sceneKey];
        InitCustomSpritesInGameObject(rootObj, sceneKey);
    }

    public static void InitAllCustomSprites()
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var sprite in sprites)
        {
            CreateCustomSprite(sprite);
        }
    }

    public static void InitCustomSpritesInGameObject(GameObject rootObj, SceneKey scene)
    {
        var spriteRenderers = rootObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach(var spriteRenderer in spriteRenderers)
        {
            ReplaceCustomSprite(spriteRenderer, scene);
            var script = spriteRenderer.gameObject.AddComponent<CustomSpriteScript>();
            script.scene = scene;
        }
    }

    public static void ReplaceCustomSprite(SpriteRenderer spriteRenderer, SceneKey scene)
    {
        if (spriteRenderer.sprite == null)
        {
            return;
        }
        if (SpriteMaps.ContainsKey(spriteRenderer.sprite.texture))
        {
            spriteRenderer.sprite = SpriteMaps[spriteRenderer.sprite.texture][spriteRenderer.sprite.name];
        }
    }
    
    public static void CreateCustomSprite(Sprite original)
    {
        if (!original.packed)
        {
            return;
        }
        var (scene, spriteAtlasIndex) = GetSceneAndSpriteAtlasIndex(original);
        if (scene == SceneKey.Invalid || !(CustomAtlasTextures.ContainsKey(scene) || CustomSeperateTextures.ContainsKey(scene)))
        {
            return;
        }
        CustomSpritesInited.Add(scene);
        if (!SpriteMaps.ContainsKey(original.texture))
        {
            SpriteMaps[original.texture] = new Dictionary<string, Sprite>();
        }
        else if (SpriteMaps[original.texture].ContainsKey(original.name))
        {
            return;
        }
        if (CustomSeperateTextures.ContainsKey(scene) && CustomSeperateTextures[scene].ContainsKey(original.name))
        {
            Plugin.LogSeperateTextureSprites($" - {scene} - seperate - {original.name}");
            Texture2D tex = CustomSeperateTextures[scene][original.name];

            Sprite replacement = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                original.pivot / original.rect.size,
                original.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                original.border
            );
            replacement.name = original.name;
            Rect vertexBox = GetBoundingBox(original.vertices);
            vertexBox = GetWithDimensionsCentered(vertexBox, tex.width / original.pixelsPerUnit, tex.height / original.pixelsPerUnit);
            Vector3[] vertices = [
                new Vector3(vertexBox.xMin, vertexBox.yMax, 0),
                new Vector3(vertexBox.xMax, vertexBox.yMax, 0),
                new Vector3(vertexBox.xMin, vertexBox.yMin, 0),
                new Vector3(vertexBox.xMax, vertexBox.yMin, 0)
            ];
            Vector2[] uvs = [
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0, 0),
                new Vector2(1, 0)
            ];
            NativeArray<Vector3> verticesNative = new NativeArray<Vector3>(vertices, Allocator.Temp);
            NativeArray<Vector2> uvsNative = new NativeArray<Vector2>(uvs, Allocator.Temp);
            SpriteDataAccessExtensions.SetVertexAttribute(replacement, VertexAttribute.Position, verticesNative);
            SpriteDataAccessExtensions.SetVertexAttribute(replacement, VertexAttribute.TexCoord0, uvsNative);
            verticesNative.Dispose();
            uvsNative.Dispose();

            SpriteMaps[original.texture][original.name] = replacement;
        }
        else if (CustomAtlasTextures.ContainsKey(scene) && CustomAtlasTextures[scene].ContainsKey(spriteAtlasIndex))
        {
            // Logs the creation of all atlas sprites because it takes soooo long that it seems like the game crashed
            Plugin.LogAtlasTextureSprites($" - {scene} - atlas - {original.name}");
            Texture2D tex = CustomAtlasTextures[scene][spriteAtlasIndex];
            Sprite replacement = Sprite.Create(
                tex,
                original.rect,
                original.pivot / original.rect.size,
                original.pixelsPerUnit,
                0,
                SpriteMeshType.Tight,
                original.border
            );
            CopySpriteMesh(original, replacement);
            replacement.name = original.name;

            SpriteMaps[original.texture][original.name] = replacement;
        }
        else
        {
            SpriteMaps[original.texture][original.name] = original;
        }
    }

    public static void CopySpriteMesh(Sprite srcSprite, Sprite destSprite)
    {
        int vertexCount = SpriteDataAccessExtensions.GetVertexCount(srcSprite);
        SpriteDataAccessExtensions.SetVertexCount(destSprite, vertexCount);
        CopyVertexAttribute<Vector3>(srcSprite, destSprite, VertexAttribute.Position);
        CopyIndices(srcSprite, destSprite);
        CopyVertexAttribute<Vector2>(srcSprite, destSprite, VertexAttribute.TexCoord0);
    }

    public static void CopyVertexAttribute<T>(Sprite srcSprite, Sprite destSprite, VertexAttribute attribute)
        where T : struct
    {
        NativeSlice<T> src = SpriteDataAccessExtensions.GetVertexAttribute<T>(srcSprite, attribute);
        NativeArray<T> dest = new NativeArray<T>(src.Length, Allocator.Temp);
        src.CopyTo(dest);
        SpriteDataAccessExtensions.SetVertexAttribute(destSprite, attribute, dest);
        dest.Dispose();
    }

    public static void CopyIndices(Sprite srcSprite, Sprite destSprite)
    {
        NativeArray<ushort> src = SpriteDataAccessExtensions.GetIndices(srcSprite);
        NativeArray<ushort> dest = new NativeArray<ushort>(src.Length, Allocator.Temp);
        src.CopyTo(dest);
        SpriteDataAccessExtensions.SetIndices(destSprite, dest);
        dest.Dispose();
    }

    public static (SceneKey, int) GetSceneAndSpriteAtlasIndex(Sprite sprite)
    {
        if (!TextureMaps.ContainsKey(sprite.texture))
        {
            Match match = SceneAndSpriteAtlasIndexRegex.Match(sprite.texture.name);
            if (match.Success)
            {
                var index = int.Parse(match.Groups[1].Value);
                var scene = ToSceneKeyOrInvalid(match.Groups[2].Value);
                TextureMaps[sprite.texture] = (scene, index);
            } 
            else
            {
                TextureMaps[sprite.texture] = (SceneKey.Invalid, -1);
            }
        }
        return TextureMaps[sprite.texture];
    }

    public static void PrintAllSpriteInfo(Sprite sprite)
    {
        Plugin.Logger.LogInfo(sprite.name);
        Plugin.Logger.LogInfo($" - rect: {sprite.rect}");
        Plugin.Logger.LogInfo($" - pivot: {sprite.pivot}");
        Plugin.Logger.LogInfo($" - pixelsPerUnit: {sprite.pixelsPerUnit}");
        Plugin.Logger.LogInfo($" - border: {sprite.border}");
        Plugin.Logger.LogInfo($" - packingMode: {sprite.packingMode}");
        Plugin.Logger.LogInfo($" - packingRotation: {sprite.packingRotation}");
        if (sprite.packingMode != SpritePackingMode.Tight)
        {
            Plugin.Logger.LogInfo($" - textureRect: {sprite.textureRect}");
        }
        Plugin.Logger.LogInfo($" - textureRectOffset: {sprite.textureRectOffset}");

        Plugin.Logger.LogInfo($" - vertices ({sprite.vertices.Length}):");
        for (int i = 0; i < sprite.vertices.Length; i++)
        {
            Plugin.Logger.LogInfo($" - - {sprite.vertices[i]}");
        }
        Plugin.Logger.LogInfo($" - uv ({sprite.uv.Length}):");
        for (int i = 0; i < sprite.uv.Length; i++)
        {
            Plugin.Logger.LogInfo($" - - {sprite.uv[i]}");
        }
        Plugin.Logger.LogInfo($" - triangles ({sprite.triangles.Length}):");
        for (int i = 0; i < sprite.triangles.Length; i++)
        {
            Plugin.Logger.LogInfo($" - - {sprite.triangles[i]}");
        }
    }

    public static Rect GetBoundingBox(Vector2[] vertices)
    {
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        foreach (var vertex in vertices)
        {
            if (vertex.x < minX)
            {
                minX = vertex.x;
            }
            if (vertex.x > maxX)
            {
                maxX = vertex.x;
            }
            if (vertex.y < minY)
            {
                minY = vertex.y;
            }
            if (vertex.y > maxY)
            {
                maxY = vertex.y;
            }
        }
        return new Rect(minX, minY, (maxX - minX), (maxY - minY));
    }

    public static Rect GetWithDimensionsCentered(Rect rect, float width, float height)
    {
        return new Rect(rect.x + (rect.width - width) / 2, rect.y + (rect.height - height) / 2, width, height);
    }
}
