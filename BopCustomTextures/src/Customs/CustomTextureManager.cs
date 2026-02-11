using BopCustomTextures.Scripts;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Rendering;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

/// <summary>
/// Manages custom textures, including loading them from source files and applying them when the mixtape is played.
/// </summary>
/// <param name="logger">Plugin-specific logger.</param>
/// <param name="variantManager">Used for mapping custom texture variant external names to internal indices. Shared with CustomJsonInitializer.</param>
/// <param name="mixtapeEventTemplates">BopCustomTexture mixtape event templates concerning custom textures.</param>
public class CustomTextureManager(ILogger logger, CustomVariantNameManager variantManager, MixtapeEventTemplate[] mixtapeEventTemplates) : BaseCustomManager(logger)
{
    /// <summary>
    /// BopCustomTexture mixtape event templates concerning custom textures. 
    /// Updated to only include scenes with custom textures as options in their "scene" parameters.
    /// </summary>
    public MixtapeEventTemplate[] mixtapeEventTemplates = mixtapeEventTemplates;

    /// <summary>
    /// Img files for altas textures.
    /// </summary>
    public readonly Dictionary<SceneKey, Dictionary<int, Dictionary<int, Texture2D>>> AtlasTextures = [];
    
    /// <summary>
    /// Img files for seperate textures.
    /// </summary>
    public readonly Dictionary<SceneKey, Dictionary<string, Dictionary<int, Texture2D>>> SeperateTextures = [];

    /// <summary>
    /// List of all seperate textures that haven't had sprite generated for them.
    /// Necessary as some sprites aren't atlas packed and thus cannot have their game identified from them.
    /// Said sprites are covered by iterating through this list and finding matches by name.
    /// </summary>
    public readonly Dictionary<SceneKey, Dictionary<Texture2D, (string, int)>> SeperateTexturesNotInited = [];

    /// <summary>
    /// List of games that have been loaded at some point, and thus have generated custom their sprites.
    /// </summary>
    public readonly HashSet<SceneKey> SpritesInited = [];

    /// <summary>
    /// Mapping of vanilla sprites to custom sprites. Maps by 'Original Texture Object' -> 'Original Sprite Name' 
    /// because just mapping just by Original Sprite object didn't work when I tried it.
    /// </summary>
    public readonly Dictionary<Texture2D, Dictionary<string, Dictionary<int, Sprite>>> SpriteMaps = [];

    /// <summary>
    /// List of all custom sprites. Necessary to avoid generating custom sprites based on them.
    /// </summary>
    public readonly HashSet<Sprite> CustomSprites = [];

    /// <summary>
    /// Maps a vanilla texture to its scenekey and sprite atlas index if a sprite atlas texture.
    /// For sprite atlas textures this can be solely determined by name, but otherwise it has to be determined via a name matching search.
    /// </summary>
    public readonly Dictionary<Texture2D, (SceneKey, int)> TextureMaps = [];

    /// <summary>
    /// List of variants active per scene. Kind of like a layer system: gameobjects will use sprites from the last variant in 
    /// the list unless said variant doesn't have a sprite, iterating backwards until a sprite can be found.
    /// If one is never found, the vanilla sprite is used.
    /// </summary>
    public readonly Dictionary<SceneKey, List<int>> Variants = [];

    /// <summary>
    /// Used to map external custom texture variant names to their internal indices. Shared with CustomJsonInitializer.
    /// </summary>
    public CustomVariantNameManager VariantManager = variantManager;

    public static readonly Regex PathRegex = new Regex(@"[\\/]text?u?r?e?s?$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegex = new Regex(@"^text?u?r?e?s?[\\/](\w+)[^\w\\/]*(\w+)?[\\/].*?([^\\/]*\.(?:png|j(?:pe?g|pe|f?if|fi)))$", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegexAtlas = new Regex(@"^sactx-(\d+)", RegexOptions.IgnoreCase);
    public static readonly Regex FileRegexSeperate = new Regex(@"^(\w+)");
    public static readonly Regex SceneAndSpriteAtlasIndexRegex = new Regex(@"^sactx-(\d+)-\d+x\d+-DXT5\|BC3-_(\w+)Atlas");
    public static readonly Regex MixtapeEventRegex = new Regex(@"^(\w*)/(\w*) texture variant$");

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
                int variant = VariantManager.GetOrAddVariant(scene, match.Groups[2].Value);
                string filename = match.Groups[3].Value;
                Match match2 = FileRegexAtlas.Match(filename);
                if (match2.Success)
                {
                    logger.LogFileLoading($"Found custom atlas texture: {scene} ~ {filename}");
                    LoadCustomAtlasTexture(path, localPath, filename, scene, int.Parse(match2.Groups[1].Value), variant);
                    return true;
                }
                Match match3 = FileRegexSeperate.Match(filename);
                if (match3.Success)
                {
                    logger.LogFileLoading($"Found custom seperate texture: {scene} ~ {filename}");
                    LoadCustomSeperateTexture(path, localPath, filename, scene, match3.Groups[1].Value, variant);
                    return true;
                }
            }
        }
        return false;
    }

    public void LoadCustomAtlasTexture(string path, string localPath, string filename, SceneKey scene, int index, int variant)
    {
        Texture2D tex = LoadImage(path, localPath, filename);
        if (tex == null)
        {
            return;
        }
        if (!AtlasTextures.ContainsKey(scene))
        {
            AtlasTextures[scene] = [];
        }
        if (!AtlasTextures[scene].ContainsKey(index))
        {
            AtlasTextures[scene][index] = [];
        }
        else if (AtlasTextures[scene][index].ContainsKey(variant))
        {
            logger.LogWarning($"Duplicate atlas texture for {scene}, index {index}");
            Object.Destroy(AtlasTextures[scene][index][variant]);
        }
        AtlasTextures[scene][index][variant] = tex;
    }

    public void LoadCustomSeperateTexture(string path, string localPath, string filename, SceneKey scene, string name, int variant)
    {
        Texture2D tex = LoadImage(path, localPath, filename);
        if (tex == null)
        {
            return;
        }
        if (!SeperateTextures.ContainsKey(scene))
        {
            SeperateTextures[scene] = [];
            SeperateTexturesNotInited[scene] = [];
        }
        if (!SeperateTextures[scene].ContainsKey(name))
        {
            SeperateTextures[scene][name] = [];
        }
        else if (SeperateTextures[scene][name].ContainsKey(variant))
        {
            logger.LogWarning($"Duplicate seperate texture for {scene} ~ {name}");
            Object.Destroy(SeperateTextures[scene][name][variant]);
        }
        SeperateTextures[scene][name][variant] = tex;
        SeperateTexturesNotInited[scene][tex] = (name, variant);
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
                foreach (var e in w.Value)
                {
                    var sprite = e.Value;
                    if (!sprite.packed)
                    {
                        Object.Destroy(sprite);
                    }
                }
            }
        }
        SpriteMaps.Clear();
        CustomSprites.Clear();
        SpritesInited.Clear();
        foreach (var q in AtlasTextures)
        {
            SceneKey scene = q.Key;
            var textures = q.Value;
            logger.LogUnloading($"Unloading custom atlas textures: {scene}");
            foreach (var w in textures)
            {
                foreach (var e in w.Value)
                {
                    Object.Destroy(e.Value);
                }
            }
        }
        AtlasTextures.Clear();
        foreach (var q in SeperateTextures)
        {
            SceneKey scene = q.Key;
            var textures = q.Value;
            logger.LogUnloading($"Unloading custom seperate textures: {scene}");
            foreach (var w in textures)
            {
                foreach (var e in w.Value)
                {
                    Object.Destroy(e.Value);
                }
            }
        }
        SeperateTextures.Clear();
        SeperateTexturesNotInited.Clear();
        TextureMaps.Clear();
    }

    public void InitCustomTextures(MixtapeLoaderCustom __instance, SceneKey sceneKey)
    {
        if (!(AtlasTextures.ContainsKey(sceneKey) || SeperateTextures.ContainsKey(sceneKey)))
        {
            return;
        }
        if (!SpritesInited.Contains(sceneKey))
        {
            SpritesInited.Add(sceneKey);
            logger.LogInfo($"Initializing all custom sprites (invoked by {sceneKey})");
            InitCustomSprites();
        }
        logger.LogInfo($"Applying custom sprites: {sceneKey}");
        Variants[sceneKey] = [0];
        GameObject rootObj = rootObjectsRef(__instance)[sceneKey];
        InitCustomSpriteRenderers(rootObj, sceneKey);
    }

    public void InitCustomSpriteRenderers(GameObject rootObj, SceneKey scene)
    {
        var spriteRenderers = rootObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var spriteRenderer in spriteRenderers)
        {
            CustomSpriteSwapper script = spriteRenderer.gameObject.AddComponent<CustomSpriteSwapper>();
            script.LastVanilla = spriteRenderer.sprite;
            spriteRenderer.sprite = ReplaceCustomSprite(script.LastVanilla);
            script.Last = spriteRenderer.sprite; // doing anything in Awake() is insufficient
            script.SpriteRenderer = spriteRenderer;
            script.TextureManager = this;
        }
    }

    public void PrepareEvents(MixtapeLoaderCustom __instance, Entity[] entities)
    {
        var sceneSpriteSwappers = new Dictionary<SceneKey, CustomSpriteSwapper[]>();
        foreach (Entity entity in entities)
        {
            if (!entity.dataModel.StartsWith(MyPluginInfo.PLUGIN_GUID))
            {
                continue;
            }
            var match = MixtapeEventRegex.Match(entity.dataModel);
            if (!match.Success)
            {
                continue;
            }
            byte operation = 0;
            if (match.Groups[2].Value == "remove")
            {
                operation = 1;
            }
            else if (match.Groups[2].Value == "set")
            {
                operation = 2;
            }
            else if (match.Groups[2].Value != "add")
            {
                continue;
            }

            var sceneStr = entity.GetString("scene");
            var scene = ToSceneKeyOrInvalid(sceneStr);
            if (scene == SceneKey.Invalid)
            {
                logger.LogError($"Scene \"{sceneStr}\" is not a valid scene key");
                continue;
            }
            if (!sceneSpriteSwappers.TryGetValue(scene, out var spriteSwappers))
            {
                if (!rootObjectsRef(__instance).TryGetValue(scene, out var rootObj))
                {
                    logger.LogError($"Cannot apply texture variant to missing scene {scene}");
                    continue;
                }
                spriteSwappers = rootObj.GetComponentsInChildren<CustomSpriteSwapper>(true);
                sceneSpriteSwappers[scene] = spriteSwappers;
            }

            var variantStrings = entity.GetString("variant").Split([',']);
            var variants = new List<int>();
            foreach (var strWhitespace in variantStrings)
            {
                var str = strWhitespace.Trim();
                if (!VariantManager.TryGetVariant(scene, str, out int variant))
                {
                    continue;
                }
                if (variants.Contains(variant))
                {
                    logger.LogWarning($"Variant \"{str}\" for scene {scene} is listed multiple times");
                    continue;
                }
                variants.Add(variant);
            }
            var destVariants = Variants[scene];

            switch (operation)
            {
                case 0:
                    __instance.scheduler.Schedule(entity.beat, delegate
                    {
                        foreach (var variant in variants)
                        {
                            destVariants.Remove(variant);
                            destVariants.Add(variant);
                        }
                        foreach (var spriteSwapper in spriteSwappers)
                        {
                            spriteSwapper.ReplaceCustomSprites();
                        }
                    });
                    break;
                case 1:
                    __instance.scheduler.Schedule(entity.beat, delegate
                    {
                        foreach (var variant in variants)
                        {
                            destVariants.Remove(variant);
                        }
                        foreach (var spriteSwapper in spriteSwappers)
                        {
                            spriteSwapper.ReplaceCustomSprites();
                        }
                    });
                    break;
                case 2: 
                    __instance.scheduler.Schedule(entity.beat, delegate
                    {
                        Variants[scene] = variants;
                        foreach (var spriteSwapper in spriteSwappers)
                        {
                            spriteSwapper.ReplaceCustomSprites();
                        }
                    });
                    break;
            }
        }
    }
    public bool UpdateEventTemplates()
    {
        var hasCustomTextures = AtlasTextures.Keys.ToHashSet().Union(SeperateTextures.Keys.ToHashSet());
        object scenes;
        bool result;
        if (hasCustomTextures.Count() < 1)
        {
            scenes = "";
            result = false;
        }
        else
        {
            scenes = new MixtapeEventTemplates.ChoiceField<string>(
                hasCustomTextures.Select(FromSceneKeyOrInvalid).ToArray());
            result = true;
        }
        foreach (var mixtapeEventTemplate in mixtapeEventTemplates)
        {
            mixtapeEventTemplate.properties["scene"] = scenes;
        }
        return result;
    }

    public Sprite ReplaceCustomSprite(Sprite sprite)
    {
        if (sprite == null ||
            !SpriteMaps.TryGetValue(sprite.texture, out var spriteMaps2) ||
            !spriteMaps2.TryGetValue(sprite.name, out var sprites) ||
            !TextureMaps.TryGetValue(sprite.texture, out var sceneTuple))
        {
            return sprite;
        }
        var (scene, _) = sceneTuple;
        var variants = Variants[scene];
        for (int i = variants.Count - 1; i >= 0; i--)
        {
            if (sprites.TryGetValue(variants[i], out var sprite2))
            {
                return sprite2;
            }
        }
        return sprite;
    }
    public Sprite ReplaceCustomSprite(Sprite sprite, List<int> localVariants)
    {
        if (sprite == null ||
            !SpriteMaps.TryGetValue(sprite.texture, out var spriteMaps2) ||
            !spriteMaps2.TryGetValue(sprite.name, out var sprites) ||
            !TextureMaps.TryGetValue(sprite.texture, out var sceneTuple)) 
        {
            return sprite;
        }
        var (scene, _) = sceneTuple;
        for (int i = localVariants.Count - 1; i >= 0; i--)
        {
            if (sprites.TryGetValue(localVariants[i], out var sprite2))
            {
                return sprite2;
            }
        }
        var variants = Variants[scene];
        for (int i = variants.Count - 1; i >= 0; i--)
        {
            if (sprites.TryGetValue(variants[i], out var sprite2))
            {
                return sprite2;
            }
        }
        return sprite;
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
        foreach (var scenePair in SeperateTexturesNotInited)
        {
            var scene = scenePair.Key;
            if (!SpritesInited.Contains(scene))
            {
                continue;
            }
            toRemove.Add(scene);
            foreach (var texPair in scenePair.Value)
            {
                Texture2D tex = texPair.Key;
                var (name, variant) = texPair.Value;
                Sprite ogSprite = null;
                foreach (var sprite in nonPackedSprites)
                {
                    if (sprite.texture.name == name)
                    {
                        if (!SpriteMaps.ContainsKey(sprite.texture))
                        {
                            SpriteMaps[sprite.texture] = [];
                        }
                        if (!SpriteMaps[sprite.texture].ContainsKey(sprite.name))
                        {
                            SpriteMaps[sprite.texture][sprite.name] = [];
                        }
                        CreateCustomSeperateSprite(sprite, tex, variant);
                        TextureMaps[sprite.texture] = (scene, -1);
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
            SeperateTexturesNotInited.Remove(scene);
        }
    }

    public bool CreateCustomSprite(Sprite original)
    {
        if (!original.packed)
        {
            if (!CustomSprites.Contains(original))
            {
                // is a nonpacked, vanilla sprite
                // sprite may need replacing by step 2 of InitCustomSprites
                return false;
            } 
            else
            {
                // is a custom sprite
                return true;
            }
        }
        var (scene, spriteAtlasIndex) = GetSceneAndSpriteAtlasIndex(original);
        if (scene == SceneKey.Invalid || !(AtlasTextures.ContainsKey(scene) || SeperateTextures.ContainsKey(scene)))
        {
            // is some sort of menu sprite
            return true;
        }
        SpritesInited.Add(scene);
        if (!SpriteMaps.ContainsKey(original.texture))
        {
            SpriteMaps[original.texture] = [];
        }
        else if (SpriteMaps[original.texture].ContainsKey(original.name))
        {
            // is a vanilla sprite we've already inited
            return true;
        }
        SpriteMaps[original.texture][original.name] = [];
        if (SeperateTextures.ContainsKey(scene) && SeperateTextures[scene].ContainsKey(original.name))
        {
            logger.LogSeperateTextureSprites($" - {scene} - seperate - {original.name}");
            var texs = SeperateTextures[scene][original.name];
            foreach (var tex in texs)
            {
                CreateCustomSeperateSprite(original, tex.Value, tex.Key);
                SeperateTexturesNotInited[scene].Remove(tex.Value);
            }
        }
        if (AtlasTextures.ContainsKey(scene) && AtlasTextures[scene].ContainsKey(spriteAtlasIndex))
        {
            logger.LogAtlasTextureSprites($" - {scene} - atlas - {original.name}");
            var texs = AtlasTextures[scene][spriteAtlasIndex];
            foreach (var tex in texs)
            {
                CreateCustomAtlasSprite(original, tex.Value, tex.Key);
            }
        }
        return true;
    }

    public void CreateCustomSeperateSprite(Sprite original, Texture2D tex, int variant)
    {
        if (SpriteMaps[original.texture][original.name].ContainsKey(variant))
        {
            return;
        }
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

        CustomSprites.Add(replacement);
        SpriteMaps[original.texture][original.name][variant] = replacement; 
    }

    public void CreateCustomAtlasSprite(Sprite original, Texture2D tex, int variant)
    {
        if (SpriteMaps[original.texture][original.name].ContainsKey(variant))
        {
            return;
        }
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
        
        CustomSprites.Add(replacement);
        SpriteMaps[original.texture][original.name][variant] = replacement;
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
