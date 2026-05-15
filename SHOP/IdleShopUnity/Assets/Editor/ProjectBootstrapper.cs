using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectBootstrapper
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string ApkPath = "Builds/Android/PocketShop-debug.apk";
    private const string LocalizationPath = "Assets/Resources/Localization";
    private const string ArtPath = "Assets/Art";

    [Serializable]
    private sealed class LocalizationEntry
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    [Serializable]
    private sealed class LocalizationTable
    {
        public string languageCode = string.Empty;
        public string languageName = string.Empty;
        public LocalizationEntry[] entries = Array.Empty<LocalizationEntry>();
    }

    [MenuItem("Pocket Shop/Setup Project")]
    public static void SetupProject()
    {
        ValidateLocalization();

        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Builds/Android");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().backgroundColor = new Color(0.95f, 0.89f, 0.79f);
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().orthographic = true;

        var game = new GameObject("IdleShopGame", typeof(IdleShopGame));
        game.transform.position = Vector3.zero;
        game.GetComponent<IdleShopGame>().ConfigureArt(
            LoadSprite($"{ArtPath}/Shop/shop_front.png"),
            LoadSprite($"{ArtPath}/Shop/shelf.png"),
            LoadSprite($"{ArtPath}/Shop/counter.png"),
            LoadSprite($"{ArtPath}/UI/coin.png"),
            LoadSprite($"{ArtPath}/UI/reputation.png"),
            LoadSprite($"{ArtPath}/UI/customer.png"),
            LoadSprite($"{ArtPath}/UI/upgrade.png"),
            new[]
            {
                LoadSprite($"{ArtPath}/Products/rice_ball.png"),
                LoadSprite($"{ArtPath}/Products/sparkling_water.png"),
                LoadSprite($"{ArtPath}/Products/bread.png"),
                LoadSprite($"{ArtPath}/Products/coffee.png"),
                LoadSprite($"{ArtPath}/Products/lunch_box.png"),
                LoadSprite($"{ArtPath}/Products/dessert.png"),
                LoadSprite($"{ArtPath}/Products/flower.png"),
                LoadSprite($"{ArtPath}/Products/gift_box.png")
            },
            new[]
            {
                LoadFirstSprite($"{ArtPath}/Staff/kobayashi_cashier.png", $"{ArtPath}/Staff/cashier.png"),
                LoadFirstSprite($"{ArtPath}/Staff/misaki_stocker.png", $"{ArtPath}/Staff/stocker.png"),
                LoadFirstSprite($"{ArtPath}/Staff/aken_promoter.png", $"{ArtPath}/Staff/promoter.png"),
                LoadSprite($"{ArtPath}/Staff/lina_merchandiser.png"),
                LoadSprite($"{ArtPath}/Staff/zhou_purchaser.png"),
                LoadSprite($"{ArtPath}/Staff/anna_manager.png")
            },
            LoadFont("Assets/Fonts/NotoSansCJK-Regular.ttc"),
            LoadFont("Assets/Fonts/NotoNaskhArabic-Regular.ttf"));

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "口袋小店";
        PlayerSettings.companyName = "LocalPrototype";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.localprototype.pocketshop");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Pocket Shop/Build Android Debug APK")]
    public static void BuildAndroidDebug()
    {
        SetupProject();
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };

        BuildPipeline.BuildPlayer(options);
    }

    [MenuItem("Pocket Shop/Validate Localization")]
    public static void ValidateLocalization()
    {
        string[] files = Directory.GetFiles(LocalizationPath, "*.json").OrderBy(path => path).ToArray();
        if (files.Length == 0)
        {
            throw new Exception($"No localization files found in {LocalizationPath}.");
        }

        Dictionary<string, LocalizationTable> tables = new Dictionary<string, LocalizationTable>();
        foreach (string file in files)
        {
            LocalizationTable table = JsonUtility.FromJson<LocalizationTable>(File.ReadAllText(file));
            if (table == null || string.IsNullOrWhiteSpace(table.languageCode))
            {
                throw new Exception($"Localization file has no languageCode: {file}");
            }

            if (tables.ContainsKey(table.languageCode))
            {
                throw new Exception($"Duplicate localization languageCode '{table.languageCode}' in {file}.");
            }

            string duplicateKey = table.entries
                .GroupBy(entry => entry.key)
                .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1)
                .Select(group => string.IsNullOrWhiteSpace(group.Key) ? "<empty>" : group.Key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(duplicateKey))
            {
                throw new Exception($"Duplicate or empty localization key '{duplicateKey}' in {file}.");
            }

            string emptyValueKey = table.entries
                .Where(entry => string.IsNullOrWhiteSpace(entry.value))
                .Select(entry => entry.key)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(emptyValueKey))
            {
                throw new Exception($"Empty localization value for key '{emptyValueKey}' in {file}.");
            }

            tables[table.languageCode] = table;
        }

        if (!tables.TryGetValue("zh-CN", out LocalizationTable reference))
        {
            throw new Exception("Missing required reference localization file zh-CN.json.");
        }

        HashSet<string> referenceKeys = new HashSet<string>(reference.entries.Select(entry => entry.key));
        foreach (LocalizationTable table in tables.Values)
        {
            HashSet<string> keys = new HashSet<string>(table.entries.Select(entry => entry.key));
            string[] missing = referenceKeys.Except(keys).OrderBy(key => key).ToArray();
            string[] extra = keys.Except(referenceKeys).OrderBy(key => key).ToArray();

            if (missing.Length > 0 || extra.Length > 0)
            {
                string missingText = missing.Length == 0 ? "none" : string.Join(", ", missing);
                string extraText = extra.Length == 0 ? "none" : string.Join(", ", extra);
                throw new Exception($"Localization keys mismatch for {table.languageCode}. Missing: {missingText}. Extra: {extraText}.");
            }
        }

        Debug.Log($"Localization validation passed: {tables.Count} languages, {referenceKeys.Count} keys each.");
    }

    private static Sprite LoadSprite(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Optional art asset not found: {path}");
            return null;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            int maxSize = MaxTextureSizeFor(path);
            if (importer.maxTextureSize != maxSize)
            {
                importer.maxTextureSize = maxSize;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                changed = true;
            }

            if (!importer.crunchedCompression)
            {
                importer.crunchedCompression = true;
                changed = true;
            }

            if (importer.compressionQuality != 70)
            {
                importer.compressionQuality = 70;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"Art asset could not be loaded as Sprite: {path}");
        }

        return sprite;
    }

    private static Sprite LoadFirstSprite(params string[] paths)
    {
        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                return LoadSprite(path);
            }
        }

        Debug.LogWarning($"Optional art asset not found: {string.Join(" or ", paths)}");
        return null;
    }

    private static int MaxTextureSizeFor(string path)
    {
        if (path.Contains("/UI/"))
        {
            return 256;
        }

        if (path.Contains("/Products/") || path.Contains("/Staff/"))
        {
            return 512;
        }

        return 1024;
    }

    private static Font LoadFont(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Optional font asset not found: {path}");
            return null;
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
        if (font == null)
        {
            Debug.LogWarning($"Font asset could not be loaded: {path}");
        }

        return font;
    }
}
