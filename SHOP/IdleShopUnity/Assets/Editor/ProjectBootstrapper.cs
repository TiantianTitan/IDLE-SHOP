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
    private const string Mvp23Path = "Assets/Art/MVP23";
    private const string Mvp30Path = "Assets/Art/MVP30";
    private const string SaveKey = "PocketShop.Unity.Save.V1";

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
            LoadMvp30PreparedArtSprites(),
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

    private static Sprite[] LoadMvp23ArtSprites()
    {
        return new[]
        {
            LoadSprite($"{Mvp23Path}/scene_shop_interior_base.png"),
            LoadSprite($"{Mvp23Path}/scene_cashier_counter.png"),
            LoadSprite($"{Mvp23Path}/scene_product_shelf_full.png"),
            LoadSprite($"{Mvp23Path}/scene_product_shelf_low.png"),
            LoadSprite($"{Mvp23Path}/scene_product_shelf_empty.png"),
            LoadFirstSprite($"{Mvp23Path}/customer_enter_01.png", $"{Mvp23Path}/npc_customer_walk_01.png"),
            LoadFirstSprite($"{Mvp23Path}/customer_enter_01.png", $"{Mvp23Path}/npc_customer_walk_02.png"),
            LoadFirstSprite($"{Mvp23Path}/customer_waiting_01.png", $"{Mvp23Path}/npc_customer_idle.png"),
            LoadSprite($"{Mvp23Path}/npc_customer_pay.png"),
            LoadFirstSprite($"{Mvp23Path}/customer_happy_01.png", $"{Mvp23Path}/npc_customer_leave_happy.png"),
            LoadSprite($"{Mvp23Path}/npc_customer_disappointed.png"),
            LoadSprite($"{Mvp23Path}/npc_staff_cashier_idle.png"),
            LoadSprite($"{Mvp23Path}/npc_staff_cashier_work.png"),
            LoadSprite($"{Mvp23Path}/fx_coin_pop.png"),
            LoadSprite($"{Mvp23Path}/fx_restock_spark.png"),
            LoadSprite($"{Mvp23Path}/ui_order_ticket.png"),
            LoadSprite($"{Mvp23Path}/ui_order_item_slot.png"),
            LoadSprite($"{Mvp23Path}/ui_stock_warning_badge.png"),
            LoadSprite($"{Mvp23Path}/ui_current_item_frame.png"),
            LoadSprite($"{Mvp23Path}/icon_checkout_one.png"),
            LoadSprite($"{Mvp23Path}/icon_restock_item.png"),
            LoadSprite($"{Mvp23Path}/icon_upgrade_product.png"),
            LoadSprite($"{Mvp23Path}/scene_empty_shelf_overlay.png"),
            LoadFirstSprite($"{Mvp23Path}/ui_order_complete_stamp.png", $"{Mvp23Path}/fx_order_complete_stamp.png"),
            LoadSprite($"{Mvp23Path}/fx_low_stock_pulse.png"),
            LoadSprite($"{Mvp23Path}/fx_customer_waiting_bubble.png"),
            LoadSprite($"{Mvp23Path}/ui_scene_floor_shadow.png"),
            LoadSprite($"{Mvp23Path}/fx_order_complete_glow.png"),
            LoadSprite($"{Mvp23Path}/fx_item_selected_glow.png"),
            LoadSprite($"{Mvp23Path}/fx_cash_float.png"),
            LoadSprite($"{Mvp23Path}/fx_restock_success_ring.png"),
            LoadSprite($"{Mvp23Path}/fx_upgrade_success.png"),
            LoadFirstSprite($"{Mvp23Path}/customer_leave_01.png", $"{Mvp23Path}/npc_customer_leave_happy.png"),
            LoadSprite($"{Mvp23Path}/npc_staff_cashier_work.png")
        };
    }

    private static Sprite[] LoadMvp30PreparedArtSprites()
    {
        return new[]
        {
            LoadSprite($"{Mvp23Path}/scene_shop_interior_base.png"),
            LoadSprite($"{Mvp23Path}/scene_cashier_counter.png"),
            LoadFirstSprite($"{Mvp30Path}/Scene/Shelf/States/shelf_full_01.png", $"{Mvp23Path}/scene_product_shelf_full.png"),
            LoadFirstSprite($"{Mvp30Path}/Scene/Shelf/States/shelf_low_01.png", $"{Mvp23Path}/scene_product_shelf_low.png"),
            LoadFirstSprite($"{Mvp30Path}/Scene/Shelf/States/shelf_empty_01.png", $"{Mvp23Path}/scene_product_shelf_empty.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Customer/Enter/customer_enter_01.png", $"{Mvp23Path}/customer_enter_01.png", $"{Mvp23Path}/npc_customer_walk_01.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Customer/Enter/customer_enter_01.png", $"{Mvp23Path}/customer_enter_01.png", $"{Mvp23Path}/npc_customer_walk_02.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Customer/Idle/customer_idle_01.png", $"{Mvp23Path}/customer_waiting_01.png", $"{Mvp23Path}/npc_customer_idle.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Customer/Pay/customer_pay_01.png", $"{Mvp23Path}/npc_customer_pay.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Customer/Happy/customer_happy_01.png", $"{Mvp23Path}/customer_happy_01.png", $"{Mvp23Path}/npc_customer_leave_happy.png"),
            LoadSprite($"{Mvp23Path}/npc_customer_disappointed.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Staff/Cashier/Idle/staff_cashier_idle_01.png", $"{Mvp23Path}/npc_staff_cashier_idle.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Staff/Cashier/Work/staff_cashier_work_01.png", $"{Mvp23Path}/npc_staff_cashier_work.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_cash_float.png", $"{Mvp23Path}/fx_cash_float.png", $"{Mvp23Path}/fx_coin_pop.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_restock_success_ring.png", $"{Mvp23Path}/fx_restock_success_ring.png", $"{Mvp23Path}/fx_restock_spark.png"),
            LoadFirstSprite($"{Mvp30Path}/UI/Orders/ui_order_ticket.png", $"{Mvp23Path}/ui_order_ticket.png"),
            LoadFirstSprite($"{Mvp30Path}/UI/Orders/ui_order_item_slot.png", $"{Mvp23Path}/ui_order_item_slot.png"),
            LoadSprite($"{Mvp23Path}/ui_stock_warning_badge.png"),
            LoadSprite($"{Mvp23Path}/ui_current_item_frame.png"),
            LoadSprite($"{Mvp23Path}/icon_checkout_one.png"),
            LoadSprite($"{Mvp23Path}/icon_restock_item.png"),
            LoadSprite($"{Mvp23Path}/icon_upgrade_product.png"),
            LoadSprite($"{Mvp23Path}/scene_empty_shelf_overlay.png"),
            LoadFirstSprite($"{Mvp30Path}/UI/Orders/ui_order_complete_stamp.png", $"{Mvp23Path}/ui_order_complete_stamp.png", $"{Mvp23Path}/fx_order_complete_stamp.png"),
            LoadSprite($"{Mvp23Path}/fx_low_stock_pulse.png"),
            LoadSprite($"{Mvp23Path}/fx_customer_waiting_bubble.png"),
            LoadSprite($"{Mvp23Path}/ui_scene_floor_shadow.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_order_complete_glow.png", $"{Mvp23Path}/fx_order_complete_glow.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_item_selected_glow.png", $"{Mvp23Path}/fx_item_selected_glow.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_cash_float.png", $"{Mvp23Path}/fx_cash_float.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_restock_success_ring.png", $"{Mvp23Path}/fx_restock_success_ring.png"),
            LoadFirstSprite($"{Mvp30Path}/FX/fx_upgrade_success.png", $"{Mvp23Path}/fx_upgrade_success.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Customer/Leave/customer_leave_01.png", $"{Mvp23Path}/customer_leave_01.png", $"{Mvp23Path}/npc_customer_leave_happy.png"),
            LoadFirstSprite($"{Mvp30Path}/Characters/Staff/Cashier/Work/staff_cashier_success_01.png", $"{Mvp30Path}/Characters/Staff/Cashier/Work/staff_cashier_work_01.png", $"{Mvp23Path}/npc_staff_cashier_work.png")
        };
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

    [MenuItem("Pocket Shop/Clear Local Save")]
    public static void ClearLocalSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("Pocket Shop local save cleared. Enter Play Mode to test the new-player flow from day one.");
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
