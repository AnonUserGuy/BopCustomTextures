using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BopCustomTextures.Scripts;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages custom textures, including loading them from source files and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger</param>
public class CustomTextureManager(ILogger logger) : BaseCustomManager(logger)
{
    public readonly Dictionary<SceneKey, Dictionary<int, Texture2D>> CustomAtlasTextures = [];
    public readonly Dictionary<SceneKey, Dictionary<string, Texture2D>> CustomSeperateTextures = [];
    public readonly Dictionary<SceneKey, Dictionary<Texture2D, string>> CustomSeperateTexturesNotInited = [];
    public readonly HashSet<SceneKey> CustomSpritesInited = [];
    public readonly Dictionary<Texture2D, Dictionary<string, Sprite>> SpriteMaps = [];
    public readonly HashSet<Sprite> CustomSprites = [];
    public readonly Dictionary<Texture2D, (SceneKey, int)> TextureMaps = [];
    public static readonly Regex PathRegex = new Regex(@"[\\/]text?u?r?e?s?$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegex = new Regex(@"^text?u?r?e?s?[\\/](\w+)[\\/].*?([^\\/]*\.(?:png|j(?:pe?g|pe|f?if|fi)))$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegexAtlas = new Regex(@"^sactx-(\d+)", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegexSeperate = new Regex(@"^(\w+)");
    public static readonly Regex SceneAndSpriteAtlasIndexRegex = new Regex(@"^sactx-(\d+)-\d+x\d+-DXT5\|BC3-_(\w+)Atlas");

    public static bool IsCustomTextureDirectory(string path)
    {
        return PathRegex.IsMatch(path);
    }

    public int LocateCustomTextures(string path, string parentPath)
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

    public bool CheckIsCustomTexture(string path, string localPath)
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
                    logger.LogFileLoading($"Found custom atlas texture: {scene} ~ {filename}");
                    LoadCustomAtlasTexture(path, localPath, filename, scene, int.Parse(match2.Groups[1].Value));
                    return true;
                }
                Match match3 = FileRegexSeperate.Match(filename);
                if (match3.Success)
                {
                    logger.LogFileLoading($"Found custom seperate texture: {scene} ~ {filename}");
                    LoadCustomSeperateTexture(path, localPath, filename, scene, match3.Groups[1].Value);
                    return true;
                }
            }
        }
        return false;
    }

    public void LoadCustomAtlasTexture(string path, string localPath, string filename, SceneKey scene, int spriteAtlasIndex)
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
            logger.LogWarning($"Duplicate atlas texture for {scene}, index {spriteAtlasIndex}");
            Object.Destroy(CustomAtlasTextures[scene][spriteAtlasIndex]);
        }
        CustomAtlasTextures[scene][spriteAtlasIndex] = tex;
    }

    public void LoadCustomSeperateTexture(string path, string localPath, string filename, SceneKey scene, string name)
    {
        Texture2D tex = LoadImage(path, localPath, filename);
        if (tex == null)
        {
            return;
        }
        if (!CustomSeperateTextures.ContainsKey(scene))
        {
            CustomSeperateTextures[scene] = new Dictionary<string, Texture2D>();
            CustomSeperateTexturesNotInited[scene] = new Dictionary<Texture2D, string>();
        }
        else if (CustomSeperateTextures[scene].ContainsKey(name))
        {
            logger.LogWarning($"Duplicate seperate texture for {scene}/{name}");
            Object.Destroy(CustomSeperateTextures[scene][name]);
        }
        CustomSeperateTextures[scene][name] = tex;
        CustomSeperateTexturesNotInited[scene][tex] = name;
    }

    public Texture2D LoadImage(string path, string localPath, string filename)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (!tex.LoadImage(bytes))
        {
            logger.LogWarning($"Couldn't load custom texture: {localPath} (is it a PNG/JPG?)");
            Object.Destroy(tex);
            return null;
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = filename;
        return tex;
    }

    public void UnloadCustomTextures()
    {
        foreach (var q in SpriteMaps)
        {
            Texture2D tex = q.Key;
            var spriteMap = q.Value;
            logger.LogUnloading($"Unloading custom sprites: {tex.name}");
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
        CustomSprites.Clear();
        CustomSpritesInited.Clear();
        foreach (var q in CustomAtlasTextures)
        {
            SceneKey scene = q.Key;
            var textures = q.Value;
            logger.LogUnloading($"Unloading custom atlas textures: {scene}");
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
            logger.LogUnloading($"Unloading custom seperate textures: {scene}");
            foreach (var w in textures)
            {
                Object.Destroy(w.Value);
            }
        }
        CustomSeperateTextures.Clear();
        CustomSeperateTexturesNotInited.Clear();
        TextureMaps.Clear();
    }

    public void InitCustomTextures(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        if (!(CustomAtlasTextures.ContainsKey(sceneKey) || CustomSeperateTextures.ContainsKey(sceneKey)))
        {
            return;
        }
        if (!CustomSpritesInited.Contains(sceneKey))
        {
            CustomSpritesInited.Add(sceneKey);
            logger.LogInfo($"Initializing all custom sprites (invoked by {sceneKey})"); 
            InitCustomSprites();
        }
        logger.LogInfo($"Applying custom sprites: {sceneKey}");
        GameObject rootObj = rootObjectsRef(__instance)[sceneKey];
        InitCustomSpriteRenderers(rootObj, sceneKey);
    }

    public void InitCustomSprites()
    {
        // No way to obtain sprites only used by a certain scene, so all sprites have to be iterated through
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        HashSet<Sprite> nonPackedSprites = [];
        foreach (var sprite in sprites)
        {
            if (!CreateCustomSprite(sprite))
            {
                nonPackedSprites.Add(sprite);
            }
        }

        // CreateCustomSprites only covers base sprites included in a sprite atlas.
        // for base sprites not included in a sprite atlas (like a handful from molecano), the following will fix
        List<SceneKey> toRemove = [];
        foreach (var scenePair in CustomSeperateTexturesNotInited)
        {
            var scene = scenePair.Key;
            if (!CustomSpritesInited.Contains(scene))
            {
                continue;
            }
            toRemove.Add(scene);
            foreach (var texPair in scenePair.Value)
            {
                Texture2D tex = texPair.Key;
                Sprite ogSprite = null;
                foreach (var sprite in nonPackedSprites)
                {
                    if (sprite.texture.name == texPair.Value)
                    {
                        SpriteMaps[sprite.texture] = new Dictionary<string, Sprite>();
                        CreateCustomSeperateSprite(sprite, tex, scene);

                        ogSprite = sprite;
                        break;
                    }
                }
                if (ogSprite == null)
                {
                    logger.LogWarning($"Found seperate texture that doesn't correspond to any game texture: {scene} ~ {tex.name}");
                }
                else
                {
                    nonPackedSprites.Remove(ogSprite);
                }
            }
        }
        foreach (var scene in toRemove)
        {
            CustomSeperateTexturesNotInited.Remove(scene);
        }
    }

    public void InitCustomSpriteRenderers(GameObject rootObj, SceneKey scene)
    {
        var spriteRenderers = rootObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var spriteRenderer in spriteRenderers)
        {
            ReplaceCustomSprite(spriteRenderer, scene);
            CustomSpriteSwapper script = spriteRenderer.gameObject.AddComponent<CustomSpriteSwapper>();
            script.last = spriteRenderer.sprite; // doing this in Awake() is insufficient
            script.scene = scene;
            script.textureManager = this;
        }
    }

    public void ReplaceCustomSprite(SpriteRenderer spriteRenderer, SceneKey scene)
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

    public bool CreateCustomSprite(Sprite original)
    {
        if (!original.packed)
        {
            if (!CustomSprites.Contains(original))
            {
                // sprite may need replacing by step 2 of InitCustomSprites
                return false;
            } 
            else
            {
                return true;
            }
        }
        var (scene, spriteAtlasIndex) = GetSceneAndSpriteAtlasIndex(original);
        if (scene == SceneKey.Invalid || !(CustomAtlasTextures.ContainsKey(scene) || CustomSeperateTextures.ContainsKey(scene)))
        {
            return true;
        }
        CustomSpritesInited.Add(scene);
        if (!SpriteMaps.ContainsKey(original.texture))
        {
            SpriteMaps[original.texture] = new Dictionary<string, Sprite>();
        }
        else if (SpriteMaps[original.texture].ContainsKey(original.name))
        {
            return true;
        }
        if (CustomSeperateTextures.ContainsKey(scene) && CustomSeperateTextures[scene].ContainsKey(original.name))
        {
            Texture2D tex = CustomSeperateTextures[scene][original.name];
            CreateCustomSeperateSprite(original, tex, scene);
            CustomSeperateTexturesNotInited[scene].Remove(tex);
        }
        else if (CustomAtlasTextures.ContainsKey(scene) && CustomAtlasTextures[scene].ContainsKey(spriteAtlasIndex))
        {
            Texture2D tex = CustomAtlasTextures[scene][spriteAtlasIndex];
            CreateCustomAtlasSprite(original, tex, scene);
        }
        else
        {
            SpriteMaps[original.texture][original.name] = original;
        }
        return true;
    }

    public void CreateCustomSeperateSprite(Sprite original, Texture2D tex, SceneKey scene)
    {
        logger.LogSeperateTextureSprites($" - {scene} - seperate - {original.name}");

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

        // create new sprite with bounds adjusted if new texture larger than old texture
        Rect vertexBox = GetBoundingBox(original.vertices);
        vertexBox = GetWithDimensionsCentered(vertexBox, tex.width / original.pixelsPerUnit, tex.height / original.pixelsPerUnit);

        // apply new sprites bounds
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
        replacement.SetVertexAttribute(VertexAttribute.Position, verticesNative);
        replacement.SetVertexAttribute(VertexAttribute.TexCoord0, uvsNative);
        verticesNative.Dispose();
        uvsNative.Dispose();

        SpriteMaps[original.texture][original.name] = replacement;
        CustomSprites.Add(replacement);
    }

    public void CreateCustomAtlasSprite(Sprite original, Texture2D tex, SceneKey scene)
    {
        logger.LogAtlasTextureSprites($" - {scene} - atlas - {original.name}");

        Sprite replacement = Sprite.Create(
            tex,
            original.rect,
            original.pivot / original.rect.size,
            original.pixelsPerUnit,
            0,
            SpriteMeshType.Tight,
            original.border
        );
        CopySpriteMesh(original, replacement); // Source of huge load time for custom atlas textures, should (somehow) be optimized
        replacement.name = original.name;

        SpriteMaps[original.texture][original.name] = replacement;
        CustomSprites.Add(replacement);
    }

    public static void CopySpriteMesh(Sprite srcSprite, Sprite destSprite)
    {
        int vertexCount = srcSprite.GetVertexCount();
        destSprite.SetVertexCount(vertexCount);
        CopyVertexAttribute<Vector3>(srcSprite, destSprite, VertexAttribute.Position); // can this be done with OverrideGeometry? Would that help?
        CopyIndices(srcSprite, destSprite); // ditto this
        CopyVertexAttribute<Vector2>(srcSprite, destSprite, VertexAttribute.TexCoord0); // unavoidable
    }

    public static void CopyVertexAttribute<T>(Sprite srcSprite, Sprite destSprite, VertexAttribute attribute)
        where T : struct
    {
        NativeSlice<T> src = srcSprite.GetVertexAttribute<T>(attribute);
        NativeArray<T> dest = new NativeArray<T>(src.Length, Allocator.Temp);
        src.CopyTo(dest);
        destSprite.SetVertexAttribute(attribute, dest);
        dest.Dispose();
    }

    public static void CopyIndices(Sprite srcSprite, Sprite destSprite)
    {
        NativeArray<ushort> src = srcSprite.GetIndices();
        NativeArray<ushort> dest = new NativeArray<ushort>(src.Length, Allocator.Temp);
        src.CopyTo(dest);
        destSprite.SetIndices(dest);
        dest.Dispose();
    }

    public (SceneKey, int) GetSceneAndSpriteAtlasIndex(Sprite sprite)
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
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public static Rect GetWithDimensionsCentered(Rect rect, float width, float height)
    {
        return new Rect(rect.x + (rect.width - width) / 2, rect.y + (rect.height - height) / 2, width, height);
    }
}
