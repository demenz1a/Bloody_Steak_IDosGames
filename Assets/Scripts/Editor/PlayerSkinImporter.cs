using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Импорт скина игрока: меню Bloody Steak/Import Player Skin (выбрать папку со скином).
///
/// Папка скина — PNG на том же холсте 1812×2595, что и базовые спрайты (Assets/Sprites/PlayerNew),
/// с именами по частям тела (см. Parts): head, chest, chef_head, chef_chest, arm_left, arm_right,
/// chef_arm_left, chef_arm_right, legs, feet, pelvis, tail. Любую часть можно не класть — она
/// останется базовой. chef_arm_* — руки только для формы повара.
///
/// Для каждой части импортёр:
/// 1. копирует настройки импорта соответствующей базовой текстуры (PPU, фильтр, сжатие и т.д.);
/// 2. режет спрайты по тем же прямоугольникам, что и у базовой текстуры, расширяя их, если
///    рисунок скина вылезает за край, и пересчитывая pivot так, чтобы точка крепления к кости
///    осталась в том же месте холста — поэтому скин встаёт на модельку без ручной подгонки;
/// 3. собирает PlayerSkin (таблица "базовый спрайт → спрайт скина") и добавляет его в
///    Assets/Resources/PlayerSkinCatalog.asset.
///
/// Batch: -executeMethod PlayerSkinImporter.ImportHalloweenBatch (импорт + настройка префаба игрока).
/// </summary>
public static class PlayerSkinImporter
{
    private const string BaseFolder = "Assets/Sprites/PlayerNew";
    private const string CatalogPath = "Assets/Resources/PlayerSkinCatalog.asset";
    private const string PlayerPrefabPath = "Assets/Prefabs/Test 1.prefab";

    /// <summary>Мелкие обрезки в базовой текстуре (например head_1) не считаются частью тела.</summary>
    private const float MinSpriteAreaShare = 0.1f;

    private struct Part
    {
        public string Key;
        public string BaseTexture;
        public bool ChefOnly;

        public Part(string key, string baseTexture, bool chefOnly = false)
        {
            Key = key;
            BaseTexture = baseTexture;
            ChefOnly = chefOnly;
        }
    }

    // Имя файла в папке скина → базовая текстура. Осторожно с руками: текстура "LeftHand 1" нарисована
    // слева на холсте (а висит на объекте RightHand), "righthand 1" — справа. Сопоставление идёт по
    // текстуре, а не по имени объекта.
    private static readonly Part[] Parts =
    {
        new Part("head", "head 1.png"),
        new Part("chest", "chest 1.png"),
        new Part("chef_head", "SheepCookHead.png"),
        new Part("chef_chest", "SheepCookChest.png"),
        new Part("arm_left", "LeftHand 1.png"),
        new Part("arm_right", "righthand 1.png"),
        new Part("chef_arm_left", "LeftHand 1.png", chefOnly: true),
        new Part("chef_arm_right", "righthand 1.png", chefOnly: true),
        new Part("legs", "legs 1.png"),
        new Part("feet", "Feets 1.png"),
        new Part("pelvis", "pelvis 1.png"),
        new Part("tail", "Tale 1.png"),
    };

    private static readonly string[] ChefMarkerTextures = { "SheepCookHead.png", "SheepCookChest.png" };

    [MenuItem("Bloody Steak/Import Player Skin")]
    private static void ImportMenu()
    {
        string abs = EditorUtility.OpenFolderPanel("Папка скина", "Assets/Sprites/Skins", "");
        if (string.IsNullOrEmpty(abs)) return;

        string folder = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
        string id = Path.GetFileName(folder).ToLowerInvariant();
        Import(folder, id, Path.GetFileName(folder));
        SetupPlayerPrefab();
    }

    public static void ImportHalloweenBatch()
    {
        Import("Assets/Sprites/Skins/Halloween", "halloween", "Хеллоуинский");
        SetupPlayerPrefab();
    }

    /// <summary>Переимпорт всех папок Assets/Sprites/Skins (id = имя папки в нижнем регистре).</summary>
    public static void ImportAllBatch()
    {
        foreach (var dir in Directory.GetDirectories("Assets/Sprites/Skins"))
        {
            string folder = dir.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            var existing = AssetDatabase.LoadAssetAtPath<PlayerSkin>($"{folder}/{name}Skin.asset");
            string title = existing != null && !string.IsNullOrEmpty(existing.DisplayName) ? existing.DisplayName : name;
            Import(folder, name.ToLowerInvariant(), title);
        }
        SetupPlayerPrefab();
    }

    public static PlayerSkin Import(string folder, string skinId, string displayName)
    {
        var always = new List<PlayerSkin.SpriteSwap>();
        var chef = new List<PlayerSkin.SpriteSwap>();

        foreach (var part in Parts)
        {
            string path = $"{folder}/{part.Key}.png";
            if (!File.Exists(path)) continue;

            var swaps = ImportPart(path, $"{BaseFolder}/{part.BaseTexture}", part.Key);
            (part.ChefOnly ? chef : always).AddRange(swaps);
        }

        string assetPath = $"{folder}/{Path.GetFileName(folder)}Skin.asset";
        var skin = AssetDatabase.LoadAssetAtPath<PlayerSkin>(assetPath);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<PlayerSkin>();
            AssetDatabase.CreateAsset(skin, assetPath);
        }
        skin.EditorSet(skinId, displayName, always.ToArray(), chef.ToArray());
        EditorUtility.SetDirty(skin);

        var catalog = LoadOrCreateCatalog();
        catalog.EditorAdd(skin);
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        Debug.Log($"[PlayerSkinImporter] {skinId}: {always.Count} замен, {chef.Count} замен формы повара.");
        return skin;
    }

    private static List<PlayerSkin.SpriteSwap> ImportPart(string path, string basePath, string key)
    {
        var baseImporter = (TextureImporter)AssetImporter.GetAtPath(basePath);
        // Прямоугольники берутся из данных импорта, а не из Sprite.rect: текстуры холста 1812×2595
        // импортируются с maxTextureSize 2048, и Sprite.rect уже уменьшен, а PNG мы читаем в полном размере.
        var baseRects = ReadSpriteRects(baseImporter);
        float maxArea = baseRects.Max(s => s.rect.width * s.rect.height);
        baseRects = baseRects
            .Where(s => s.rect.width * s.rect.height >= maxArea * MinSpriteAreaShare)
            .OrderBy(s => s.name)
            .ToList();
        var baseByName = AssetDatabase.LoadAllAssetsAtPath(basePath).OfType<Sprite>().ToDictionary(s => s.name);

        // 1. Те же настройки импорта, что у базовой текстуры.
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var settings = new TextureImporterSettings();
        baseImporter.ReadTextureSettings(settings);
        importer.SetTextureSettings(settings);
        importer.SetPlatformTextureSettings(baseImporter.GetDefaultPlatformTextureSettings());
        foreach (var platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
        {
            var ps = baseImporter.GetPlatformTextureSettings(platform);
            if (ps.overridden) importer.SetPlatformTextureSettings(ps);
        }
        importer.SaveAndReimport();

        // 2. Прямоугольники базовых спрайтов, расширенные под рисунок скина, с тем же pivot на холсте.
        var pixels = ReadPixels(path, out int width, out int height);
        var rects = FitRects(baseRects, pixels, width, height);

        var provider = GetProvider(importer);

        var spriteRects = new List<SpriteRect>();
        for (int i = 0; i < baseRects.Count; i++)
        {
            var b = baseRects[i];
            var rect = rects[i];
            Vector2 pivotOnCanvas = b.rect.position + Vector2.Scale(EffectivePivot(b), b.rect.size);
            spriteRects.Add(new SpriteRect
            {
                name = $"{key}_{b.name}",
                rect = rect,
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2((pivotOnCanvas.x - rect.x) / rect.width, (pivotOnCanvas.y - rect.y) / rect.height),
                spriteID = GUID.Generate(),
            });
        }
        provider.SetSpriteRects(spriteRects.ToArray());
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(spriteRects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();

        // 3. Пары "базовый спрайт → спрайт скина".
        var created = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        var swaps = new List<PlayerSkin.SpriteSwap>();
        foreach (var b in baseRects)
        {
            if (baseByName.TryGetValue(b.name, out var original) && created.TryGetValue($"{key}_{b.name}", out var sprite))
                swaps.Add(new PlayerSkin.SpriteSwap { original = original, replacement = sprite });
        }
        return swaps;
    }

    private static ISpriteEditorDataProvider GetProvider(TextureImporter importer)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        return provider;
    }

    private static List<SpriteRect> ReadSpriteRects(TextureImporter importer) =>
        GetProvider(importer).GetSpriteRects().ToList();

    /// <summary>Pivot в долях прямоугольника: у не-Custom выравнивания поле pivot игнорируется Unity.</summary>
    private static Vector2 EffectivePivot(SpriteRect r)
    {
        switch (r.alignment)
        {
            case SpriteAlignment.TopLeft: return new Vector2(0f, 1f);
            case SpriteAlignment.TopCenter: return new Vector2(0.5f, 1f);
            case SpriteAlignment.TopRight: return new Vector2(1f, 1f);
            case SpriteAlignment.LeftCenter: return new Vector2(0f, 0.5f);
            case SpriteAlignment.RightCenter: return new Vector2(1f, 0.5f);
            case SpriteAlignment.BottomLeft: return new Vector2(0f, 0f);
            case SpriteAlignment.BottomCenter: return new Vector2(0.5f, 0f);
            case SpriteAlignment.BottomRight: return new Vector2(1f, 0f);
            case SpriteAlignment.Custom: return r.pivot;
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    private static Color32[] ReadPixels(string path, out int width, out int height)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path)); // строки снизу вверх, как Rect спрайтов
        width = tex.width;
        height = tex.height;
        var pixels = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        return pixels;
    }

    /// <summary>
    /// Каждый непрозрачный пиксель скина относится к ближайшему базовому прямоугольнику
    /// (у ног и ступней их два — левая и правая), и прямоугольник расширяется до его рисунка.
    /// </summary>
    private static Rect[] FitRects(List<SpriteRect> baseSprites, Color32[] pixels, int width, int height)
    {
        int n = baseSprites.Count;
        var min = new Vector2Int[n];
        var max = new Vector2Int[n];
        for (int i = 0; i < n; i++)
        {
            var r = baseSprites[i].rect;
            min[i] = new Vector2Int((int)r.xMin, (int)r.yMin);
            max[i] = new Vector2Int((int)r.xMax, (int)r.yMax);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < 10) continue;

                int best = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    var r = baseSprites[i].rect;
                    float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
                    float dy = Mathf.Max(r.yMin - y, 0f, y - r.yMax);
                    float d = dx * dx + dy * dy;
                    if (d < bestDist) { bestDist = d; best = i; }
                }
                min[best] = Vector2Int.Min(min[best], new Vector2Int(x, y));
                max[best] = Vector2Int.Max(max[best], new Vector2Int(x + 1, y + 1));
            }
        }

        var rects = new Rect[n];
        for (int i = 0; i < n; i++)
        {
            var lo = Vector2Int.Max(min[i], Vector2Int.zero);
            var hi = Vector2Int.Min(max[i], new Vector2Int(width, height));
            rects[i] = new Rect(lo.x, lo.y, hi.x - lo.x, hi.y - lo.y);
        }
        return rects;
    }

    private static PlayerSkinCatalog LoadOrCreateCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PlayerSkinCatalog>(CatalogPath);
        if (catalog != null) return catalog;

        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
        catalog = ScriptableObject.CreateInstance<PlayerSkinCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogPath);
        return catalog;
    }

    /// <summary>Вешает PlayerSkinApplier на корень префаба игрока и задаёт маркеры формы повара.</summary>
    private static void SetupPlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var applier = root.GetComponent<PlayerSkinApplier>() ?? root.AddComponent<PlayerSkinApplier>();
            var markers = ChefMarkerTextures
                .SelectMany(t => AssetDatabase.LoadAllAssetsAtPath($"{BaseFolder}/{t}").OfType<Sprite>())
                .ToArray();

            var so = new SerializedObject(applier);
            var prop = so.FindProperty("chefMarkers");
            prop.arraySize = markers.Length;
            for (int i = 0; i < markers.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
