using System;
using System.Collections.Generic;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class IdleShopGame : MonoBehaviour
{
    private enum Tab
    {
        Shop,
        Stock,
        Upgrades,
        Staff,
        Settings
    }

    private enum CustomerSceneState
    {
        Entering,
        Waiting,
        Paying,
        Blocked,
        HappyLeaving
    }

    [Serializable]
    private sealed class ProductDef
    {
        public string NameKey;
        public string SubtitleKey;
        public float Cost;
        public float Price;
        public float Demand;
        public int UnlockLevel;
        public int BaseMaxStock;
        public int MaxOrderQty;
        public Color Accent;

        public ProductDef(string nameKey, string subtitleKey, float cost, float price, float demand, int unlockLevel, int baseMaxStock, int maxOrderQty, Color accent)
        {
            NameKey = nameKey;
            SubtitleKey = subtitleKey;
            Cost = cost;
            Price = price;
            Demand = demand;
            UnlockLevel = unlockLevel;
            BaseMaxStock = baseMaxStock;
            MaxOrderQty = maxOrderQty;
            Accent = accent;
        }
    }

    [Serializable]
    private sealed class UpgradeDef
    {
        public string NameKey;
        public string DetailKey;
        public float BaseCost;
        public int MaxLevel;

        public UpgradeDef(string nameKey, string detailKey, float baseCost, int maxLevel)
        {
            NameKey = nameKey;
            DetailKey = detailKey;
            BaseCost = baseCost;
            MaxLevel = maxLevel;
        }
    }

    [Serializable]
    private sealed class StaffDef
    {
        public string NameKey;
        public string DetailKey;
        public float BaseCost;
        public int MaxLevel;

        public StaffDef(string nameKey, string detailKey, float baseCost, int maxLevel)
        {
            NameKey = nameKey;
            DetailKey = detailKey;
            BaseCost = baseCost;
            MaxLevel = maxLevel;
        }
    }

    [Serializable]
    private sealed class SaveData
    {
        public int saveVersion = 3;
        public float cash = 80f;
        public float reputation;
        public float xp;
        public float totalEarned;
        public float dayProgress;
        public float queue;
        public int level = 1;
        public int day = 1;
        public int soldToday;
        public int pendingOfflineMinutes;
        public int pendingOfflineSold;
        public float pendingOfflineEarned;
        public int restockActions;
        public int completedOrders;
        public int selectedOrderProductIndex = -1;
        public int currentOrderEarned;
        public long lastSavedUnix;
        public float[] stock = Array.Empty<float>();
        public int[] sold = Array.Empty<int>();
        public int[] currentOrder = Array.Empty<int>();
        public int[] currentOrderInitial = Array.Empty<int>();
        public bool[] unlocked = Array.Empty<bool>();
        public int[] upgrades = Array.Empty<int>();
        public int[] staff = Array.Empty<int>();
        public bool[] milestonesClaimed = Array.Empty<bool>();
    }

    private const string SaveKey = "PocketShop.Unity.Save.V1";
    private const int CurrentSaveVersion = 3;
    private const float DayLength = 90f;
    private const float OfflineCapSeconds = 2f * 60f * 60f;
    private const float LowStockRatio = 0.20f;
    private const float UpgradeGoalRatio = 0.70f;
    private const float StaffGoalRatio = 0.70f;
    private const float CustomerEnterDurationSeconds = 5.50f;
    private const float CustomerLeaveDurationSeconds = 4.80f;
    private const int StaffRecommendationMinSold = 30;
    private const int MilestoneCount = 5;
    private const int Mvp23SceneInterior = 0;
    private const int Mvp23CashierCounter = 1;
    private const int Mvp23ShelfFull = 2;
    private const int Mvp23ShelfLow = 3;
    private const int Mvp23ShelfEmpty = 4;
    private const int Mvp23CustomerWalk01 = 5;
    private const int Mvp23CustomerWalk02 = 6;
    private const int Mvp23CustomerIdle = 7;
    private const int Mvp23CustomerPay = 8;
    private const int Mvp23CustomerHappy = 9;
    private const int Mvp23CustomerDisappointed = 10;
    private const int Mvp23StaffCashierIdle = 11;
    private const int Mvp23StaffCashierWork = 12;
    private const int Mvp23FxCoinPop = 13;
    private const int Mvp23FxRestockSpark = 14;
    private const int Mvp23UiOrderTicket = 15;
    private const int Mvp23UiOrderItemSlot = 16;
    private const int Mvp23UiStockWarningBadge = 17;
    private const int Mvp23UiCurrentItemFrame = 18;
    private const int Mvp23IconCheckoutOne = 19;
    private const int Mvp23IconRestockItem = 20;
    private const int Mvp23IconUpgradeProduct = 21;
    private const int Mvp23SceneEmptyShelfOverlay = 22;
    private const int Mvp23FxOrderCompleteStamp = 23;
    private const int Mvp23FxLowStockPulse = 24;
    private const int Mvp23FxCustomerWaitingBubble = 25;
    private const int Mvp23SceneFloorShadow = 26;
    private const int Mvp23FxOrderCompleteGlow = 27;
    private const int Mvp23FxItemSelectedGlow = 28;
    private const int Mvp23FxCashFloat = 29;
    private const int Mvp23FxRestockSuccessRing = 30;
    private const int Mvp23FxUpgradeSuccess = 31;
    private const int Mvp23CustomerLeave = 32;
    private const int Mvp30StaffCashierSuccess = 33;
    private const int Mvp30ShelfRestocked = 34;
    private const int Mvp30IconOrder = 35;
    private const int Mvp30CustomerWalk01 = 36;
    private const int Mvp30CustomerWalk02 = 37;
    private const int Mvp30CustomerWalk03 = 38;
    private const int Mvp30CustomerWalk04 = 39;
    private readonly ProductDef[] products =
    {
        new ProductDef("product.rice_ball.name", "product.rice_ball.subtitle", 6f, 12f, 1.35f, 1, 12, 2, new Color(0.94f, 0.48f, 0.32f)),
        new ProductDef("product.sparkling_water.name", "product.sparkling_water.subtitle", 9f, 19f, 1.18f, 2, 12, 2, new Color(0.18f, 0.58f, 0.72f)),
        new ProductDef("product.bread.name", "product.bread.subtitle", 11f, 24f, 1.05f, 3, 12, 2, new Color(0.85f, 0.56f, 0.27f)),
        new ProductDef("product.coffee.name", "product.coffee.subtitle", 14f, 30f, 0.95f, 2, 12, 2, new Color(0.54f, 0.34f, 0.22f)),
        new ProductDef("product.lunch_box.name", "product.lunch_box.subtitle", 24f, 52f, 0.78f, 3, 10, 2, new Color(0.89f, 0.64f, 0.22f)),
        new ProductDef("product.dessert.name", "product.dessert.subtitle", 30f, 68f, 0.64f, 4, 9, 2, new Color(0.84f, 0.48f, 0.64f)),
        new ProductDef("product.flower.name", "product.flower.subtitle", 48f, 118f, 0.48f, 6, 8, 1, new Color(0.78f, 0.38f, 0.62f)),
        new ProductDef("product.gift_box.name", "product.gift_box.subtitle", 72f, 180f, 0.34f, 8, 6, 1, new Color(0.62f, 0.46f, 0.86f))
    };

    private readonly UpgradeDef[] upgrades =
    {
        new UpgradeDef("upgrade.shelf.name", "upgrade.shelf.detail", 210f, 8),
        new UpgradeDef("upgrade.signboard.name", "upgrade.signboard.detail", 240f, 10),
        new UpgradeDef("upgrade.fridge.name", "upgrade.fridge.detail", 150f, 7),
        new UpgradeDef("upgrade.express.name", "upgrade.express.detail", 420f, 7)
    };

    private readonly StaffDef[] staffDefs =
    {
        new StaffDef("staff.kobayashi.name", "staff.kobayashi.detail", 180f, 30),
        new StaffDef("staff.misaki.name", "staff.misaki.detail", 220f, 30),
        new StaffDef("staff.aken.name", "staff.aken.detail", 260f, 30),
        new StaffDef("staff.lina.name", "staff.lina.detail", 360f, 25),
        new StaffDef("staff.zhou.name", "staff.zhou.detail", 520f, 25),
        new StaffDef("staff.anna.name", "staff.anna.detail", 900f, 20)
    };

    private readonly List<string> log = new List<string>();
    private readonly Color bg = new Color(0.95f, 0.89f, 0.79f);
    private readonly Color panel = new Color(1.0f, 0.97f, 0.90f);
    private readonly Color ink = new Color(0.12f, 0.20f, 0.17f);
    private readonly Color pine = new Color(0.08f, 0.22f, 0.18f);
    private readonly Color coral = new Color(0.86f, 0.32f, 0.26f);
    private readonly Color blue = new Color(0.18f, 0.46f, 0.58f);
    private readonly Color honey = new Color(0.93f, 0.65f, 0.20f);
    private readonly Color mintPanel = new Color(0.80f, 0.90f, 0.80f);
    private readonly Color inactiveTab = new Color(0.70f, 0.60f, 0.46f);
    private readonly Color dock = new Color(0.12f, 0.09f, 0.07f);
    private readonly Color inactiveNav = new Color(0.38f, 0.27f, 0.17f);
    private Sprite roundedPanelSprite = null!;

    [SerializeField] private Sprite shopFrontSprite = null!;
    [SerializeField] private Sprite shelfSprite = null!;
    [SerializeField] private Sprite counterSprite = null!;
    [SerializeField] private Sprite coinSprite = null!;
    [SerializeField] private Sprite reputationSprite = null!;
    [SerializeField] private Sprite customerSprite = null!;
    [SerializeField] private Sprite upgradeSprite = null!;
    [SerializeField] private Sprite[] productSprites = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] staffSprites = Array.Empty<Sprite>();
    [SerializeField] private Sprite[] mvp23Sprites = Array.Empty<Sprite>();
    [SerializeField] private Font mainFont = null!;
    [SerializeField] private Font arabicFont = null!;

    private SaveData data = new SaveData();
    private Tab activeTab = Tab.Shop;
    private RectTransform contentRoot = null!;
    private Text titleText = null!;
    private Text cashText = null!;
    private Text reputationText = null!;
    private Text totalText = null!;
    private Text newItemsText = null!;
    private Text queueText = null!;
    private Text stageCashText = null!;
    private Text stageReputationText = null!;
    private Text stageIncomeText = null!;
    private Text autoSaleText = null!;
    private Text autoIntervalText = null!;
    private Text salePopText = null!;
    private RectTransform autoSaleFill = null!;
    private RectTransform headerAutoFill = null!;
    private RectTransform salePopRectTransform = null!;
    private RectTransform salePopIconRectTransform = null!;
    private RectTransform sceneProductMotion = null!;
    private RectTransform sceneCustomerMotion = null!;
    private RectTransform sceneStaffMotion = null!;
    private RectTransform sceneRestockFxMotion = null!;
    private RectTransform sceneCheckoutItemMotion = null!;
    private Image salePopIconImage = null!;
    private Image sceneProductImage = null!;
    private Outline sceneProductOutline = null!;
    private Image sceneCustomerImage = null!;
    private Image sceneStaffImage = null!;
    private Image sceneRestockFxImage = null!;
    private Image sceneCheckoutItemImage = null!;
    private Image sceneCashierGlow = null!;
    private Image sceneCustomerGlow = null!;
    private Text offlineText = null!;
    private LayoutElement offlineLayout = null!;
    private RectTransform offlineRewardOverlay = null!;
    private readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
    private float renderTimer;
    private float saveTimer;
    private bool wideLayout;
    private Vector2Int layoutScreenSize;
    private string offlineNotice = string.Empty;
    private string salePopMessage = string.Empty;
    private string orderCompleteMessage = string.Empty;
    private string orderCompleteTitle = string.Empty;
    private float salePopTimer;
    private float orderCompleteTimer;
    private float newOrderFlashTimer;
    private float restockPulseTimer;
    private float upgradePulseTimer;
    private float orderSelectPulseTimer;
    private float displayedAutoSaleProgress;
    private Color sceneProductBaseColor;
    private bool pendingOrderCompletion;
    private int recentSoldProductIndex = -1;
    private int recentRestockProductIndex = -1;

    public void ConfigureArt(Sprite shopFront, Sprite shelf, Sprite counter, Sprite coin, Sprite reputation, Sprite customer, Sprite upgrade, Sprite[] productsArt, Sprite[] staffArt, Sprite[] mvp23Art, Font defaultUiFont, Font rtlFont)
    {
        shopFrontSprite = shopFront;
        shelfSprite = shelf;
        counterSprite = counter;
        coinSprite = coin;
        reputationSprite = reputation;
        customerSprite = customer;
        upgradeSprite = upgrade;
        productSprites = productsArt ?? Array.Empty<Sprite>();
        staffSprites = staffArt ?? Array.Empty<Sprite>();
        mvp23Sprites = mvp23Art ?? Array.Empty<Sprite>();
        mainFont = defaultUiFont;
        arabicFont = rtlFont;
    }

#if UNITY_EDITOR
    private void EnsureEditorMvp30ArtConfigured()
    {
        EnsureEditorMvp30SpriteImports();

        productSprites = new[]
        {
            EditorFirstSprite("Assets/Art/MVP30/Products/product_onigiri_01.png", "Assets/Art/Products/rice_ball.png"),
            EditorFirstSprite("Assets/Art/MVP30/Products/product_tea_01.png", "Assets/Art/Products/sparkling_water.png"),
            EditorSprite("Assets/Art/Products/bread.png"),
            EditorSprite("Assets/Art/Products/coffee.png"),
            EditorFirstSprite("Assets/Art/MVP30/Products/product_bento_01.png", "Assets/Art/Products/lunch_box.png"),
            EditorFirstSprite("Assets/Art/MVP30/Products/product_dessert_01.png", "Assets/Art/Products/dessert.png"),
            EditorSprite("Assets/Art/Products/flower.png"),
            EditorSprite("Assets/Art/Products/gift_box.png")
        };

        mvp23Sprites = new[]
        {
            EditorFirstSprite("Assets/Art/MVP30/Scene/Interior/scene_shop_interior_base.png", "Assets/Art/MVP23/scene_shop_interior_base.png"),
            EditorFirstSprite("Assets/Art/MVP30/Scene/Counter/scene_cashier_counter.png", "Assets/Art/MVP23/scene_cashier_counter.png"),
            EditorFirstSprite("Assets/Art/MVP30/Scene/Shelf/States/shelf_full_01.png", "Assets/Art/MVP23/scene_product_shelf_full.png"),
            EditorFirstSprite("Assets/Art/MVP30/Scene/Shelf/States/shelf_low_01.png", "Assets/Art/MVP23/scene_product_shelf_low.png"),
            EditorFirstSprite("Assets/Art/MVP30/Scene/Shelf/States/shelf_empty_01.png", "Assets/Art/MVP23/scene_product_shelf_empty.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_01.png", "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png", "Assets/Art/MVP23/customer_enter_01.png", "Assets/Art/MVP23/npc_customer_walk_01.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_03.png", "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png", "Assets/Art/MVP23/customer_enter_01.png", "Assets/Art/MVP23/npc_customer_walk_02.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Idle/customer_idle_01.png", "Assets/Art/MVP23/customer_waiting_01.png", "Assets/Art/MVP23/npc_customer_idle.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Pay/customer_pay_01.png", "Assets/Art/MVP23/npc_customer_pay.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Happy/customer_happy_01.png", "Assets/Art/MVP23/customer_happy_01.png", "Assets/Art/MVP23/npc_customer_leave_happy.png"),
            EditorSprite("Assets/Art/MVP23/npc_customer_disappointed.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Staff/Cashier/Idle/staff_cashier_idle_01.png", "Assets/Art/MVP23/npc_staff_cashier_idle.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_work_01.png", "Assets/Art/MVP23/npc_staff_cashier_work.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_cash_float.png", "Assets/Art/MVP23/fx_cash_float.png", "Assets/Art/MVP23/fx_coin_pop.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_restock_success_ring.png", "Assets/Art/MVP23/fx_restock_success_ring.png", "Assets/Art/MVP23/fx_restock_spark.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Orders/ui_order_ticket.png", "Assets/Art/MVP23/ui_order_ticket.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Orders/ui_order_item_slot.png", "Assets/Art/MVP23/ui_order_item_slot.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Orders/ui_stock_warning_badge.png", "Assets/Art/MVP23/ui_stock_warning_badge.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Orders/ui_current_item_frame.png", "Assets/Art/MVP23/ui_current_item_frame.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Icons/icon_checkout_one.png", "Assets/Art/MVP23/icon_checkout_one.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Icons/icon_restock_item.png", "Assets/Art/MVP23/icon_restock_item.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Icons/icon_upgrade_product.png", "Assets/Art/MVP23/icon_upgrade_product.png"),
            EditorSprite("Assets/Art/MVP23/scene_empty_shelf_overlay.png"),
            EditorFirstSprite("Assets/Art/MVP30/UI/Orders/ui_order_complete_stamp.png", "Assets/Art/MVP23/ui_order_complete_stamp.png", "Assets/Art/MVP23/fx_order_complete_stamp.png"),
            EditorSprite("Assets/Art/MVP23/fx_low_stock_pulse.png"),
            EditorSprite("Assets/Art/MVP23/fx_customer_waiting_bubble.png"),
            EditorSprite("Assets/Art/MVP23/ui_scene_floor_shadow.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_order_complete_glow.png", "Assets/Art/MVP23/fx_order_complete_glow.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_item_selected_glow.png", "Assets/Art/MVP23/fx_item_selected_glow.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_cash_float.png", "Assets/Art/MVP23/fx_cash_float.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_restock_success_ring.png", "Assets/Art/MVP23/fx_restock_success_ring.png"),
            EditorFirstSprite("Assets/Art/MVP30/FX/fx_upgrade_success.png", "Assets/Art/MVP23/fx_upgrade_success.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Leave/customer_leave_01.png", "Assets/Art/MVP23/customer_leave_01.png", "Assets/Art/MVP23/npc_customer_leave_happy.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_success_01.png", "Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_work_01.png", "Assets/Art/MVP23/npc_staff_cashier_work.png"),
            EditorFirstSprite("Assets/Art/MVP30/Scene/Shelf/States/shelf_restocked_01.png", "Assets/Art/MVP30/Scene/Shelf/States/shelf_full_01.png", "Assets/Art/MVP23/scene_product_shelf_full.png"),
            EditorSprite("Assets/Art/MVP30/UI/Icons/icon_order.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_01.png", "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_02.png", "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_03.png", "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png"),
            EditorFirstSprite("Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_04.png", "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png")
        };

        if (staffSprites == null || staffSprites.Length == 0 || staffSprites[0] == null)
        {
            staffSprites = new[]
            {
                EditorFirstSprite("Assets/Art/MVP30/Characters/Staff/Cashier/Idle/staff_cashier_idle_01.png", "Assets/Art/Staff/kobayashi_cashier.png", "Assets/Art/Staff/cashier.png")
            };
        }

        Debug.Log("MVP3.0 editor art auto-configured from Assets/Art/MVP30.");
    }

    private static void EnsureEditorMvp30SpriteImports()
    {
        string[] paths =
        {
            "Assets/Art/MVP30/Scene/Interior/scene_shop_interior_base.png",
            "Assets/Art/MVP30/Scene/Counter/scene_cashier_counter.png",
            "Assets/Art/MVP30/Scene/Shelf/States/shelf_full_01.png",
            "Assets/Art/MVP30/Scene/Shelf/States/shelf_low_01.png",
            "Assets/Art/MVP30/Scene/Shelf/States/shelf_empty_01.png",
            "Assets/Art/MVP30/Scene/Shelf/States/shelf_restocked_01.png",
            "Assets/Art/MVP30/Characters/Customer/Enter/customer_enter_01.png",
            "Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_01.png",
            "Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_02.png",
            "Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_03.png",
            "Assets/Art/MVP30/Characters/Customer/Walk/customer_walk_04.png",
            "Assets/Art/MVP30/Characters/Customer/Idle/customer_idle_01.png",
            "Assets/Art/MVP30/Characters/Customer/Pay/customer_pay_01.png",
            "Assets/Art/MVP30/Characters/Customer/Happy/customer_happy_01.png",
            "Assets/Art/MVP30/Characters/Customer/Leave/customer_leave_01.png",
            "Assets/Art/MVP30/Characters/Staff/Cashier/Idle/staff_cashier_idle_01.png",
            "Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_work_01.png",
            "Assets/Art/MVP30/Characters/Staff/Cashier/Work/staff_cashier_success_01.png",
            "Assets/Art/MVP30/Products/product_onigiri_01.png",
            "Assets/Art/MVP30/Products/product_tea_01.png",
            "Assets/Art/MVP30/Products/product_bento_01.png",
            "Assets/Art/MVP30/Products/product_dessert_01.png",
            "Assets/Art/MVP30/UI/Orders/ui_order_ticket.png",
            "Assets/Art/MVP30/UI/Orders/ui_order_item_slot.png",
            "Assets/Art/MVP30/UI/Orders/ui_stock_warning_badge.png",
            "Assets/Art/MVP30/UI/Orders/ui_current_item_frame.png",
            "Assets/Art/MVP30/UI/Orders/ui_order_complete_stamp.png",
            "Assets/Art/MVP30/UI/Icons/icon_checkout_one.png",
            "Assets/Art/MVP30/UI/Icons/icon_restock_item.png",
            "Assets/Art/MVP30/UI/Icons/icon_upgrade_product.png",
            "Assets/Art/MVP30/UI/Icons/icon_order.png",
            "Assets/Art/MVP30/FX/fx_order_complete_glow.png",
            "Assets/Art/MVP30/FX/fx_item_selected_glow.png",
            "Assets/Art/MVP30/FX/fx_cash_float.png",
            "Assets/Art/MVP30/FX/fx_restock_success_ring.png",
            "Assets/Art/MVP30/FX/fx_upgrade_success.png"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            EnsureEditorSpriteImport(paths[i]);
        }
    }

    private static void EnsureEditorSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        int maxSize = EditorSpriteMaxTextureSize(path);
        if (importer.maxTextureSize != maxSize)
        {
            importer.maxTextureSize = maxSize;
            dirty = true;
        }

        if (dirty)
        {
            importer.SaveAndReimport();
        }
    }

    private static int EditorSpriteMaxTextureSize(string path)
    {
        if (path.Contains("/Scene/Interior/") || path.Contains("/Scene/Counter/"))
        {
            return 2048;
        }

        if (path.Contains("/Characters/"))
        {
            return 1024;
        }

        return 512;
    }

    private static Sprite EditorSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite EditorFirstSprite(params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            Sprite sprite = EditorSprite(paths[i]);
            if (sprite != null)
            {
                return sprite;
            }
        }

        Debug.LogWarning($"Optional editor art asset not found: {string.Join(" or ", paths)}");
        return null;
    }
#endif

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        try
        {
#if UNITY_EDITOR
            EnsureEditorMvp30ArtConfigured();
#endif
            LocalizationManager.EnsureLoaded();
            Load();
            CatchUpOffline();
            BuildUI();
            RenderAll();
            Debug.Log("IdleShopGame started.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            BuildStartupErrorUI(exception);
        }
    }

    private void Update()
    {
        if (NeedsLayoutRebuild())
        {
            RebuildInterface();
        }

        Simulate(Time.deltaTime, false);
        if (CheckMilestones())
        {
            RenderAll();
            saveTimer = 2f;
        }

        if (salePopTimer > 0f)
        {
            salePopTimer = Mathf.Max(0f, salePopTimer - Time.deltaTime);
        }

        if (orderCompleteTimer > 0f)
        {
            orderCompleteTimer = Mathf.Max(0f, orderCompleteTimer - Time.deltaTime);
            if (pendingOrderCompletion && orderCompleteTimer <= 0f)
            {
                pendingOrderCompletion = false;
                GenerateCurrentOrder();
                newOrderFlashTimer = CustomerEnterDurationSeconds;
                RenderAll();
            }
        }

        if (newOrderFlashTimer > 0f)
        {
            newOrderFlashTimer = Mathf.Max(0f, newOrderFlashTimer - Time.deltaTime);
        }

        if (restockPulseTimer > 0f)
        {
            restockPulseTimer = Mathf.Max(0f, restockPulseTimer - Time.deltaTime);
        }

        if (upgradePulseTimer > 0f)
        {
            upgradePulseTimer = Mathf.Max(0f, upgradePulseTimer - Time.deltaTime);
        }

        if (orderSelectPulseTimer > 0f)
        {
            orderSelectPulseTimer = Mathf.Max(0f, orderSelectPulseTimer - Time.deltaTime);
        }

        UpdateAutoSaleVisuals();
        renderTimer += Time.deltaTime;
        saveTimer += Time.deltaTime;

        if (renderTimer >= 0.12f)
        {
            RenderHeader();

            renderTimer = 0f;
        }

        if (saveTimer >= 2f)
        {
            Save();
            saveTimer = 0f;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private bool NeedsLayoutRebuild()
    {
        Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
        if (currentSize == layoutScreenSize)
        {
            return false;
        }

        bool nextWideLayout = ShouldUseWideLayout();
        layoutScreenSize = currentSize;
        return nextWideLayout != wideLayout;
    }

    private static bool ShouldUseWideLayout()
    {
        int width = Mathf.Max(Screen.width, 1);
        int height = Mathf.Max(Screen.height, 1);
        return width / (float)height >= 0.72f;
    }

    private static bool IsShortPortraitScreen()
    {
        int width = Mathf.Max(Screen.width, 1);
        int height = Mathf.Max(Screen.height, 1);
        return width < height && height / (float)width < 1.9f;
    }

    private void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                SaveData loaded = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (loaded != null && loaded.saveVersion == CurrentSaveVersion)
                {
                    data = loaded;
                }
                else
                {
                    data = new SaveData();
                    PlayerPrefs.DeleteKey(SaveKey);
                }
            }
            catch
            {
                data = new SaveData();
            }
        }

        NormalizeData();
        if (data.pendingOfflineEarned > 0.01f)
        {
            offlineNotice = F("offline.notice", Money(data.pendingOfflineEarned));
        }

        if (log.Count == 0)
        {
            AddLog(T("log.opening"));
        }
    }

    private void NormalizeData()
    {
        data.stock = Resize(data.stock, products.Length);
        data.sold = Resize(data.sold, products.Length);
        data.currentOrder = Resize(data.currentOrder, products.Length);
        data.currentOrderInitial = Resize(data.currentOrderInitial, products.Length);
        data.unlocked = Resize(data.unlocked, products.Length);
        data.upgrades = Resize(data.upgrades, upgrades.Length);
        data.staff = Resize(data.staff, staffDefs.Length);
        data.milestonesClaimed = Resize(data.milestonesClaimed, MilestoneCount);

        data.level = Mathf.Max(1, data.level);
        data.cash = Mathf.Max(0f, data.cash);
        data.currentOrderEarned = Mathf.Max(0, data.currentOrderEarned);
        data.day = Mathf.Max(1, data.day);
        if (data.selectedOrderProductIndex < -1 || data.selectedOrderProductIndex >= products.Length)
        {
            data.selectedOrderProductIndex = -1;
        }

        data.saveVersion = CurrentSaveVersion;
        data.unlocked[0] = true;

        for (int i = 0; i < products.Length; i++)
        {
            if (data.level >= products[i].UnlockLevel)
            {
                data.unlocked[i] = true;
            }
        }

        if (data.totalEarned <= 0.01f)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (data.unlocked[i] && data.stock[i] <= 0f)
                {
                    data.stock[i] = Mathf.Min(InitialStock(i), MaxStock(i));
                }
            }
        }

        bool hasActiveOrder = false;
        bool hasInitialOrder = false;
        for (int i = 0; i < products.Length; i++)
        {
            hasActiveOrder |= data.currentOrder[i] > 0;
            hasInitialOrder |= data.currentOrderInitial[i] > 0;
        }

        if (hasActiveOrder && !hasInitialOrder)
        {
            for (int i = 0; i < products.Length; i++)
            {
                data.currentOrderInitial[i] = data.currentOrder[i];
            }
        }

        EnsureCurrentOrder();
        if (data.selectedOrderProductIndex < 0 || CurrentOrderQuantity(data.selectedOrderProductIndex) <= 0)
        {
            data.selectedOrderProductIndex = CurrentOrderDisplayProductIndex();
        }
    }

    private static float[] Resize(float[] source, int length)
    {
        var result = new float[length];
        if (source != null)
        {
            Array.Copy(source, result, Mathf.Min(source.Length, length));
        }

        return result;
    }

    private static int[] Resize(int[] source, int length)
    {
        var result = new int[length];
        if (source != null)
        {
            Array.Copy(source, result, Mathf.Min(source.Length, length));
        }

        return result;
    }

    private static bool[] Resize(bool[] source, int length)
    {
        var result = new bool[length];
        if (source != null)
        {
            Array.Copy(source, result, Mathf.Min(source.Length, length));
        }

        return result;
    }

    private void Save()
    {
        data.lastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void CatchUpOffline()
    {
        if (data.lastSavedUnix <= 0)
        {
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        float elapsed = Mathf.Clamp((float)(now - data.lastSavedUnix), 0f, OfflineCapSecondsLimit());
        if (elapsed < 20f)
        {
            return;
        }

        float before = data.cash;
        int soldBefore = TotalSoldCount();
        Simulate(elapsed, true);
        float gained = Mathf.Max(0f, data.cash - before);
        int soldAfter = TotalSoldCount();
        if (gained > 0f)
        {
            data.cash = Mathf.Max(0f, data.cash - gained);
            data.totalEarned = Mathf.Max(0f, data.totalEarned - gained);
            data.pendingOfflineMinutes += Mathf.Max(1, Mathf.FloorToInt(elapsed / 60f));
            data.pendingOfflineSold += Mathf.Max(0, soldAfter - soldBefore);
            data.pendingOfflineEarned += gained;
            offlineNotice = F("offline.notice", Money(gained));
            Save();
        }
    }

    private int TotalSoldCount()
    {
        int total = 0;
        for (int i = 0; i < data.sold.Length; i++)
        {
            total += data.sold[i];
        }

        return total;
    }

    private int TotalStockCount()
    {
        int total = 0;
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i])
            {
                total += Mathf.FloorToInt(data.stock[i]);
            }
        }

        return total;
    }

    private int TotalStockCapacity()
    {
        int total = 0;
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i])
            {
                total += MaxStock(i);
            }
        }

        return Mathf.Max(1, total);
    }

    private int CurrentMilestoneIndex()
    {
        for (int i = 0; i < MilestoneCount; i++)
        {
            if (!data.milestonesClaimed[i])
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsMilestoneComplete(int index)
    {
        switch (index)
        {
            case 0:
                return data.completedOrders >= 1;
            case 1:
                return data.restockActions >= 1;
            case 2:
                return data.completedOrders >= 3;
            case 3:
                return data.upgrades.Length > 2 && data.upgrades[2] >= 1;
            case 4:
                return data.staff.Length > 0 && data.staff[0] >= 1;
            default:
                return false;
        }
    }

    private float MilestoneProgress(int index)
    {
        switch (index)
        {
            case 0:
                return Mathf.Clamp01(data.completedOrders);
            case 1:
                return data.restockActions >= 1 ? 1f : 0f;
            case 2:
                return Mathf.Clamp01(data.completedOrders / 3f);
            case 3:
                return data.upgrades.Length > 2 && data.upgrades[2] >= 1 ? 1f : 0f;
            case 4:
                return data.staff.Length > 0 && data.staff[0] >= 1 ? 1f : 0f;
            default:
                return 1f;
        }
    }

    private int MilestoneReward(int index)
    {
        switch (index)
        {
            case 0:
                return 12;
            case 1:
                return 30;
            case 2:
                return 35;
            case 3:
                return 80;
            case 4:
                return 120;
            default:
                return 0;
        }
    }

    private string MilestoneName(int index)
    {
        switch (index)
        {
            case 0:
                return T("milestone.order_1.name");
            case 1:
                return T("milestone.restock_once.name");
            case 2:
                return T("milestone.order_3.name");
            case 3:
                return T("milestone.price_upgrade.name");
            case 4:
                return T("milestone.cashier.name");
            default:
                return string.Empty;
        }
    }

    private string CurrentMilestoneText()
    {
        int index = CurrentMilestoneIndex();
        if (index < 0)
        {
            return T("milestone.all_done");
        }

        string reward = Money(MilestoneReward(index));
        switch (index)
        {
            case 0:
                return F("milestone.order_1", data.completedOrders, reward);
            case 1:
                return F("milestone.restock_once", reward);
            case 2:
                return F("milestone.order_3", data.completedOrders, reward);
            case 3:
                return F("milestone.price_upgrade", reward);
            case 4:
                return F("milestone.cashier", reward);
            default:
                return T("milestone.all_done");
        }
    }

    private string CurrentMilestoneHint()
    {
        switch (CurrentMilestoneIndex())
        {
            case 0:
                return T("milestone.hint.sell");
            case 1:
                return T("milestone.hint.restock");
            case 2:
                return T("milestone.hint.order_3");
            case 3:
                return T("milestone.hint.upgrade");
            case 4:
                return T("milestone.hint.cashier");
            default:
                return T("milestone.hint.all_done");
        }
    }

    private bool CheckMilestones()
    {
        bool completedAny = false;
        int totalReward = 0;
        for (int i = 0; i < MilestoneCount; i++)
        {
            if (data.milestonesClaimed[i] || !IsMilestoneComplete(i))
            {
                continue;
            }

            int reward = MilestoneReward(i);
            data.milestonesClaimed[i] = true;
            data.cash += reward;
            data.totalEarned += reward;
            totalReward += reward;
            AddLog(F("milestone.claimed", MilestoneName(i), Money(reward)));
            completedAny = true;
        }

        if (totalReward > 0)
        {
            salePopMessage = $"+{Money(totalReward)}";
            salePopTimer = 1.25f;
            upgradePulseTimer = Mathf.Max(upgradePulseTimer, 0.55f);
        }

        return completedAny;
    }

    private void Simulate(float seconds, bool offline)
    {
        if (seconds <= 0f)
        {
            return;
        }

        data.dayProgress += seconds / DayLength;
        while (data.dayProgress >= 1f)
        {
            data.dayProgress -= 1f;
            int bonus = Mathf.FloorToInt(Mathf.Sqrt(data.soldToday) * 7f + data.reputation * 1.5f);
            if (bonus > 0)
            {
                data.cash += bonus;
                data.totalEarned += bonus;
                AddLog(F("log.day_bonus", data.day, Money(bonus)));
            }

            data.day += 1;
            data.soldToday = 0;
        }

        if (pendingOrderCompletion)
        {
            data.queue = 0f;
            return;
        }

        EnsureCurrentOrder();
        if (CurrentOrderServeIndex() < 0)
        {
            data.queue = 0f;
            return;
        }

        data.queue += seconds * OrderProgressPerSecond();
        int served = 0;
        int serveLimit = offline ? 6000 : 20;
        while (data.queue >= 1f && served < serveLimit)
        {
            data.queue -= 1f;
            bool sold = CompleteCurrentOrder(false);
            if (!sold && offline)
            {
                break;
            }

            served++;
        }
    }

    private bool CompleteCurrentOrder(bool manual)
    {
        EnsureCurrentOrder();
        int productIndex = CurrentOrderServeIndex();
        if (productIndex < 0)
        {
            if (manual)
            {
                AddLog(CurrentOrderShortageText());
            }

            data.reputation = Mathf.Max(0f, data.reputation - 0.02f);
            data.queue = 0f;
            return false;
        }

        string orderText = CurrentOrderSummary();
        string completionTitle = OrderCompletionTitle(orderText);
        int earned = CurrentOrderItemValue(productIndex);
        data.currentOrder[productIndex] = Mathf.Max(0, data.currentOrder[productIndex] - 1);
        data.stock[productIndex] -= 1f;
        data.sold[productIndex] += 1;

        data.cash += earned;
        data.totalEarned += earned;
        data.soldToday += 1;
        data.currentOrderEarned += earned;
        data.xp += 10f + Mathf.Floor(earned / 14f);
        data.reputation += 0.026f * (1f + StaffLevel(2) * 0.12f + StaffLevel(5) * 0.05f);
        salePopMessage = $"+{Money(earned)}";
        salePopTimer = manual ? 1.15f : 0.8f;
        recentSoldProductIndex = productIndex;

        bool orderDone = CurrentOrderItemCount() <= 0;
        if (manual || UnityEngine.Random.value < 0.14f)
        {
            AddLog(F("log.sold", ProductName(productIndex), Money(earned)));
        }

        if (orderDone)
        {
            int orderEarned = Mathf.Max(earned, data.currentOrderEarned);
            data.completedOrders += 1;
            data.xp += 12f + Mathf.Floor(earned / 16f);
            orderCompleteMessage = F("order.complete_feedback", Money(orderEarned));
            orderCompleteTitle = completionTitle;
            orderCompleteTimer = CustomerLeaveDurationSeconds;
            pendingOrderCompletion = true;
            data.queue = 0f;
            AddLog(F("log.order_completed", orderCompleteTitle, Money(orderEarned)));
        }
        else if (CurrentOrderQuantity(data.selectedOrderProductIndex) <= 0)
        {
            data.selectedOrderProductIndex = CurrentOrderDisplayProductIndex();
        }

        while (data.xp >= XpNeeded())
        {
            data.xp -= XpNeeded();
            data.level += 1;
            for (int i = 0; i < products.Length; i++)
            {
                if (!data.unlocked[i] && data.level >= products[i].UnlockLevel)
                {
                    data.unlocked[i] = true;
                    AddLog(F("log.product_unlocked", ProductName(i)));
                }
            }
        }

        return true;
    }

    private void CompleteOrderManualAndRender()
    {
        CompleteCurrentOrder(true);
        CheckMilestones();
        RenderAll();
    }

    private void SelectOrderProductAndRender(int productIndex)
    {
        EnsureCurrentOrder();
        if (productIndex < 0 || productIndex >= products.Length || CurrentOrderQuantity(productIndex) <= 0)
        {
            AddLog(T("order.waiting"));
            RenderAll();
            return;
        }

        data.selectedOrderProductIndex = productIndex;
        orderSelectPulseTimer = 0.42f;
        AddLog(F("log.focus_product", ProductName(productIndex)));
        RenderAll();
    }

    private void EnsureCurrentOrder()
    {
        if (data.currentOrder == null || data.currentOrder.Length != products.Length)
        {
            data.currentOrder = Resize(data.currentOrder, products.Length);
        }

        if (data.currentOrderInitial == null || data.currentOrderInitial.Length != products.Length)
        {
            data.currentOrderInitial = Resize(data.currentOrderInitial, products.Length);
        }

        if (pendingOrderCompletion)
        {
            return;
        }

        if (CurrentOrderItemCount() <= 0 || !CurrentOrderUsesUnlockedProducts())
        {
            GenerateCurrentOrder();
            newOrderFlashTimer = CustomerEnterDurationSeconds;
        }
    }

    private void GenerateCurrentOrder()
    {
        pendingOrderCompletion = false;
        data.currentOrderEarned = 0;
        orderCompleteTitle = string.Empty;
        data.selectedOrderProductIndex = -1;
        for (int i = 0; i < data.currentOrder.Length; i++)
        {
            data.currentOrder[i] = 0;
            data.currentOrderInitial[i] = 0;
        }

        int unlockedCount = UnlockedProductCount();
        if (unlockedCount <= 0)
        {
            data.currentOrder[0] = 1;
            data.currentOrderInitial[0] = 1;
            return;
        }

        int maxKinds = Mathf.Min(unlockedCount, MaxOrderKinds());
        int minKinds = EarlyOrderSingleProductOnly() || unlockedCount < 2 ? 1 : 2;
        int targetKinds = EarlyOrderSingleProductOnly()
            ? 1
            : Mathf.Clamp(minKinds + (UnityEngine.Random.value < ComboOrderChance() ? 1 : 0) + (UnityEngine.Random.value < ComboOrderChance() * 0.45f ? 1 : 0), minKinds, maxKinds);
        int guard = 0;
        while (OrderDistinctProductCount() < targetKinds && guard < 40)
        {
            guard++;
            int index = ChooseOrderProductIndex();
            if (index < 0)
            {
                break;
            }

            int maxQty = Mathf.Max(1, products[index].MaxOrderQty + (data.level >= 5 ? 1 : 0));
            if (EarlyOrderSingleProductOnly())
            {
                maxQty = Mathf.Min(maxQty, data.completedOrders <= 0 ? 1 : 2);
            }

            int quantity = UnityEngine.Random.Range(1, maxQty + 1);
            data.currentOrder[index] = Mathf.Max(data.currentOrder[index], quantity);
        }

        if (CurrentOrderItemCount() <= 0)
        {
            int fallback = FirstUnlockedProductIndex();
            data.currentOrder[fallback] = 1;
        }

        for (int i = 0; i < data.currentOrder.Length; i++)
        {
            data.currentOrderInitial[i] = data.currentOrder[i];
        }

        data.selectedOrderProductIndex = CurrentOrderDisplayProductIndex();
    }

    private bool EarlyOrderSingleProductOnly()
    {
        return data.completedOrders < 3 && data.level <= 1;
    }

    private int ChooseOrderProductIndex()
    {
        float total = 0f;
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i] && data.currentOrder[i] <= 0)
            {
                total += ProductOrderDemandWeight(i);
            }
        }

        if (total <= 0f)
        {
            return -1;
        }

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i] || data.currentOrder[i] > 0)
            {
                continue;
            }

            roll -= ProductOrderDemandWeight(i);
            if (roll <= 0f)
            {
                return i;
            }
        }

        return 0;
    }

    private float ProductOrderDemandWeight(int productIndex)
    {
        float premium = Mathf.InverseLerp(products[0].Price, products[products.Length - 1].Price, products[productIndex].Price);
        float promotionQuality = StaffLevel(2) * 0.024f;
        float merchandisingQuality = StaffLevel(3) * 0.014f;
        float priceUpgradeQuality = data.upgrades[2] * 0.018f;
        float qualityBoost = 1f + premium * (priceUpgradeQuality + promotionQuality + merchandisingQuality);
        float stockConfidence = data.stock[productIndex] > 0f ? 1.08f : 0.86f;
        float newPlayerBias = data.completedOrders < 8 && productIndex > 1 ? 0.72f : 1f;
        return Mathf.Max(0.01f, products[productIndex].Demand * ProductOrderFrequencyMultiplier(productIndex) * qualityBoost * stockConfidence * newPlayerBias);
    }

    private bool CurrentOrderUsesUnlockedProducts()
    {
        for (int i = 0; i < products.Length; i++)
        {
            if (CurrentOrderQuantity(i) > 0 && !data.unlocked[i])
            {
                return false;
            }
        }

        return true;
    }

    private int FirstUnlockedProductIndex()
    {
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i])
            {
                return i;
            }
        }

        return 0;
    }

    private int UnlockedProductCount()
    {
        int count = 0;
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i])
            {
                count++;
            }
        }

        return count;
    }

    private int OrderDistinctProductCount()
    {
        int count = 0;
        for (int i = 0; i < products.Length; i++)
        {
            if (CurrentOrderInitialQuantity(i) > 0)
            {
                count++;
            }
        }

        return count;
    }

    private int CurrentOrderQuantity(int productIndex)
    {
        return data.currentOrder != null && productIndex >= 0 && productIndex < data.currentOrder.Length ? Mathf.Max(0, data.currentOrder[productIndex]) : 0;
    }

    private int CurrentOrderInitialQuantity(int productIndex)
    {
        int initial = data.currentOrderInitial != null && productIndex >= 0 && productIndex < data.currentOrderInitial.Length ? Mathf.Max(0, data.currentOrderInitial[productIndex]) : 0;
        return Mathf.Max(initial, CurrentOrderQuantity(productIndex));
    }

    private int CurrentOrderCompletedQuantity(int productIndex)
    {
        return Mathf.Clamp(CurrentOrderInitialQuantity(productIndex) - CurrentOrderQuantity(productIndex), 0, CurrentOrderInitialQuantity(productIndex));
    }

    private int CurrentOrderItemCount()
    {
        int count = 0;
        for (int i = 0; i < products.Length; i++)
        {
            count += CurrentOrderQuantity(i);
        }

        return count;
    }

    private int CurrentOrderInitialItemCount()
    {
        int count = 0;
        for (int i = 0; i < products.Length; i++)
        {
            count += CurrentOrderInitialQuantity(i);
        }

        return count;
    }

    private int CurrentOrderCompletedItemCount()
    {
        int count = 0;
        for (int i = 0; i < products.Length; i++)
        {
            count += CurrentOrderCompletedQuantity(i);
        }

        return count;
    }

    private float CurrentOrderCompletionProgress()
    {
        int initial = CurrentOrderInitialItemCount();
        if (initial <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(CurrentOrderCompletedItemCount() / (float)initial);
    }

    private int CurrentOrderValue()
    {
        int value = 0;
        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderQuantity(i);
            if (quantity > 0)
            {
                value += SalePrice(i) * quantity;
            }
        }

        return Mathf.FloorToInt(value * OrderValueMultiplier());
    }

    private int CurrentOrderInitialValue()
    {
        int value = 0;
        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderInitialQuantity(i);
            if (quantity > 0)
            {
                value += SalePrice(i) * quantity;
            }
        }

        return Mathf.FloorToInt(value * OrderValueMultiplier());
    }

    private int CurrentOrderItemValue(int productIndex)
    {
        return productIndex >= 0 && productIndex < products.Length ? Mathf.FloorToInt(SalePrice(productIndex) * OrderValueMultiplier()) : 0;
    }

    private int CurrentOrderNextItemValue()
    {
        return CurrentOrderItemValue(CurrentOrderDisplayProductIndex());
    }

    private bool CanFulfillCurrentOrder()
    {
        EnsureCurrentOrder();
        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderQuantity(i);
            if (quantity > 0 && data.stock[i] < quantity)
            {
                return false;
            }
        }

        return true;
    }

    private string CurrentOrderSummary()
    {
        EnsureCurrentOrder();
        var parts = new List<string>();
        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderInitialQuantity(i);
            if (quantity > 0)
            {
                parts.Add(F("order.item", ProductName(i), quantity));
            }
        }

        return parts.Count > 0 ? string.Join(" + ", parts) : T("order.waiting");
    }

    private string OrderCompletionTitle(string orderSummary)
    {
        int distinctCount = OrderDistinctProductCount();
        if (distinctCount == 1)
        {
            for (int i = 0; i < products.Length; i++)
            {
                if (CurrentOrderInitialQuantity(i) > 0)
                {
                    return ProductName(i);
                }
            }
        }

        return string.IsNullOrEmpty(orderSummary) ? T("order.waiting") : orderSummary;
    }

    private string CurrentOrderShortageText()
    {
        EnsureCurrentOrder();
        int selected = data.selectedOrderProductIndex;
        if (selected >= 0 && selected < products.Length)
        {
            int selectedQuantity = CurrentOrderQuantity(selected);
            int selectedStock = Mathf.FloorToInt(data.stock[selected]);
            if (selectedQuantity > 0 && selectedStock < selectedQuantity)
            {
                return F("order.shortage", ProductName(selected), selectedQuantity - selectedStock);
            }
        }

        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderQuantity(i);
            int stock = Mathf.FloorToInt(data.stock[i]);
            if (quantity > 0 && stock < quantity)
            {
                return F("order.shortage", ProductName(i), quantity - stock);
            }
        }

        return T("log.empty_shelf");
    }

    private string CurrentOrderFocusText()
    {
        EnsureCurrentOrder();
        int productIndex = CurrentOrderDisplayProductIndex();
        int quantity = CurrentOrderQuantity(productIndex);
        if (productIndex < 0 || quantity <= 0)
        {
            return T("order.waiting");
        }

        int stock = Mathf.FloorToInt(data.stock[productIndex]);
        if (stock <= 0)
        {
            return F("order.focus_blocked", ProductName(productIndex), quantity);
        }

        return F("order.focus", ProductName(productIndex), quantity, stock);
    }

    private string CurrentOrderStepText()
    {
        EnsureCurrentOrder();
        int productIndex = CurrentOrderDisplayProductIndex();
        if (productIndex < 0 || productIndex >= products.Length)
        {
            return T("order.waiting");
        }

        int quantity = CurrentOrderQuantity(productIndex);
        int stock = Mathf.FloorToInt(data.stock[productIndex]);
        int missing = Mathf.Max(0, quantity - stock);
        if (quantity <= 0)
        {
            return T("order.waiting");
        }

        if (missing > 0)
        {
            return F("order.step_blocked", ProductName(productIndex), missing);
        }

        return F("order.step_selling", ProductName(productIndex), quantity, stock);
    }

    private string CurrentOrderNextStepText()
    {
        EnsureCurrentOrder();
        int productIndex = CurrentOrderDisplayProductIndex();
        if (productIndex < 0 || productIndex >= products.Length)
        {
            return T("order.waiting");
        }

        int quantity = CurrentOrderQuantity(productIndex);
        int stock = Mathf.FloorToInt(data.stock[productIndex]);
        int missing = Mathf.Max(0, quantity - stock);
        if (missing > 0)
        {
            int unitCost = UnitCost(productIndex);
            int affordable = Mathf.Min(missing, Mathf.FloorToInt(data.cash / Mathf.Max(1, unitCost)));
            if (affordable > 0)
            {
                return F("order.next_restock", ProductName(productIndex), affordable, Money(affordable * unitCost));
            }

            return F("order.next_wait_cash", Money(Mathf.Max(0f, unitCost - data.cash)));
        }

        if (quantity > 0 && stock > 0)
        {
            return F("order.next_checkout", ProductName(productIndex), Money(CurrentOrderItemValue(productIndex)));
        }

        return T("order.waiting");
    }

    private int CurrentOrderServeIndex()
    {
        EnsureCurrentOrder();
        int selected = data.selectedOrderProductIndex;
        if (selected >= 0 && selected < products.Length && CurrentOrderQuantity(selected) > 0)
        {
            return data.stock[selected] >= 1f ? selected : -1;
        }

        for (int i = 0; i < products.Length; i++)
        {
            if (CurrentOrderQuantity(i) > 0 && data.stock[i] >= 1f)
            {
                return i;
            }
        }

        return -1;
    }

    private int CurrentOrderDisplayProductIndex()
    {
        if (pendingOrderCompletion && recentSoldProductIndex >= 0 && recentSoldProductIndex < products.Length)
        {
            return recentSoldProductIndex;
        }

        int selected = data.selectedOrderProductIndex;
        if (selected >= 0 && selected < products.Length && CurrentOrderQuantity(selected) > 0)
        {
            return selected;
        }

        int serveIndex = CurrentOrderServeIndex();
        if (serveIndex >= 0)
        {
            return serveIndex;
        }

        int missingIndex = BestMissingOrderProductIndex(data.stock);
        if (missingIndex >= 0)
        {
            return missingIndex;
        }

        for (int i = 0; i < products.Length; i++)
        {
            if (CurrentOrderQuantity(i) > 0)
            {
                return i;
            }
        }

        return FirstUnlockedProductIndex();
    }

    private int CurrentOrderMissingAmount(int productIndex)
    {
        if (productIndex < 0 || productIndex >= products.Length)
        {
            return 0;
        }

        return Mathf.Max(0, CurrentOrderQuantity(productIndex) - Mathf.FloorToInt(data.stock[productIndex]));
    }

    private int CurrentOrderMissingCost()
    {
        int cost = 0;
        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderQuantity(i);
            int stock = Mathf.FloorToInt(data.stock[i]);
            if (quantity > stock)
            {
                cost += (quantity - stock) * UnitCost(i);
            }
        }

        return cost;
    }

    private float OrderProgressPerSecond()
    {
        return 1f / Mathf.Max(1f, CheckoutDurationSeconds());
    }

    private float CheckoutDurationSeconds()
    {
        int productIndex = CurrentOrderServeIndex();
        if (productIndex < 0)
        {
            productIndex = CurrentOrderDisplayProductIndex();
        }

        return CheckoutDurationSeconds(productIndex);
    }

    private float CheckoutDurationSeconds(int productIndex)
    {
        return CheckoutDurationSecondsFor(productIndex, StaffLevel(0), StaffLevel(5), data.upgrades[1]);
    }

    private float CheckoutDurationSecondsFor(int productIndex, int cashierLevel, int managerLevel, int speedUpgradeLevel)
    {
        float staffSpeed = cashierLevel * 0.08f + managerLevel * 0.035f;
        float upgradeSpeed = speedUpgradeLevel * 0.055f;
        float duration = BaseCheckoutDurationSeconds() * ProductCheckoutDurationMultiplier(productIndex) / (1f + staffSpeed + upgradeSpeed);
        return Mathf.Clamp(duration, 4.2f, 13f);
    }

    private float BaseCheckoutDurationSeconds()
    {
        return data.completedOrders < 3 ? 11.5f : data.completedOrders < 8 ? 9.6f : 8.2f;
    }

    private float OrderValueMultiplier()
    {
        return 1f + StaffLevel(3) * 0.018f + StaffLevel(5) * 0.025f;
    }

    private float OfflineCapSecondsLimit()
    {
        return OfflineCapSeconds * (1f + StaffLevel(5) * 0.08f);
    }

    private int ProductTier(int productIndex)
    {
        if (productIndex >= 6)
        {
            return 2;
        }

        return productIndex >= 3 ? 1 : 0;
    }

    private string ProductTierLabel(int productIndex)
    {
        switch (ProductTier(productIndex))
        {
            case 0:
                return T("product.tier.fast");
            case 1:
                return T("product.tier.profit");
            default:
                return T("product.tier.premium");
        }
    }

    private string ProductTierShortLabel(int productIndex)
    {
        switch (ProductTier(productIndex))
        {
            case 0:
                return T("product.tier.fast_short");
            case 1:
                return T("product.tier.profit_short");
            default:
                return T("product.tier.premium_short");
        }
    }

    private float ProductCheckoutDurationMultiplier(int productIndex)
    {
        switch (ProductTier(productIndex))
        {
            case 0:
                return 0.84f;
            case 1:
                return 1.02f;
            default:
                return 1.22f;
        }
    }

    private float ProductOrderFrequencyMultiplier(int productIndex)
    {
        switch (ProductTier(productIndex))
        {
            case 0:
                return 1.12f;
            case 1:
                return 0.92f + StaffLevel(3) * 0.004f;
            default:
                return 0.58f + StaffLevel(2) * 0.006f + StaffLevel(3) * 0.004f;
        }
    }

    private int MaxOrderKinds()
    {
        int baseKinds = data.level >= 4 ? 3 : data.level >= 2 ? 2 : 2;
        return Mathf.Clamp(baseKinds + (StaffLevel(3) >= 8 ? 1 : 0), 1, 4);
    }

    private float ComboOrderChance()
    {
        return Mathf.Clamp01(0.26f + data.level * 0.018f + StaffLevel(3) * 0.018f);
    }

    private int EstimatedIncomePerMinute()
    {
        EnsureCurrentOrder();
        if (CurrentOrderServeIndex() < 0)
        {
            return 0;
        }

        return Mathf.FloorToInt(OrderProgressPerSecond() * 60f * Mathf.Max(1, CurrentOrderNextItemValue()));
    }

    private int StaffLevel(int index)
    {
        return data.staff != null && index >= 0 && index < data.staff.Length ? data.staff[index] : 0;
    }

    private float TrafficPerSecond()
    {
        return OrderProgressPerSecond();
    }

    private float TrafficPerSecondWithStaffLevel(int staffIndex, int level)
    {
        int cashierLevel = staffIndex == 0 ? level : StaffLevel(0);
        int managerLevel = staffIndex == 5 ? level : StaffLevel(5);
        float duration = CheckoutDurationSecondsFor(CurrentOrderDisplayProductIndex(), cashierLevel, managerLevel, data.upgrades[1]);
        return 1f / duration;
    }

    private float AutoSaleInterval()
    {
        return 1f / Mathf.Max(0.01f, TrafficPerSecond());
    }

    private bool HasSellableStock()
    {
        if (pendingOrderCompletion)
        {
            return false;
        }

        return CurrentOrderServeIndex() >= 0;
    }

    private float NextAutoSaleSeconds()
    {
        if (!HasSellableStock())
        {
            return -1f;
        }

        float rate = Mathf.Max(0.01f, OrderProgressPerSecond());
        return Mathf.Max(0f, (1f - Mathf.Clamp01(data.queue)) / rate);
    }

    private float XpNeeded()
    {
        return 220f + data.level * 80f;
    }

    private int MaxStock(int productIndex)
    {
        return products[productIndex].BaseMaxStock + data.upgrades[0] * 4 + StaffLevel(1);
    }

    private int SalePrice(int productIndex)
    {
        float upgraded = products[productIndex].Price * Mathf.Pow(1.16f, data.upgrades[2]);
        float managed = upgraded * (1f + StaffLevel(5) * 0.025f);
        return Mathf.FloorToInt(managed);
    }

    private float PriceUpgradeBonus(int productIndex)
    {
        return Mathf.Max(2f, products[productIndex].Price * 0.12f);
    }

    private int UnitCost(int productIndex)
    {
        float upgraded = products[productIndex].Cost * Mathf.Pow(0.94f, data.upgrades[3]);
        float purchased = upgraded * (1f - StaffLevel(4) * 0.025f);
        return Mathf.Max(1, Mathf.FloorToInt(purchased));
    }

    private int RestockPack()
    {
        return 4 + StaffLevel(1) * 2;
    }

    private int PrimaryProductIndex()
    {
        return CurrentOrderDisplayProductIndex();
    }

    private int BestRestockIndex()
    {
        int missingOrderIndex = BestMissingOrderProductIndex(data.stock);
        if (missingOrderIndex >= 0)
        {
            return missingOrderIndex;
        }

        int bestIndex = -1;
        float bestRatio = 2f;
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i])
            {
                continue;
            }

            int maxStock = MaxStock(i);
            if (data.stock[i] >= maxStock)
            {
                continue;
            }

            float ratio = data.stock[i] / Mathf.Max(1, maxStock);
            if (ratio < bestRatio)
            {
                bestRatio = ratio;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private float LowestUnlockedStockRatio()
    {
        float lowest = 1f;
        bool found = false;
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i])
            {
                continue;
            }

            found = true;
            lowest = Mathf.Min(lowest, data.stock[i] / Mathf.Max(1, MaxStock(i)));
        }

        return found ? lowest : 0f;
    }

    private void CalculateRestockAllPlan(out int totalAmount, out int totalCost)
    {
        totalAmount = 0;
        totalCost = 0;
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i])
            {
                continue;
            }

            int amount = MaxStock(i) - Mathf.FloorToInt(data.stock[i]);
            if (amount <= 0)
            {
                continue;
            }

            totalAmount += amount;
            totalCost += amount * UnitCost(i);
        }
    }

    private void CalculateAffordableRestockPlan(out int totalAmount, out int totalCost)
    {
        totalAmount = 0;
        totalCost = 0;
        float remainingCash = data.cash;
        float[] simulatedStock = new float[data.stock.Length];
        Array.Copy(data.stock, simulatedStock, data.stock.Length);

        int guard = 0;
        while (remainingCash >= 1f && guard < 10000)
        {
            guard++;
            int index = BestRestockIndex(simulatedStock);
            if (index < 0)
            {
                break;
            }

            int cost = UnitCost(index);
            if (remainingCash < cost)
            {
                break;
            }

            remainingCash -= cost;
            simulatedStock[index] += 1f;
            totalAmount += 1;
            totalCost += cost;
        }
    }

    private int PriorityRestockAmount(int productIndex)
    {
        if (productIndex < 0 || productIndex >= products.Length || !data.unlocked[productIndex])
        {
            return 0;
        }

        int capacity = MaxStock(productIndex) - Mathf.FloorToInt(data.stock[productIndex]);
        if (capacity <= 0)
        {
            return 0;
        }

        int missingForOrder = CurrentOrderMissingAmount(productIndex);
        int target = missingForOrder > 0 ? missingForOrder : RestockPack();
        return Mathf.Min(capacity, Mathf.Max(1, target));
    }

    private void CalculateAffordablePriorityRestockPlan(int productIndex, out int amount, out int cost)
    {
        amount = 0;
        cost = 0;
        int target = PriorityRestockAmount(productIndex);
        if (target <= 0)
        {
            return;
        }

        int unitCost = UnitCost(productIndex);
        amount = Mathf.Min(target, Mathf.FloorToInt(data.cash / Mathf.Max(1, unitCost)));
        cost = amount * unitCost;
    }

    private int BestRestockIndex(float[] stock)
    {
        int missingOrderIndex = BestMissingOrderProductIndex(stock);
        if (missingOrderIndex >= 0)
        {
            return missingOrderIndex;
        }

        int bestIndex = -1;
        float bestRatio = 2f;
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i])
            {
                continue;
            }

            int maxStock = MaxStock(i);
            if (stock[i] >= maxStock)
            {
                continue;
            }

            float ratio = stock[i] / Mathf.Max(1, maxStock);
            if (ratio < bestRatio)
            {
                bestRatio = ratio;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private int BestMissingOrderProductIndex(float[] stock)
    {
        int selected = data.selectedOrderProductIndex;
        if (selected >= 0 && selected < products.Length && data.unlocked[selected])
        {
            int selectedMissing = CurrentOrderQuantity(selected) - Mathf.FloorToInt(stock[selected]);
            if (selectedMissing > 0)
            {
                return selected;
            }
        }

        int bestIndex = -1;
        int largestMissing = 0;
        for (int i = 0; i < products.Length; i++)
        {
            int missing = CurrentOrderQuantity(i) - Mathf.FloorToInt(stock[i]);
            if (data.unlocked[i] && missing > largestMissing)
            {
                largestMissing = missing;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private int CheapestRestockCost()
    {
        int cheapest = int.MaxValue;
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i] || data.stock[i] >= MaxStock(i))
            {
                continue;
            }

            cheapest = Mathf.Min(cheapest, UnitCost(i));
        }

        return cheapest == int.MaxValue ? 0 : cheapest;
    }

    private int BestUpgradeIndex()
    {
        int[] priority = { 2, 1, 0, 3 };
        for (int i = 0; i < priority.Length; i++)
        {
            int index = priority[i];
            if (index >= upgrades.Length || data.upgrades[index] >= upgrades[index].MaxLevel)
            {
                continue;
            }

            int cost = UpgradeCost(index);
            if (data.cash >= cost)
            {
                return index;
            }
        }

        for (int i = 0; i < priority.Length; i++)
        {
            int index = priority[i];
            if (index < upgrades.Length && data.upgrades[index] < upgrades[index].MaxLevel)
            {
                return index;
            }
        }

        return -1;
    }

    private int BestStaffIndex()
    {
        int bestIndex = -1;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < staffDefs.Length; i++)
        {
            if (data.staff[i] >= staffDefs[i].MaxLevel)
            {
                continue;
            }

            int cost = StaffCost(i);
            float affordability = data.cash >= cost ? 1f : -Mathf.Clamp01((cost - data.cash) / Mathf.Max(1f, cost));
            float score = StaffPriorityScore(i) + affordability * 0.55f - StaffLevel(i) * 0.035f;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private float StaffPriorityScore(int staffIndex)
    {
        float stockRatio = LowestUnlockedStockRatio();
        int unlockedCount = UnlockedProductCount();
        switch (staffIndex)
        {
            case 0:
                return 1.15f + Mathf.Min(0.65f, data.completedOrders * 0.018f);
            case 1:
                return CurrentOrderMissingCost() > 0 ? 2.75f : stockRatio < 0.35f ? 2.05f : 0.92f;
            case 2:
                return data.completedOrders >= 6 ? 1.34f + unlockedCount * 0.06f : 0.72f;
            case 3:
                return unlockedCount >= 3 ? 1.48f + Mathf.Clamp01(ComboOrderChance()) * 0.45f : 0.62f;
            case 4:
                return unlockedCount >= 4 ? 1.32f + Mathf.Clamp01(CheapestRestockCost() / 24f) * 0.45f : 0.55f;
            case 5:
                return data.completedOrders >= 10 ? 1.42f + (StaffLevel(0) + StaffLevel(3)) * 0.025f : 0.48f;
            default:
                return 0f;
        }
    }

    private bool ShouldRecommendStaff()
    {
        return data.upgrades[2] > 0 && (data.level >= 3 || TotalSoldCount() >= StaffRecommendationMinSold || data.completedOrders >= 8);
    }

    private int UpgradeCost(int upgradeIndex)
    {
        return Mathf.FloorToInt(upgrades[upgradeIndex].BaseCost * Mathf.Pow(1.86f, data.upgrades[upgradeIndex]));
    }

    private int StaffCost(int staffIndex)
    {
        return Mathf.FloorToInt(staffDefs[staffIndex].BaseCost * Mathf.Pow(1.78f, data.staff[staffIndex]));
    }

    private int InitialStock(int productIndex)
    {
        if (productIndex == 0)
        {
            return 4;
        }

        if (productIndex == 1)
        {
            return 2;
        }

        return 1;
    }

    private void BuyStock(int productIndex)
    {
        if (!data.unlocked[productIndex])
        {
            return;
        }

        int capacity = MaxStock(productIndex) - Mathf.FloorToInt(data.stock[productIndex]);
        int amount = Mathf.Min(capacity, RestockPack());
        if (amount <= 0)
        {
            AddLog(F("log.product_full", ProductName(productIndex)));
            RenderAll();
            return;
        }

        int cost = amount * UnitCost(productIndex);
        if (data.cash < cost)
        {
            AddLog(F("log.cash_short", Money(cost - data.cash)));
            RenderAll();
            return;
        }

        data.cash -= cost;
        data.stock[productIndex] += amount;
        data.restockActions += 1;
        restockPulseTimer = 0.65f;
        recentRestockProductIndex = productIndex;
        AddRestockLog(productIndex, amount);
        CheckMilestones();
        RenderAll();
    }

    private void BuyRestockAll()
    {
        int index = BestRestockIndex();
        if (index < 0)
        {
            AddLog(T("action.full_stock"));
            RenderAll();
            return;
        }

        int targetAmount = PriorityRestockAmount(index);
        int unitCost = UnitCost(index);
        int bought = Mathf.Min(targetAmount, Mathf.FloorToInt(data.cash / Mathf.Max(1, unitCost)));
        if (bought <= 0)
        {
            AddLog(F("log.cash_short", Money(Mathf.Max(0f, unitCost - data.cash))));
            RenderAll();
            return;
        }

        data.cash -= bought * unitCost;
        data.stock[index] += bought;

        restockPulseTimer = 0.65f;
        recentRestockProductIndex = index;
        data.restockActions += 1;
        AddRestockLog(index, bought);
        CheckMilestones();
        RenderAll();
    }

    private void AddRestockLog(int productIndex, int amount)
    {
        bool affectsCurrentOrder = CurrentOrderQuantity(productIndex) > 0;
        bool canContinueOrder = affectsCurrentOrder && data.stock[productIndex] >= 1f;
        if (canContinueOrder)
        {
            AddLog(F("log.restocked_order_ready", ProductName(productIndex), amount));
            return;
        }

        AddLog(F("log.restocked", ProductName(productIndex), amount));
    }

    private void BuyUpgrade(int upgradeIndex)
    {
        if (data.upgrades[upgradeIndex] >= upgrades[upgradeIndex].MaxLevel)
        {
            return;
        }

        int cost = UpgradeCost(upgradeIndex);
        if (data.cash < cost)
        {
            AddLog(F("log.upgrade_short", UpgradeName(upgradeIndex), Money(cost - data.cash)));
            RenderAll();
            return;
        }

        data.cash -= cost;
        data.upgrades[upgradeIndex] += 1;
        upgradePulseTimer = 0.75f;
        AddLog(F("log.upgraded", UpgradeName(upgradeIndex), data.upgrades[upgradeIndex]));
        CheckMilestones();
        RenderAll();
    }

    private void HireStaff(int staffIndex)
    {
        if (data.staff[staffIndex] >= staffDefs[staffIndex].MaxLevel)
        {
            return;
        }

        int cost = StaffCost(staffIndex);
        if (data.cash < cost)
        {
            AddLog(F("log.staff_short", StaffName(staffIndex), Money(cost - data.cash)));
            RenderAll();
            return;
        }

        data.cash -= cost;
        data.staff[staffIndex] += 1;
        AddLog(F("log.staff_upgraded", StaffName(staffIndex), data.staff[staffIndex]));
        CheckMilestones();
        RenderAll();
    }

    private void Advertise()
    {
        int cost = Mathf.FloorToInt(18f + data.level * 4f + data.reputation * 1.2f);
        if (data.cash < cost)
        {
            AddLog(F("log.ad_short", Money(cost - data.cash)));
            RenderAll();
            return;
        }

        data.cash -= cost;
        data.queue += 0.35f + StaffLevel(2) * 0.015f;
        data.reputation += 0.08f;
        AddLog(T("log.advertised"));
        RenderAll();
    }

    private void AddLog(string message)
    {
        log.Insert(0, message);
        while (log.Count > 5)
        {
            log.RemoveAt(log.Count - 1);
        }
    }

    private string T(string key)
    {
        return LocalizationManager.T(key);
    }

    private string F(string key, params object[] args)
    {
        return LocalizationManager.F(key, args);
    }

    private string ProductName(int index)
    {
        return T(products[index].NameKey);
    }

    private string ProductSubtitle(int index)
    {
        return T(products[index].SubtitleKey);
    }

    private string UpgradeName(int index)
    {
        return T(upgrades[index].NameKey);
    }

    private string UpgradeDetail(int index)
    {
        return T(upgrades[index].DetailKey);
    }

    private string UpgradeImpactText(int upgradeIndex, int productIndex)
    {
        switch (upgradeIndex)
        {
            case 0:
                return F("recommend.upgrade_stock", 5);
            case 1:
            {
                float current = OrderProgressPerSecond();
                float nextDuration = CheckoutDurationSecondsFor(productIndex, StaffLevel(0), StaffLevel(5), data.upgrades[1] + 1);
                float next = 1f / nextDuration;
                int extraOrders = Mathf.Max(1, Mathf.RoundToInt((next - current) * 60f));
                return F("recommend.upgrade_speed", extraOrders);
            }
            case 2:
            {
                int currentPrice = SalePrice(productIndex);
                int nextPrice = Mathf.FloorToInt(products[productIndex].Price * Mathf.Pow(1.16f, data.upgrades[2] + 1) * (1f + StaffLevel(5) * 0.025f));
                return F("recommend.upgrade_price", Money(Mathf.Max(1, nextPrice - currentPrice)));
            }
            case 3:
            {
                int currentCost = UnitCost(productIndex);
                int nextCost = Mathf.Max(1, Mathf.FloorToInt(products[productIndex].Cost * Mathf.Pow(0.94f, data.upgrades[3] + 1) * (1f - StaffLevel(4) * 0.025f)));
                return F("recommend.upgrade_cost", Money(Mathf.Max(1, currentCost - nextCost)));
            }
            default:
                return UpgradeDetail(upgradeIndex);
        }
    }

    private string ProductEconomyText(int productIndex)
    {
        int price = SalePrice(productIndex);
        int cost = UnitCost(productIndex);
        return $"{ProductTierLabel(productIndex)} · {F("stock.profit_line", Money(Mathf.Max(0, price - cost)), Money(price), Money(cost))}";
    }

    private string StaffName(int index)
    {
        return T(staffDefs[index].NameKey);
    }

    private string StaffDetail(int index)
    {
        return T(staffDefs[index].DetailKey);
    }

    private string StaffAutomationText(int index)
    {
        switch (index)
        {
            case 0:
            {
                float current = TrafficPerSecond();
                float next = TrafficPerSecondWithStaffLevel(index, StaffLevel(index) + 1);
                int extraOrders = Mathf.Max(1, Mathf.RoundToInt((next - current) * 60f));
                return F("staff.cashier.automation", extraOrders);
            }
            case 1:
                return F("staff.stocker.automation", RestockPack(), RestockPack() + 2);
            case 2:
            {
                int nextReputationBoost = Mathf.RoundToInt((StaffLevel(index) + 1) * 12f);
                return F("staff.promoter.automation", $"{nextReputationBoost}%");
            }
            case 3:
                return F("staff.merchandiser.automation", Mathf.RoundToInt(ComboOrderChance() * 100f), Mathf.RoundToInt(Mathf.Clamp01(ComboOrderChance() + 0.018f) * 100f));
            case 4:
                return F("staff.purchaser.automation", Money(UnitCost(PrimaryProductIndex())), Money(Mathf.Max(1, Mathf.FloorToInt(products[PrimaryProductIndex()].Cost * Mathf.Pow(0.94f, data.upgrades[3]) * (1f - (StaffLevel(4) + 1) * 0.025f)))));
            case 5:
            {
                string currentHours = (OfflineCapSecondsLimit() / 3600f).ToString("0.0", CultureInfo.InvariantCulture);
                string nextHours = ((OfflineCapSeconds * (1f + (StaffLevel(index) + 1) * 0.08f)) / 3600f).ToString("0.0", CultureInfo.InvariantCulture);
                return F("staff.manager.automation", currentHours, nextHours);
            }
            default:
                return StaffDetail(index);
        }
    }

    private string ShortCashShort(float amount)
    {
        return F("action.cash_short", Money(Mathf.Max(0f, amount)));
    }

    private string HeaderStockText()
    {
        int stock = TotalStockCount();
        int capacity = TotalStockCapacity();
        if (stock <= 0)
        {
            return F("stock.empty_count", stock, capacity);
        }

        if (stock / (float)capacity <= LowStockRatio)
        {
            return F("stock.low_count", stock, capacity);
        }

        return F("stock.stock_count", stock, capacity);
    }

    private bool IsRtlLanguage()
    {
        return LocalizationManager.CurrentLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
    }

    private Font CurrentFont()
    {
        if (IsRtlLanguage() && arabicFont != null)
        {
            return arabicFont;
        }

        return mainFont != null ? mainFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private TextAnchor LocalizedAnchor(TextAnchor requested)
    {
        if (!IsRtlLanguage())
        {
            return requested;
        }

        if (requested == TextAnchor.UpperLeft)
        {
            return TextAnchor.UpperRight;
        }

        if (requested == TextAnchor.MiddleLeft)
        {
            return TextAnchor.MiddleRight;
        }

        if (requested == TextAnchor.LowerLeft)
        {
            return TextAnchor.LowerRight;
        }

        return requested;
    }

    private Sprite ProductSprite(int index)
    {
        return productSprites != null && index >= 0 && index < productSprites.Length ? productSprites[index] : null;
    }

    private Sprite StaffSprite(int index)
    {
        return staffSprites != null && index >= 0 && index < staffSprites.Length ? staffSprites[index] : null;
    }

    private Sprite Mvp23Sprite(int index)
    {
        return mvp23Sprites != null && index >= 0 && index < mvp23Sprites.Length ? mvp23Sprites[index] : null;
    }

    private Sprite CustomerWalkSprite(float time)
    {
        int frame = Mathf.FloorToInt(time * 4f) % 4;
        switch (frame)
        {
            case 0:
                return FirstSprite(Mvp23Sprite(Mvp30CustomerWalk01), Mvp23Sprite(Mvp23CustomerWalk01), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
            case 1:
                return FirstSprite(Mvp23Sprite(Mvp30CustomerWalk02), Mvp23Sprite(Mvp23CustomerWalk02), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
            case 2:
                return FirstSprite(Mvp23Sprite(Mvp30CustomerWalk03), Mvp23Sprite(Mvp23CustomerWalk01), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
            default:
                return FirstSprite(Mvp23Sprite(Mvp30CustomerWalk04), Mvp23Sprite(Mvp23CustomerWalk02), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
        }
    }

    private CustomerSceneState CurrentCustomerSceneState(bool showingOrderComplete, bool hasSellableStock, bool paying, bool entering)
    {
        if (showingOrderComplete)
        {
            return CustomerSceneState.HappyLeaving;
        }

        if (entering)
        {
            return CustomerSceneState.Entering;
        }

        if (paying)
        {
            return CustomerSceneState.Paying;
        }

        return hasSellableStock ? CustomerSceneState.Waiting : CustomerSceneState.Blocked;
    }

    private static Sprite FirstSprite(params Sprite[] sprites)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                return sprites[i];
            }
        }

        return null;
    }

    private string Money(float value)
    {
        string symbol = T("currency.symbol");
        if (value >= 1000000f)
        {
            return symbol + (value / 1000000f).ToString("0.0", CultureInfo.InvariantCulture) + "M";
        }

        if (LocalizationManager.CurrentLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase) && value >= 10000f)
        {
            return symbol + (value / 10000f).ToString("0.0", CultureInfo.InvariantCulture) + T("number.ten_thousand");
        }

        if (value >= 1000f)
        {
            return symbol + (value / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + "K";
        }

        return symbol + Mathf.FloorToInt(value).ToString(CultureInfo.InvariantCulture);
    }

    private string Compact(float value)
    {
        if (LocalizationManager.CurrentLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase) && value >= 10000f)
        {
            return (value / 10000f).ToString("0.0", CultureInfo.InvariantCulture) + T("number.ten_thousand");
        }

        if (value >= 1000f)
        {
            return (value / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + "K";
        }

        return Mathf.FloorToInt(value).ToString(CultureInfo.InvariantCulture);
    }

    private void BuildUI()
    {
        EnsureEventSystem();
        tabButtons.Clear();
        wideLayout = ShouldUseWideLayout();
        layoutScreenSize = new Vector2Int(Screen.width, Screen.height);

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = wideLayout ? 1f : 0f;

        Image bgImage = canvasObject.AddComponent<Image>();
        bgImage.color = bg;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        RectTransform safe = CreateRect("SafeArea", root);
        safe.anchorMin = Vector2.zero;
        safe.anchorMax = Vector2.one;
        safe.offsetMin = Vector2.zero;
        safe.offsetMax = Vector2.zero;
        bool shortPortrait = IsShortPortraitScreen();
        safe.gameObject.AddComponent<SafeAreaFitter>().SetPadding(wideLayout ? 16f : shortPortrait ? 14f : 18f, wideLayout ? 16f : shortPortrait ? 14f : 18f, wideLayout ? 10f : 12f, wideLayout ? 12f : shortPortrait ? 8f : 12f);

        if (wideLayout)
        {
            BuildWideShell(safe);
        }
        else
        {
            BuildPortraitShell(safe);
        }

        BuildOfflineRewardPopup(safe);
        UpdateTabButtons();
    }

    private void BuildStartupErrorUI(Exception exception)
    {
        EnsureEventSystem();
        var canvasObject = new GameObject("StartupErrorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f;

        Image bgImage = canvasObject.AddComponent<Image>();
        bgImage.color = bg;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        RectTransform panelRect = CreateRect("StartupError", root);
        panelRect.anchorMin = new Vector2(0.06f, 0.36f);
        panelRect.anchorMax = new Vector2(0.94f, 0.64f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.sprite = RoundedPanelSprite();
        panelImage.type = Image.Type.Sliced;
        panelImage.color = panel;

        RectTransform textRect = CreateRect("Message", panelRect);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30f, 26f);
        textRect.offsetMax = new Vector2(-30f, -26f);

        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = CurrentFont();
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.color = pine;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = $"启动失败\n{exception.GetType().Name}\n请查看 Logcat";
    }

    private void BuildPortraitShell(RectTransform safe)
    {
        bool shortPortrait = IsShortPortraitScreen();
        float headerHeight = shortPortrait ? 136f : 150f;
        float navHeight = shortPortrait ? 120f : 128f;
        const float gap = 8f;

        RectTransform header = CreateHeader(safe, headerHeight, false);
        AnchorTop(header, 0f, headerHeight);

        RectTransform offline = CreateOfflineNotice(safe);
        AnchorTop(offline, headerHeight + gap, 38f);

        RectTransform nav = CreateNavigation(safe, false);
        AnchorBottom(nav, 0f, navHeight);

        ScrollRect scroll = CreateScroll(safe);
        AnchorFill(scroll.GetComponent<RectTransform>(), 0f, 0f, headerHeight + gap, navHeight + gap);
        contentRoot = scroll.content;
    }

    private void BuildWideShell(RectTransform safe)
    {
        const float navWidth = 120f;
        const float gap = 8f;
        const float headerHeight = 128f;

        RectTransform nav = CreateNavigation(safe, true);
        AnchorLeft(nav, 0f, navWidth);

        RectTransform main = CreateRect("Main", safe);
        AnchorFill(main, navWidth + gap, 0f, 0f, 0f);

        RectTransform header = CreateHeader(main, headerHeight, true);
        AnchorTop(header, 0f, headerHeight);

        RectTransform offline = CreateOfflineNotice(main);
        AnchorTop(offline, headerHeight + gap, 34f);

        ScrollRect scroll = CreateScroll(main);
        AnchorFill(scroll.GetComponent<RectTransform>(), 0f, 0f, headerHeight + gap, 0f);
        contentRoot = scroll.content;
    }

    private RectTransform CreateHeader(RectTransform parent, float height, bool compact)
    {
        RectTransform top = CreatePanel("Top", parent, pine);
        top.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        VerticalLayoutGroup topLayout = top.gameObject.AddComponent<VerticalLayoutGroup>();
        topLayout.padding = compact ? new RectOffset(14, 14, 7, 7) : new RectOffset(16, 16, 10, 10);
        topLayout.spacing = compact ? 7f : 8f;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandWidth = true;
        topLayout.childForceExpandHeight = false;

        titleText = CreateText("HiddenTitle", top, T("app.title"), 1, FontStyle.Normal, panel, TextAnchor.MiddleLeft);
        titleText.gameObject.SetActive(false);

        RectTransform metricRow = CreateRect("MetricRow", top);
        metricRow.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 52f : 48f;
        HorizontalLayoutGroup metricLayout = metricRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metricLayout.spacing = compact ? 8f : 10f;
        metricLayout.childAlignment = TextAnchor.MiddleCenter;
        metricLayout.childControlWidth = true;
        metricLayout.childControlHeight = true;
        metricLayout.childForceExpandWidth = true;
        metricLayout.childForceExpandHeight = true;

        cashText = CreateHeaderChip(metricRow, "HeaderCash", coinSprite, honey, compact ? 210f : 330f, compact ? 25 : 25);
        newItemsText = CreateHeaderChip(metricRow, "HeaderStock", shelfSprite != null ? shelfSprite : counterSprite, blue, compact ? 190f : 300f, compact ? 22 : 22);
        if (compact)
        {
            totalText = CreateHeaderChip(metricRow, "HeaderIncome", customerSprite, coral, 210f, 22);
        }

        RectTransform timerRow = CreatePanel("HeaderTimer", top, new Color(0.08f, 0.18f, 0.15f, 0.96f));
        timerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 54f : 46f;
        HorizontalLayoutGroup timerLayout = timerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        timerLayout.padding = compact ? new RectOffset(12, 12, 9, 9) : new RectOffset(12, 12, 8, 8);
        timerLayout.spacing = compact ? 12f : 12f;
        timerLayout.childAlignment = TextAnchor.MiddleCenter;
        timerLayout.childControlWidth = true;
        timerLayout.childControlHeight = true;
        timerLayout.childForceExpandWidth = false;

        queueText = CreateText("AutoTimer", timerRow, "", compact ? 28 : 28, FontStyle.Bold, coral, TextAnchor.MiddleLeft);
        LayoutElement timerTextLayout = queueText.gameObject.AddComponent<LayoutElement>();
        timerTextLayout.preferredWidth = compact ? 330f : 330f;
        timerTextLayout.flexibleWidth = 0f;
        queueText.resizeTextForBestFit = true;
        queueText.resizeTextMinSize = compact ? 17 : 16;
        queueText.resizeTextMaxSize = compact ? 28 : 28;
        headerAutoFill = CreateProgress(timerRow, 0f, coral, compact ? 26f : 22f);
        LayoutElement progressLayout = headerAutoFill.parent.GetComponent<LayoutElement>();
        progressLayout.flexibleWidth = 1f;
        return top;
    }

    private Text CreateHeaderChip(RectTransform parent, string name, Sprite icon, Color accent, float width, int fontSize)
    {
        RectTransform chip = CreatePanel(name, parent, new Color(0.08f, 0.18f, 0.15f, 0.96f));
        LayoutElement chipLayout = chip.gameObject.AddComponent<LayoutElement>();
        chipLayout.preferredWidth = width;
        chipLayout.flexibleWidth = 1f;
        HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 7, 7);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        RectTransform iconBox = CreatePanel("HeaderIcon", chip, accent);
        iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = fontSize + 22f;
        if (icon != null)
        {
            CreateImage("Icon", iconBox, icon, true);
        }

        Text text = CreateText("Value", chip, "", fontSize, FontStyle.Bold, panel, TextAnchor.MiddleLeft);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(16, fontSize - 14);
        text.resizeTextMaxSize = fontSize;
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return text;
    }

    private RectTransform CreateStats(RectTransform parent, bool compact)
    {
        RectTransform stats = CreateRect("Stats", parent);
        stats.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 62f : 132f;
        if (compact)
        {
            HorizontalLayoutGroup row = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childControlHeight = true;
            row.childControlWidth = true;
            row.childForceExpandHeight = true;
            row.childForceExpandWidth = true;
        }
        else
        {
            GridLayoutGroup grid = stats.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(8f, 8f);
            grid.cellSize = new Vector2(500f, 62f);
        }

        cashText = CreateStat(stats, T("stat.cash"), coinSprite);
        reputationText = CreateStat(stats, T("stat.reputation"), reputationSprite);
        totalText = CreateStat(stats, T("stat.total"), customerSprite);
        newItemsText = CreateStat(stats, T("stat.new_items"), upgradeSprite);
        return stats;
    }

    private RectTransform CreateOfflineNotice(RectTransform parent)
    {
        offlineText = CreateText("OfflineNotice", parent, "", wideLayout ? 21 : 23, FontStyle.Bold, new Color(0.36f, 0.24f, 0.06f), TextAnchor.MiddleLeft);
        offlineLayout = offlineText.gameObject.AddComponent<LayoutElement>();
        offlineLayout.preferredHeight = 0f;
        offlineText.gameObject.SetActive(false);
        return offlineText.GetComponent<RectTransform>();
    }

    private void BuildOfflineRewardPopup(RectTransform parent)
    {
        offlineRewardOverlay = CreateRect("OfflineRewardOverlay", parent);
        AnchorFill(offlineRewardOverlay, 0f, 0f, 0f, 0f);
        Image overlayImage = offlineRewardOverlay.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0.03f, 0.02f, 0.01f, 0.62f);

        RectTransform card = CreatePanel("OfflineRewardCard", offlineRewardOverlay, panel);
        card.anchorMin = wideLayout ? new Vector2(0.18f, 0.22f) : new Vector2(0.07f, 0.29f);
        card.anchorMax = wideLayout ? new Vector2(0.82f, 0.78f) : new Vector2(0.93f, 0.71f);
        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(30, 30, 24, 24) : new RectOffset(34, 34, 30, 30);
        layout.spacing = wideLayout ? 14f : 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("Title", card, T("offline.title"), wideLayout ? 36 : 44, FontStyle.Bold, pine, TextAnchor.MiddleCenter);
        CreateText("Body", card, F("offline.body", data.pendingOfflineMinutes, data.pendingOfflineSold, Money(data.pendingOfflineEarned)), wideLayout ? 27 : 33, FontStyle.Bold, ink, TextAnchor.MiddleCenter);
        CreateText("Cap", card, F("offline.cap", (OfflineCapSecondsLimit() / 3600f).ToString("0.0", CultureInfo.InvariantCulture)), wideLayout ? 22 : 26, FontStyle.Normal, new Color(0.34f, 0.28f, 0.18f), TextAnchor.MiddleCenter);
        CreateButton(card, T("action.claim"), honey, ClaimOfflineReward);

        offlineRewardOverlay.gameObject.SetActive(HasPendingOfflineReward());
    }

    private bool HasPendingOfflineReward()
    {
        return data.pendingOfflineEarned > 0.01f;
    }

    private void ClaimOfflineReward()
    {
        float earned = data.pendingOfflineEarned;
        data.cash += earned;
        data.totalEarned += earned;
        data.pendingOfflineMinutes = 0;
        data.pendingOfflineSold = 0;
        data.pendingOfflineEarned = 0f;
        offlineNotice = string.Empty;
        AddLog(F("offline.claimed", Money(earned)));
        if (offlineRewardOverlay != null)
        {
            offlineRewardOverlay.gameObject.SetActive(false);
        }

        RenderHeader();
        Save();
    }

    private RectTransform CreateNavigation(RectTransform parent, bool vertical)
    {
        RectTransform tabs = CreatePanel("Tabs", parent, dock);
        LayoutElement tabsLayoutElement = tabs.gameObject.AddComponent<LayoutElement>();
        if (vertical)
        {
            tabsLayoutElement.preferredWidth = 128f;
            VerticalLayoutGroup tabsLayout = tabs.gameObject.AddComponent<VerticalLayoutGroup>();
            tabsLayout.padding = new RectOffset(7, 7, 7, 7);
            tabsLayout.spacing = 7f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlHeight = true;
            tabsLayout.childControlWidth = true;
            tabsLayout.childForceExpandHeight = false;
            tabsLayout.childForceExpandWidth = true;
        }
        else
        {
            tabsLayoutElement.preferredHeight = 136f;
            HorizontalLayoutGroup tabsLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.padding = new RectOffset(8, 8, 8, 8);
            tabsLayout.spacing = 8f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childForceExpandWidth = true;
        }

        AddTabButton(tabs, Tab.Shop, T("tab.shop"), vertical);
        AddTabButton(tabs, Tab.Stock, T("tab.stock"), vertical);
        AddTabButton(tabs, Tab.Upgrades, T("tab.upgrades"), vertical);
        AddTabButton(tabs, Tab.Staff, T("tab.staff"), vertical);
        AddTabButton(tabs, Tab.Settings, T("tab.settings"), vertical);
        return tabs;
    }

    private void RenderAll()
    {
        RenderHeader();
        RenderTab();
        UpdateTabButtons();
    }

    private void RenderHeader()
    {
        int primaryIndex = PrimaryProductIndex();
        int autoIncomePerMinute = EstimatedIncomePerMinute();
        SetTextIfChanged(titleText, F("header.level", T("app.title"), data.level));
        SetTextIfChanged(cashText, $"{T("stat.cash")} {Money(data.cash)}");
        SetTextIfChanged(reputationText, $"{T("stat.reputation")}\n{Compact(data.reputation)}");
        SetTextIfChanged(totalText, $"{T("stat.income")} {Money(autoIncomePerMinute)}/min");
        SetTextIfChanged(newItemsText, HeaderStockText());
        SetTextIfChanged(queueText, HasSellableStock() ? F("order.timer", NextAutoSaleSeconds().ToString("0.0", CultureInfo.InvariantCulture)) : T("shop.waiting_restock"));
        if (stageCashText != null)
        {
            SetTextIfChanged(stageCashText, $"{T("stat.cash")}\n{Money(data.cash)}");
        }

        if (stageReputationText != null)
        {
            SetTextIfChanged(stageReputationText, $"{T("stat.reputation")}\n{Compact(data.reputation)}");
        }

        if (stageIncomeText != null)
        {
            SetTextIfChanged(stageIncomeText, $"{T("guide.sell")}\n{Money(autoIncomePerMinute)}/min");
        }

        SetTextIfChanged(offlineText, offlineNotice);
        bool hasOfflineNotice = !string.IsNullOrEmpty(offlineNotice);
        offlineText.gameObject.SetActive(hasOfflineNotice);
        offlineLayout.preferredHeight = hasOfflineNotice ? 42f : 0f;
        UpdateAutoSaleText();
    }

    private void UpdateAutoSaleVisuals()
    {
        if (autoSaleFill == null && headerAutoFill == null)
        {
            UpdateSceneMotion();
            UpdateSalePopVisual();
            return;
        }

        float targetProgress = HasSellableStock() ? Mathf.Clamp01(data.queue) : 0f;
        if (targetProgress < displayedAutoSaleProgress - 0.35f)
        {
            displayedAutoSaleProgress = targetProgress;
        }
        else
        {
            float speed = targetProgress > displayedAutoSaleProgress ? 3.6f : 7.0f;
            displayedAutoSaleProgress = Mathf.MoveTowards(displayedAutoSaleProgress, targetProgress, Time.deltaTime * speed);
        }

        if (autoSaleFill != null)
        {
            autoSaleFill.anchorMax = new Vector2(displayedAutoSaleProgress, 1f);
            autoSaleFill.offsetMin = Vector2.zero;
            autoSaleFill.offsetMax = Vector2.zero;
        }

        if (headerAutoFill != null)
        {
            headerAutoFill.anchorMax = new Vector2(displayedAutoSaleProgress, 1f);
            headerAutoFill.offsetMin = Vector2.zero;
            headerAutoFill.offsetMax = Vector2.zero;
        }

        UpdateSceneMotion();
        UpdateSalePopVisual();
    }

    private void UpdateSceneMotion()
    {
        float time = Time.unscaledTime;
        float saleBurst = Mathf.Clamp01(salePopTimer / 0.45f);
        float restockBurst = Mathf.Clamp01(restockPulseTimer / 0.65f);
        float upgradeBurst = Mathf.Clamp01(upgradePulseTimer / 0.75f);
        float orderCompleteBurst = Mathf.Clamp01(orderCompleteTimer / CustomerLeaveDurationSeconds);
        float newOrderBurst = Mathf.Clamp01(newOrderFlashTimer / CustomerEnterDurationSeconds);
        float selectBurst = Mathf.Clamp01(orderSelectPulseTimer / 0.42f);
        float productSaleBurst = recentSoldProductIndex == CurrentOrderDisplayProductIndex() ? saleBurst : 0f;
        float productRestockBurst = recentRestockProductIndex == CurrentOrderDisplayProductIndex() ? restockBurst : 0f;
        if (sceneProductMotion != null)
        {
            float idlePulse = 1f + Mathf.Sin(time * 3.2f) * 0.012f;
            float salePulse = 1f + productSaleBurst * 0.16f + productRestockBurst * 0.18f + upgradeBurst * 0.05f + orderCompleteBurst * 0.08f + selectBurst * 0.12f;
            sceneProductMotion.localScale = Vector3.one * idlePulse * salePulse;
        }

        if (sceneProductOutline != null)
        {
            float visualBurst = Mathf.Max(selectBurst, productRestockBurst, productSaleBurst);
            sceneProductOutline.effectColor = new Color(1f, 0.90f, 0.30f, 0.72f + visualBurst * 0.24f);
            float distance = wideLayout ? 3f : 4f;
            sceneProductOutline.effectDistance = new Vector2(distance + visualBurst * 2f, -distance - visualBurst * 2f);
        }

        if (sceneProductImage != null)
        {
            sceneProductImage.color = Color.Lerp(sceneProductBaseColor, Color.white, Mathf.Max(selectBurst * 0.18f, productRestockBurst * 0.24f, productSaleBurst * 0.22f));
        }

        if (sceneRestockFxMotion != null && sceneRestockFxImage != null)
        {
            bool showRestock = restockBurst > 0f;
            if (sceneRestockFxMotion.gameObject.activeSelf != showRestock)
            {
                sceneRestockFxMotion.gameObject.SetActive(showRestock);
            }

            if (showRestock)
            {
                float alpha = Mathf.Clamp01(restockPulseTimer / 0.65f);
                sceneRestockFxMotion.localScale = Vector3.one * (1f + (1f - alpha) * 0.24f);
                sceneRestockFxImage.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        if (sceneCheckoutItemMotion != null && sceneCheckoutItemImage != null)
        {
            bool showCheckoutItem = salePopTimer > 0f && recentSoldProductIndex >= 0;
            if (sceneCheckoutItemMotion.gameObject.activeSelf != showCheckoutItem)
            {
                sceneCheckoutItemMotion.gameObject.SetActive(showCheckoutItem);
            }

            if (showCheckoutItem)
            {
                SceneCustomerRect(out float customerWidth, out float checkoutCustomerHeight, out float customerBaseY);
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - salePopTimer / 1.15f));
                float startX = SceneCustomerCounterX() + customerWidth * 0.44f;
                float endX = wideLayout ? 0.66f : 0.64f;
                float arc = Mathf.Sin(progress * Mathf.PI) * 0.035f;
                float x = Mathf.Lerp(startX, endX, progress);
                float y = customerBaseY + Mathf.Lerp(0.17f, 0.21f, progress) + arc;
                float size = Mathf.Min(wideLayout ? 0.060f : 0.072f, checkoutCustomerHeight * 0.22f);
                SetSceneRect(sceneCheckoutItemMotion, x, y, size, size);
                sceneCheckoutItemMotion.localScale = Vector3.one;
                sceneCheckoutItemImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - progress * 0.22f));
            }
        }

        if (sceneCustomerMotion != null)
        {
            bool showingOrderComplete = pendingOrderCompletion || (orderCompleteTimer > 0f && !string.IsNullOrEmpty(orderCompleteMessage));
            bool hasSellableStock = HasSellableStock();
            bool canServe = hasSellableStock || showingOrderComplete;
            SceneCustomerRect(out float width, out float height, out float baseY);
            float exitY = wideLayout ? 0.03f : 0.025f;
            float doorX = SceneCustomerDoorX();
            float entryX = SceneCustomerEntryX();
            float counterX = SceneCustomerCounterX();
            float shelfX = SceneCustomerShelfX();
            float exitStartX = wideLayout ? 0.74f : 0.73f;
            float enteringProgress = 1f - newOrderBurst;
            float leavingProgress = 1f - orderCompleteBurst;
            bool entering = newOrderBurst > 0f && !showingOrderComplete;
            bool paying = saleBurst > 0f && canServe && !showingOrderComplete;
            CustomerSceneState sceneState = CurrentCustomerSceneState(showingOrderComplete, hasSellableStock, paying, entering);
            float x = sceneState == CustomerSceneState.Blocked ? shelfX : counterX;
            float y = baseY;
            bool movingSprite = false;
            bool faceRight = true;
            switch (sceneState)
            {
                case CustomerSceneState.Entering:
                    x = Mathf.Lerp(entryX, counterX, Mathf.SmoothStep(0f, 1f, enteringProgress));
                    movingSprite = true;
                    faceRight = true;
                    break;
                case CustomerSceneState.HappyLeaving:
                    if (leavingProgress < 0.22f)
                    {
                        x = counterX;
                        y = baseY;
                        faceRight = true;
                    }
                    else if (leavingProgress < 0.40f)
                    {
                        float dropProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.40f, leavingProgress));
                        x = Mathf.Lerp(counterX, exitStartX, dropProgress);
                        y = Mathf.Lerp(baseY, exitY, dropProgress);
                        faceRight = true;
                        movingSprite = true;
                    }
                    else
                    {
                        x = Mathf.Lerp(exitStartX, doorX, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 1f, leavingProgress)));
                        y = exitY;
                        faceRight = false;
                        movingSprite = true;
                    }
                    break;
                case CustomerSceneState.Blocked:
                    x = shelfX;
                    faceRight = true;
                    break;
                case CustomerSceneState.Paying:
                case CustomerSceneState.Waiting:
                default:
                    x = counterX;
                    faceRight = true;
                    break;
            }

            SetSceneRect(sceneCustomerMotion, x, y, width, height);
            sceneCustomerMotion.localScale = new Vector3(faceRight ? 1f : -1f, 1f, 1f);
            sceneCustomerMotion.localRotation = Quaternion.identity;

            if (sceneCustomerImage != null)
            {
                Sprite sprite = null;
                switch (sceneState)
                {
                    case CustomerSceneState.HappyLeaving:
                        sprite = movingSprite
                            ? FirstSprite(CustomerWalkSprite(time), Mvp23Sprite(Mvp23CustomerLeave), Mvp23Sprite(Mvp23CustomerHappy), Mvp23Sprite(Mvp23CustomerIdle), customerSprite)
                            : FirstSprite(Mvp23Sprite(Mvp23CustomerHappy), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
                        break;
                    case CustomerSceneState.Blocked:
                        sprite = FirstSprite(Mvp23Sprite(Mvp23CustomerDisappointed), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
                        break;
                    case CustomerSceneState.Paying:
                        sprite = FirstSprite(Mvp23Sprite(Mvp23CustomerPay), Mvp23Sprite(Mvp23CustomerHappy), Mvp23Sprite(Mvp23CustomerIdle), customerSprite);
                        break;
                    case CustomerSceneState.Entering:
                        sprite = CustomerWalkSprite(time);
                        break;
                    case CustomerSceneState.Waiting:
                    default:
                        sprite = FirstSprite(Mvp23Sprite(Mvp23CustomerIdle), Mvp23Sprite(Mvp23CustomerWalk01), customerSprite);
                        break;
                }

                sceneCustomerImage.sprite = sprite;
                ConfigureCustomerArtFrame(sceneCustomerImage.rectTransform);
                float enterAlpha = entering ? Mathf.Clamp01(0.40f + enteringProgress * 0.60f) : 1f;
                float leaveAlpha = sceneState == CustomerSceneState.HappyLeaving ? Mathf.Clamp01(1f - Mathf.InverseLerp(0.82f, 1f, leavingProgress) * 0.48f) : 1f;
                sceneCustomerImage.color = canServe ? new Color(1f, 1f, 1f, Mathf.Min(enterAlpha, leaveAlpha)) : new Color(0.86f, 0.86f, 0.86f, 0.92f);
            }
        }

        if (sceneStaffMotion != null)
        {
            sceneStaffMotion.localScale = new Vector3(-1f, 1f, 1f);
            if (sceneStaffImage != null)
            {
                sceneStaffImage.sprite = FirstSprite(
                    orderCompleteBurst > 0f ? Mvp23Sprite(Mvp30StaffCashierSuccess) : null,
                    saleBurst > 0f ? Mvp23Sprite(Mvp23StaffCashierWork) : Mvp23Sprite(Mvp23StaffCashierIdle),
                    StaffSprite(0),
                    customerSprite);
            }
        }

        if (sceneCashierGlow != null)
        {
            float alpha = Mathf.Clamp01(saleBurst * 0.22f + upgradeBurst * 0.14f);
            bool visible = alpha > 0.01f;
            if (sceneCashierGlow.gameObject.activeSelf != visible)
            {
                sceneCashierGlow.gameObject.SetActive(visible);
            }

            Color color = sceneCashierGlow.color;
            color.a = alpha;
            sceneCashierGlow.color = color;
        }

        if (sceneCustomerGlow != null)
        {
            float alpha = Mathf.Clamp01(saleBurst * 0.10f + restockBurst * 0.16f);
            bool visible = alpha > 0.01f;
            if (sceneCustomerGlow.gameObject.activeSelf != visible)
            {
                sceneCustomerGlow.gameObject.SetActive(visible);
            }

            Color color = sceneCustomerGlow.color;
            color.a = alpha;
            sceneCustomerGlow.color = color;
        }
    }

    private void UpdateAutoSaleText()
    {
        if (autoSaleText == null)
        {
            return;
        }

        if (!HasSellableStock())
        {
            SetTextIfChanged(autoSaleText, T("shop.waiting_restock"));
        }
        else
        {
            string seconds = NextAutoSaleSeconds().ToString("0.0", CultureInfo.InvariantCulture);
            SetTextIfChanged(autoSaleText, F("order.timer", seconds));
        }

        if (autoIntervalText != null)
        {
            int autoIncomePerMinute = EstimatedIncomePerMinute();
            SetTextIfChanged(autoIntervalText, $"{Money(autoIncomePerMinute)}/min · {HeaderStockText()}");
        }
    }

    private void UpdateSalePopVisual()
    {
        if (salePopText == null)
        {
            return;
        }

        bool showSalePop = salePopTimer > 0f && !string.IsNullOrEmpty(salePopMessage);
        if (salePopText.gameObject.activeSelf != showSalePop)
        {
            salePopText.gameObject.SetActive(showSalePop);
        }

        if (salePopIconRectTransform != null && salePopIconRectTransform.gameObject.activeSelf != showSalePop)
        {
            salePopIconRectTransform.gameObject.SetActive(showSalePop);
        }

        if (!showSalePop)
        {
            return;
        }

        float alpha = Mathf.Clamp01(salePopTimer / 1.15f);
        float rise = (1f - alpha) * (wideLayout ? 18f : 28f);
        SetTextIfChanged(salePopText, salePopMessage);
        salePopText.color = new Color(honey.r, honey.g, honey.b, alpha);
        if (salePopRectTransform != null)
        {
            salePopRectTransform.offsetMin = new Vector2(0f, rise);
            salePopRectTransform.offsetMax = new Vector2(0f, rise);
        }

        if (salePopIconImage != null)
        {
            salePopIconImage.color = new Color(1f, 1f, 1f, alpha);
        }

        if (salePopIconRectTransform != null)
        {
            salePopIconRectTransform.offsetMin = new Vector2(0f, rise);
            salePopIconRectTransform.offsetMax = new Vector2(0f, rise);
            salePopIconRectTransform.localScale = Vector3.one * (1f + (1f - alpha) * 0.16f);
        }
    }

    private static void SetTextIfChanged(Text text, string value)
    {
        if (text != null && text.text != value)
        {
            text.text = value;
        }
    }

    private static void SetSceneRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x + width, y + height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ConfigureCustomerArtFrame(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SceneProductRect(int productIndex, out float minX, out float minY, out float maxX, out float maxY)
    {
        switch (productIndex)
        {
            case 0:
                minX = 0.16f;
                minY = 0.52f;
                maxX = 0.25f;
                maxY = 0.66f;
                return;
            case 1:
                minX = 0.27f;
                minY = 0.52f;
                maxX = 0.36f;
                maxY = 0.66f;
                return;
            case 2:
                minX = 0.38f;
                minY = 0.52f;
                maxX = 0.47f;
                maxY = 0.66f;
                return;
            case 3:
                minX = 0.38f;
                minY = 0.36f;
                maxX = 0.47f;
                maxY = 0.50f;
                return;
            case 4:
                minX = 0.16f;
                minY = 0.36f;
                maxX = 0.25f;
                maxY = 0.50f;
                return;
            case 5:
                minX = 0.27f;
                minY = 0.36f;
                maxX = 0.36f;
                maxY = 0.50f;
                return;
            case 6:
                minX = 0.50f;
                minY = 0.52f;
                maxX = 0.59f;
                maxY = 0.66f;
                return;
            case 7:
                minX = 0.50f;
                minY = 0.36f;
                maxX = 0.59f;
                maxY = 0.50f;
                return;
            default:
                minX = 0.16f;
                minY = 0.52f;
                maxX = 0.25f;
                maxY = 0.66f;
                return;
        }
    }

    private void SceneCustomerRect(out float width, out float height, out float baseY)
    {
        width = wideLayout ? 0.15f : 0.18f;
        height = wideLayout ? 0.31f : 0.34f;
        baseY = wideLayout ? 0.12f : 0.10f;
    }

    private float SceneCustomerCounterX()
    {
        return wideLayout ? 0.44f : 0.42f;
    }

    private float SceneCustomerShelfX()
    {
        return wideLayout ? 0.16f : 0.14f;
    }

    private float SceneCustomerDoorX()
    {
        return wideLayout ? -0.17f : -0.18f;
    }

    private float SceneQueueX(int index)
    {
        if (index <= 0)
        {
            return wideLayout ? 0.10f : 0.08f;
        }

        return wideLayout ? 0.25f : 0.24f;
    }

    private float SceneCustomerEntryX()
    {
        return data.completedOrders > 0 ? SceneQueueX(0) : SceneCustomerDoorX();
    }

    private static void DisableImageRaycast(RectTransform rect)
    {
        Image image = rect != null ? rect.GetComponent<Image>() : null;
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    private void CreateSceneQueuedCustomers(RectTransform actorLayer, Sprite customerSpriteForQueue, bool showQueue)
    {
        if (!showQueue || customerSpriteForQueue == null)
        {
            return;
        }

        SceneCustomerRect(out float width, out float height, out float baseY);
        int queueCount = data.completedOrders >= 6 ? 2 : 1;
        for (int i = 0; i < queueCount; i++)
        {
            RectTransform queuedCustomer = CreateRect($"SceneQueuedCustomer{i + 1}", actorLayer);
            SetSceneRect(queuedCustomer, SceneQueueX(i), baseY, width, height);
            queuedCustomer.localScale = Vector3.one;

            if (Mvp23Sprite(Mvp23SceneFloorShadow) != null)
            {
                RectTransform shadow = CreateImagePanel("QueueShadow", queuedCustomer, Mvp23Sprite(Mvp23SceneFloorShadow), Color.clear, false);
                shadow.anchorMin = new Vector2(0.10f, 0f);
                shadow.anchorMax = new Vector2(0.90f, 0.18f);
                shadow.offsetMin = Vector2.zero;
                shadow.offsetMax = Vector2.zero;
            }

            Image queuedImage = CreateImage("QueueCustomerArt", queuedCustomer, customerSpriteForQueue, true);
            queuedImage.color = new Color(1f, 1f, 1f, i == 0 ? 0.78f : 0.58f);
        }
    }

    private RectTransform CreateSceneSprite(string name, RectTransform parent, Sprite sprite, Color fallbackColor, float minX, float minY, float maxX, float maxY)
    {
        RectTransform rect = CreateImagePanel(name, parent, sprite, fallbackColor, true);
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = rect.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }

        return rect;
    }

    private void CreateSceneShelfProducts(RectTransform stage)
    {
        for (int i = 0; i < products.Length; i++)
        {
            if (!data.unlocked[i] || ProductSprite(i) == null)
            {
                continue;
            }

            SceneProductRect(i, out float minX, out float minY, out float maxX, out float maxY);
            RectTransform item = CreateSceneSprite($"ShelfProduct{i}", stage, ProductSprite(i), Color.clear, minX, minY, maxX, maxY);
            Image image = item.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            int stock = Mathf.FloorToInt(data.stock[i]);
            if (stock <= 0)
            {
                image.color = new Color(0.72f, 0.72f, 0.72f, 0.52f);
            }
            else if (i == CurrentOrderDisplayProductIndex())
            {
                image.color = Color.white;
            }
            else if (stock < CurrentOrderQuantity(i))
            {
                image.color = new Color(1f, 0.88f, 0.66f, 0.88f);
            }
        }
    }

    private void RenderTab()
    {
        stageCashText = null!;
        stageReputationText = null!;
        stageIncomeText = null!;
        autoSaleText = null!;
        autoIntervalText = null!;
        salePopText = null!;
        autoSaleFill = null!;
        salePopRectTransform = null!;
        salePopIconRectTransform = null!;
        sceneProductMotion = null!;
        sceneCustomerMotion = null!;
        sceneStaffMotion = null!;
        sceneRestockFxMotion = null!;
        sceneCheckoutItemMotion = null!;
        salePopIconImage = null!;
        sceneProductImage = null!;
        sceneProductOutline = null!;
        sceneCustomerImage = null!;
        sceneStaffImage = null!;
        sceneRestockFxImage = null!;
        sceneCheckoutItemImage = null!;
        sceneCashierGlow = null!;
        sceneCustomerGlow = null!;
        Clear(contentRoot);
        if (activeTab == Tab.Shop)
        {
            RenderShop();
            return;
        }

        if (activeTab == Tab.Stock)
        {
            RenderStock();
            return;
        }

        if (activeTab == Tab.Upgrades)
        {
            RenderUpgrades();
            return;
        }

        if (activeTab == Tab.Staff)
        {
            RenderStaff();
            return;
        }

        RenderSettings();
    }

    private void RenderShop()
    {
        int primaryIndex = PrimaryProductIndex();
        int restockIndex = BestRestockIndex();
        int upgradeIndex = BestUpgradeIndex();
        int staffIndex = BestStaffIndex();
        int priorityRestockAmount = PriorityRestockAmount(restockIndex);
        int priorityRestockCost = restockIndex >= 0 ? priorityRestockAmount * UnitCost(restockIndex) : 0;
        CalculateAffordablePriorityRestockPlan(restockIndex, out int affordablePriorityRestockAmount, out int affordablePriorityRestockCost);
        int cheapestRestockCost = CheapestRestockCost();
        int autoIncomePerMinute = EstimatedIncomePerMinute();
        int nextItemValue = CurrentOrderNextItemValue();
        int upgradeCost = upgradeIndex >= 0 ? UpgradeCost(upgradeIndex) : 0;
        int staffCost = staffIndex >= 0 ? StaffCost(staffIndex) : 0;
        bool shortPortrait = IsShortPortraitScreen();
        bool hasSellableStock = HasSellableStock();
        bool showingOrderComplete = pendingOrderCompletion || (orderCompleteTimer > 0f && !string.IsNullOrEmpty(orderCompleteMessage));
        int orderMissingCost = CurrentOrderMissingCost();
        bool orderHasMissing = orderMissingCost > 0;
        bool lowStock = hasSellableStock && LowestUnlockedStockRatio() <= LowStockRatio;

        RectTransform stage = CreatePanel("GameStage", contentRoot, new Color(0.16f, 0.36f, 0.32f));
        stage.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 540f : shortPortrait ? 650f : 710f;

        RectTransform backgroundLayer = CreateRect("SceneLayerBackground", stage);
        AnchorFill(backgroundLayer, 0f, 0f, 0f, 0f);
        RectTransform stockLayer = CreateRect("SceneLayerStock", stage);
        AnchorFill(stockLayer, 0f, 0f, 0f, 0f);
        RectTransform actorLayer = CreateRect("SceneLayerActors", stage);
        AnchorFill(actorLayer, 0f, 0f, 0f, 0f);
        RectTransform foregroundLayer = CreateRect("SceneLayerForeground", stage);
        AnchorFill(foregroundLayer, 0f, 0f, 0f, 0f);
        RectTransform fxLayer = CreateRect("SceneLayerFx", stage);
        AnchorFill(fxLayer, 0f, 0f, 0f, 0f);
        backgroundLayer.SetSiblingIndex(0);
        stockLayer.SetSiblingIndex(1);
        actorLayer.SetSiblingIndex(2);
        foregroundLayer.SetSiblingIndex(3);
        fxLayer.SetSiblingIndex(4);

        RectTransform sceneWall = CreatePanel("SceneWall", backgroundLayer, new Color(0.96f, 0.83f, 0.58f, 1f));
        AnchorFill(sceneWall, 0f, 0f, 0f, 0f);
        DisableImageRaycast(sceneWall);

        RectTransform stageArt = CreateImagePanel("ShopFront", backgroundLayer, FirstSprite(Mvp23Sprite(Mvp23SceneInterior), shopFrontSprite), Color.clear, false);
        AnchorFill(stageArt, 0f, 0f, 0f, 0f);
        Image stageArtImage = stageArt.GetComponent<Image>();
        if (stageArtImage != null)
        {
            stageArtImage.color = Color.white;
            stageArtImage.raycastTarget = false;
        }

        RectTransform rearShelfBand = CreatePanel("SceneBackWallBand", backgroundLayer, new Color(0.44f, 0.30f, 0.18f, 0.32f));
        rearShelfBand.anchorMin = new Vector2(0.04f, 0.56f);
        rearShelfBand.anchorMax = new Vector2(0.96f, 0.88f);
        rearShelfBand.offsetMin = Vector2.zero;
        rearShelfBand.offsetMax = Vector2.zero;
        DisableImageRaycast(rearShelfBand);

        RectTransform floor = CreatePanel("SceneFloor", backgroundLayer, new Color(0.72f, 0.48f, 0.28f, 0.96f));
        floor.anchorMin = new Vector2(0f, 0f);
        floor.anchorMax = new Vector2(1f, 0.34f);
        floor.offsetMin = Vector2.zero;
        floor.offsetMax = Vector2.zero;
        DisableImageRaycast(floor);

        RectTransform walkLane = CreatePanel("SceneWalkLane", backgroundLayer, new Color(0.94f, 0.73f, 0.42f, 0.08f));
        walkLane.anchorMin = new Vector2(0.04f, 0.08f);
        walkLane.anchorMax = new Vector2(0.96f, 0.23f);
        walkLane.offsetMin = Vector2.zero;
        walkLane.offsetMax = Vector2.zero;
        DisableImageRaycast(walkLane);

        if (!hasSellableStock && !showingOrderComplete && Mvp23Sprite(Mvp23SceneEmptyShelfOverlay) != null)
        {
            RectTransform emptyOverlay = CreateImagePanel("EmptyShelfOverlay", backgroundLayer, Mvp23Sprite(Mvp23SceneEmptyShelfOverlay), Color.clear, false);
            emptyOverlay.anchorMin = new Vector2(0.04f, 0.30f);
            emptyOverlay.anchorMax = new Vector2(0.96f, 0.86f);
            emptyOverlay.offsetMin = Vector2.zero;
            emptyOverlay.offsetMax = Vector2.zero;
        }

        Sprite shelfStateSprite = FirstSprite(
            restockPulseTimer > 0f ? Mvp23Sprite(Mvp30ShelfRestocked) : null,
            !hasSellableStock && !showingOrderComplete ? Mvp23Sprite(Mvp23ShelfEmpty) : lowStock ? Mvp23Sprite(Mvp23ShelfLow) : Mvp23Sprite(Mvp23ShelfFull),
            shelfSprite);
        Sprite counterStateSprite = FirstSprite(Mvp23Sprite(Mvp23CashierCounter), counterSprite);
        if (shelfStateSprite != null || counterStateSprite != null)
        {
            RectTransform shelfArt = CreateImagePanel("ShelfArt", stockLayer, shelfStateSprite, Color.clear, true);
            shelfArt.anchorMin = new Vector2(0.06f, 0.31f);
            shelfArt.anchorMax = new Vector2(0.48f, 0.82f);
            shelfArt.offsetMin = Vector2.zero;
            shelfArt.offsetMax = Vector2.zero;
            Image shelfImage = shelfArt.GetComponent<Image>();
            if (shelfImage != null)
            {
                shelfImage.raycastTarget = false;
            }

            CreateSceneShelfProducts(stockLayer);

            if (counterStateSprite != null)
            {
                RectTransform counterArt = CreateImagePanel("CounterArt", stockLayer, counterStateSprite, Color.clear, true);
                counterArt.anchorMin = new Vector2(0.46f, 0.12f);
                counterArt.anchorMax = new Vector2(0.98f, 0.58f);
                counterArt.offsetMin = Vector2.zero;
                counterArt.offsetMax = Vector2.zero;
                counterArt.localScale = new Vector3(-1f, 1f, 1f);
                Image counterImage = counterArt.GetComponent<Image>();
                if (counterImage != null)
                {
                    counterImage.raycastTarget = false;
                }
            }
            else
            {
                RectTransform counterBase = CreatePanel("SceneCounterBase", stockLayer, new Color(0.48f, 0.29f, 0.16f, 0.98f));
                counterBase.anchorMin = new Vector2(0.54f, 0.20f);
                counterBase.anchorMax = new Vector2(0.92f, 0.48f);
                counterBase.offsetMin = Vector2.zero;
                counterBase.offsetMax = Vector2.zero;

                RectTransform counterTop = CreatePanel("SceneCounterTop", stockLayer, new Color(0.78f, 0.55f, 0.31f, 0.98f));
                counterTop.anchorMin = new Vector2(0.50f, 0.43f);
                counterTop.anchorMax = new Vector2(0.94f, 0.55f);
                counterTop.offsetMin = Vector2.zero;
                counterTop.offsetMax = Vector2.zero;

                RectTransform register = CreatePanel("SceneRegister", stockLayer, new Color(0.95f, 0.82f, 0.58f, 0.98f));
                register.anchorMin = new Vector2(0.70f, 0.54f);
                register.anchorMax = new Vector2(0.84f, 0.70f);
                register.offsetMin = Vector2.zero;
                register.offsetMax = Vector2.zero;
            }
        }

        RectTransform customerPoint = CreatePanel("CustomerPulse", fxLayer, new Color(0.95f, 0.78f, 0.32f, 0f));
        customerPoint.anchorMin = new Vector2(0.42f, 0.12f);
        customerPoint.anchorMax = new Vector2(0.62f, 0.44f);
        customerPoint.offsetMin = Vector2.zero;
        customerPoint.offsetMax = Vector2.zero;
        sceneCustomerGlow = customerPoint.GetComponent<Image>();
        sceneCustomerGlow.raycastTarget = false;
        customerPoint.gameObject.SetActive(false);

        RectTransform cashierPoint = CreatePanel("CashierPulse", fxLayer, new Color(0.94f, 0.65f, 0.20f, 0f));
        cashierPoint.anchorMin = new Vector2(0.68f, 0.16f);
        cashierPoint.anchorMax = new Vector2(0.88f, 0.48f);
        cashierPoint.offsetMin = Vector2.zero;
        cashierPoint.offsetMax = Vector2.zero;
        sceneCashierGlow = cashierPoint.GetComponent<Image>();
        sceneCashierGlow.raycastTarget = false;
        cashierPoint.gameObject.SetActive(false);

        Sprite customerActorSprite = FirstSprite(showingOrderComplete ? Mvp23Sprite(Mvp23CustomerHappy) : hasSellableStock ? Mvp23Sprite(Mvp23CustomerIdle) : Mvp23Sprite(Mvp23CustomerDisappointed), customerSprite);
        CreateSceneQueuedCustomers(actorLayer, FirstSprite(Mvp23Sprite(Mvp23CustomerIdle), customerSprite), hasSellableStock && !showingOrderComplete && newOrderFlashTimer <= 0.01f);

        SceneCustomerRect(out float customerWidth, out float customerHeight, out float customerBaseY);
        float renderCustomerX = newOrderFlashTimer > 0f && !showingOrderComplete
            ? SceneCustomerEntryX()
            : showingOrderComplete || hasSellableStock ? SceneCustomerCounterX() : SceneCustomerShelfX();
        RectTransform customerActor = CreateRect("SceneCustomer", actorLayer);
        SetSceneRect(customerActor, renderCustomerX, customerBaseY, customerWidth, customerHeight);
        sceneCustomerMotion = customerActor;
        if (Mvp23Sprite(Mvp23SceneFloorShadow) != null)
        {
            RectTransform customerShadow = CreateImagePanel("CustomerShadow", customerActor, Mvp23Sprite(Mvp23SceneFloorShadow), Color.clear, false);
            customerShadow.anchorMin = new Vector2(0.10f, 0f);
            customerShadow.anchorMax = new Vector2(0.90f, 0.18f);
            customerShadow.offsetMin = Vector2.zero;
            customerShadow.offsetMax = Vector2.zero;
        }

        if (customerActorSprite != null)
        {
            sceneCustomerImage = CreateImage("CustomerArt", customerActor, customerActorSprite, true);
            sceneCustomerImage.color = hasSellableStock || showingOrderComplete ? Color.white : new Color(0.86f, 0.86f, 0.86f, 0.92f);
        }

        if (!hasSellableStock && !showingOrderComplete && Mvp23Sprite(Mvp23FxCustomerWaitingBubble) != null)
        {
            RectTransform waitingBubble = CreateImagePanel("CustomerWaitingBubble", customerActor, Mvp23Sprite(Mvp23FxCustomerWaitingBubble), Color.clear, true);
            waitingBubble.anchorMin = new Vector2(0.44f, 0.72f);
            waitingBubble.anchorMax = new Vector2(1.08f, 1.12f);
            waitingBubble.offsetMin = Vector2.zero;
            waitingBubble.offsetMax = Vector2.zero;
        }

        RectTransform staffActor = CreateRect("SceneCashierStaff", actorLayer);
        SetSceneRect(staffActor, wideLayout ? 0.68f : 0.67f, wideLayout ? 0.18f : 0.16f, wideLayout ? 0.15f : 0.18f, wideLayout ? 0.31f : 0.34f);
        staffActor.localScale = new Vector3(-1f, 1f, 1f);
        sceneStaffMotion = staffActor;
        if (Mvp23Sprite(Mvp23SceneFloorShadow) != null)
        {
            RectTransform staffShadow = CreateImagePanel("StaffShadow", staffActor, Mvp23Sprite(Mvp23SceneFloorShadow), Color.clear, false);
            staffShadow.anchorMin = new Vector2(0.10f, 0f);
            staffShadow.anchorMax = new Vector2(0.90f, 0.18f);
            staffShadow.offsetMin = Vector2.zero;
            staffShadow.offsetMax = Vector2.zero;
        }

        Sprite staffActorSprite = FirstSprite(Mvp23Sprite(Mvp23StaffCashierIdle), StaffSprite(0), customerSprite);
        if (staffActorSprite != null)
        {
            sceneStaffImage = CreateImage("StaffArt", staffActor, staffActorSprite, true);
        }

        SceneProductRect(primaryIndex, out float productMinX, out float productMinY, out float productMaxX, out float productMaxY);
        RectTransform productIcon = CreateRect("SceneProductFocus", fxLayer);
        productIcon.anchorMin = new Vector2(Mathf.Max(0.02f, productMinX - 0.025f), Mathf.Max(0.06f, productMinY - 0.035f));
        productIcon.anchorMax = new Vector2(Mathf.Min(0.98f, productMaxX + 0.025f), Mathf.Min(0.92f, productMaxY + 0.035f));
        productIcon.offsetMin = Vector2.zero;
        productIcon.offsetMax = Vector2.zero;
        sceneProductMotion = productIcon;

        if (Mvp23Sprite(Mvp23FxItemSelectedGlow) != null)
        {
            RectTransform selectedGlow = CreateImagePanel("SelectedProductGlow", productIcon, Mvp23Sprite(Mvp23FxItemSelectedGlow), Color.clear, true);
            selectedGlow.anchorMin = new Vector2(-0.08f, -0.08f);
            selectedGlow.anchorMax = new Vector2(1.08f, 1.08f);
            selectedGlow.offsetMin = Vector2.zero;
            selectedGlow.offsetMax = Vector2.zero;
            Image selectedGlowImage = selectedGlow.GetComponent<Image>();
            if (selectedGlowImage != null)
            {
                float alpha = 0.34f + Mathf.Clamp01(orderSelectPulseTimer / 0.42f) * 0.20f;
                selectedGlowImage.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        if (lowStock && Mvp23Sprite(Mvp23FxLowStockPulse) != null)
        {
            RectTransform lowPulse = CreateImagePanel("LowStockPulse", productIcon, Mvp23Sprite(Mvp23FxLowStockPulse), Color.clear, true);
            lowPulse.anchorMin = new Vector2(-0.12f, -0.10f);
            lowPulse.anchorMax = new Vector2(1.12f, 1.10f);
            lowPulse.offsetMin = Vector2.zero;
            lowPulse.offsetMax = Vector2.zero;
        }

        Sprite restockFxSprite = FirstSprite(Mvp23Sprite(Mvp23FxRestockSuccessRing), Mvp23Sprite(Mvp23FxRestockSpark));
        if (restockFxSprite != null)
        {
            RectTransform restockFx = CreateImagePanel("RestockSparkFx", fxLayer, restockFxSprite, Color.clear, true);
            restockFx.anchorMin = new Vector2(Mathf.Max(0.02f, productMinX - 0.07f), Mathf.Max(0.04f, productMinY - 0.08f));
            restockFx.anchorMax = new Vector2(Mathf.Min(0.98f, productMaxX + 0.07f), Mathf.Min(0.96f, productMaxY + 0.08f));
            restockFx.offsetMin = Vector2.zero;
            restockFx.offsetMax = Vector2.zero;
            sceneRestockFxMotion = restockFx;
            sceneRestockFxImage = restockFx.GetComponent<Image>();
            restockFx.gameObject.SetActive(restockPulseTimer > 0f);
        }

        Sprite upgradeFxSprite = Mvp23Sprite(Mvp23FxUpgradeSuccess);
        if (upgradeFxSprite != null && upgradePulseTimer > 0f)
        {
            RectTransform upgradeFx = CreateImagePanel("UpgradeSuccessFx", fxLayer, upgradeFxSprite, Color.clear, true);
            upgradeFx.anchorMin = new Vector2(Mathf.Max(0.02f, productMinX - 0.09f), Mathf.Max(0.04f, productMinY - 0.10f));
            upgradeFx.anchorMax = new Vector2(Mathf.Min(0.98f, productMaxX + 0.09f), Mathf.Min(0.96f, productMaxY + 0.10f));
            upgradeFx.offsetMin = Vector2.zero;
            upgradeFx.offsetMax = Vector2.zero;
            Image upgradeFxImage = upgradeFx.GetComponent<Image>();
            if (upgradeFxImage != null)
            {
                float alpha = Mathf.Clamp01(upgradePulseTimer / 0.75f);
                upgradeFxImage.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        Sprite checkoutItemSprite = ProductSprite(recentSoldProductIndex >= 0 ? recentSoldProductIndex : primaryIndex);
        if (checkoutItemSprite != null)
        {
            RectTransform checkoutItem = CreateRect("SceneCheckoutItem", fxLayer);
            sceneCheckoutItemMotion = checkoutItem;
            SetSceneRect(checkoutItem, SceneCustomerCounterX() + customerWidth * 0.44f, customerBaseY + 0.17f, wideLayout ? 0.060f : 0.072f, wideLayout ? 0.060f : 0.072f);
            sceneCheckoutItemImage = CreateImage("Item", checkoutItem, checkoutItemSprite, true);
            checkoutItem.gameObject.SetActive(salePopTimer > 0f && recentSoldProductIndex >= 0);
        }

        displayedAutoSaleProgress = hasSellableStock ? Mathf.Clamp01(data.queue) : 0f;
        salePopText = CreateText("SalePop", fxLayer, salePopMessage, wideLayout ? 42 : shortPortrait ? 48 : 54, FontStyle.Bold, honey, TextAnchor.MiddleRight);
        LayoutElement salePopLayout = salePopText.gameObject.AddComponent<LayoutElement>();
        salePopLayout.ignoreLayout = true;
        salePopRectTransform = salePopText.GetComponent<RectTransform>();
        salePopRectTransform.anchorMin = new Vector2(0.50f, 0.50f);
        salePopRectTransform.anchorMax = new Vector2(0.96f, 0.78f);
        salePopRectTransform.offsetMin = Vector2.zero;
        salePopRectTransform.offsetMax = Vector2.zero;
        salePopText.gameObject.SetActive(salePopTimer > 0f && !string.IsNullOrEmpty(salePopMessage));

        Sprite saleIconSprite = FirstSprite(Mvp23Sprite(Mvp23FxCashFloat), Mvp23Sprite(Mvp23FxCoinPop), coinSprite);
        if (saleIconSprite != null)
        {
            RectTransform saleIcon = CreateRect("SalePopIcon", fxLayer);
            salePopIconRectTransform = saleIcon;
            saleIcon.anchorMin = new Vector2(0.40f, 0.52f);
            saleIcon.anchorMax = new Vector2(0.52f, 0.72f);
            saleIcon.offsetMin = Vector2.zero;
            saleIcon.offsetMax = Vector2.zero;
            salePopIconImage = CreateImage("Icon", saleIcon, saleIconSprite, true);
            saleIcon.gameObject.SetActive(salePopTimer > 0f && !string.IsNullOrEmpty(salePopMessage));
        }

        RectTransform orderCard = CreateImagePanel("CurrentOrder", contentRoot, Mvp23Sprite(Mvp23UiOrderTicket), new Color(1f, 0.97f, 0.88f, 0.94f), false);
        LayoutElement orderCardLayout = orderCard.gameObject.AddComponent<LayoutElement>();
        orderCardLayout.preferredHeight = wideLayout ? 174f : shortPortrait ? 202f : 218f;
        orderCardLayout.flexibleHeight = 0f;
        if (orderCompleteTimer > 0f)
        {
            orderCard.gameObject.AddComponent<UIFlashTint>().Configure(honey, orderCompleteTimer);
            if (Mvp23Sprite(Mvp23FxOrderCompleteGlow) != null)
            {
                RectTransform completeGlow = CreateImagePanel("OrderCompleteGlow", orderCard, Mvp23Sprite(Mvp23FxOrderCompleteGlow), Color.clear, true);
                LayoutElement glowLayout = completeGlow.gameObject.AddComponent<LayoutElement>();
                glowLayout.ignoreLayout = true;
                AnchorFill(completeGlow, -0.03f, -0.05f, -0.03f, -0.05f);
                Image glowImage = completeGlow.GetComponent<Image>();
                if (glowImage != null)
                {
                    glowImage.raycastTarget = false;
                    float alpha = Mathf.Clamp01(orderCompleteTimer / 1.05f);
                    glowImage.color = new Color(1f, 1f, 1f, alpha);
                }

                completeGlow.SetAsLastSibling();
            }
        }
        else if (newOrderFlashTimer > 0f)
        {
            orderCard.gameObject.AddComponent<UIFlashTint>().Configure(blue, newOrderFlashTimer);
        }

        VerticalLayoutGroup orderLayout = orderCard.gameObject.AddComponent<VerticalLayoutGroup>();
        orderLayout.padding = wideLayout ? new RectOffset(16, 16, 8, 8) : new RectOffset(18, 18, 9, 9);
        orderLayout.spacing = wideLayout ? 3f : 4f;
        orderLayout.childControlWidth = true;
        orderLayout.childControlHeight = true;
        orderLayout.childForceExpandWidth = true;
        orderLayout.childForceExpandHeight = false;
        int completedItems = showingOrderComplete ? 0 : CurrentOrderCompletedItemCount();
        int requiredItems = Mathf.Max(1, CurrentOrderInitialItemCount());
        string orderBaseTitle = showingOrderComplete ? orderCompleteMessage : F("order.current_short", Money(CurrentOrderInitialValue()));
        string orderTitleText = $"{orderBaseTitle} · {F("order.progress", completedItems, requiredItems)}";

        RectTransform orderHeader = CreateRect("OrderHeader", orderCard);
        LayoutElement headerLayout = orderHeader.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = wideLayout ? 24f : shortPortrait ? 28f : 30f;
        headerLayout.flexibleHeight = 0f;
        HorizontalLayoutGroup headerGroup = orderHeader.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerGroup.spacing = wideLayout ? 7f : 9f;
        headerGroup.childAlignment = TextAnchor.MiddleLeft;
        headerGroup.childControlWidth = true;
        headerGroup.childControlHeight = true;
        headerGroup.childForceExpandWidth = false;
        headerGroup.childForceExpandHeight = false;
        if (Mvp23Sprite(Mvp30IconOrder) != null)
        {
            RectTransform orderIcon = CreateImagePanel("OrderIcon", orderHeader, Mvp23Sprite(Mvp30IconOrder), Color.clear, true);
            LayoutElement orderIconLayout = orderIcon.gameObject.AddComponent<LayoutElement>();
            orderIconLayout.preferredWidth = wideLayout ? 22f : shortPortrait ? 26f : 28f;
            orderIconLayout.flexibleWidth = 0f;
        }

        Text orderTitle = CreateText("OrderTitle", orderHeader, orderTitleText, wideLayout ? 22 : shortPortrait ? 25 : 27, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        orderTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        orderTitle.resizeTextForBestFit = true;
        orderTitle.resizeTextMinSize = wideLayout ? 14 : 17;
        orderTitle.resizeTextMaxSize = wideLayout ? 22 : shortPortrait ? 25 : 27;
        CreateCurrentOrderItemGrid(orderCard, primaryIndex);
        string orderFocusText = showingOrderComplete ? (string.IsNullOrEmpty(orderCompleteTitle) ? T("order.waiting") : orderCompleteTitle) : CurrentOrderStepText();
        Text orderFocus = CreateText("OrderFocus", orderCard, orderFocusText, wideLayout ? 17 : shortPortrait ? 20 : 22, FontStyle.Bold, hasSellableStock ? products[primaryIndex].Accent : showingOrderComplete ? honey : coral, TextAnchor.MiddleLeft);
        orderFocus.resizeTextForBestFit = true;
        orderFocus.resizeTextMinSize = wideLayout ? 12 : 14;
        orderFocus.resizeTextMaxSize = wideLayout ? 17 : shortPortrait ? 20 : 22;
        string orderStateText = showingOrderComplete ? T("order.waiting") : CurrentOrderNextStepText();
        Text orderState = CreateText("OrderState", orderCard, orderStateText, wideLayout ? 16 : shortPortrait ? 18 : 20, FontStyle.Bold, hasSellableStock ? coral : blue, TextAnchor.MiddleLeft);
        orderState.resizeTextForBestFit = true;
        orderState.resizeTextMinSize = wideLayout ? 11 : 13;
        orderState.resizeTextMaxSize = wideLayout ? 16 : shortPortrait ? 18 : 20;
        CreateProgress(orderCard, showingOrderComplete ? 0f : CurrentOrderCompletionProgress(), blue, wideLayout ? 7f : 8f);
        autoSaleFill = CreateProgress(orderCard, showingOrderComplete ? 0f : data.queue, coral, wideLayout ? 8f : 9f);

        if (orderCompleteTimer > 0f && !string.IsNullOrEmpty(orderCompleteMessage))
        {
            RectTransform completeBadge = CreateImagePanel("OrderCompleteBadge", orderCard, Mvp23Sprite(Mvp23FxOrderCompleteStamp), new Color(0.94f, 0.45f, 0.18f, 0.94f), true);
            LayoutElement completeBadgeLayout = completeBadge.gameObject.AddComponent<LayoutElement>();
            completeBadgeLayout.ignoreLayout = true;
            AnchorFill(completeBadge, 0.12f, 0.18f, 0.12f, 0.18f);
            Image completeImage = completeBadge.GetComponent<Image>();
            if (completeImage != null)
            {
                completeImage.raycastTarget = false;
            }

            Text completeText = CreateText("CompleteText", completeBadge, orderCompleteMessage, wideLayout ? 32 : shortPortrait ? 38 : 42, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            completeText.resizeTextForBestFit = true;
            completeText.resizeTextMinSize = wideLayout ? 20 : 25;
            completeText.resizeTextMaxSize = wideLayout ? 32 : shortPortrait ? 38 : 42;
            completeBadge.SetAsLastSibling();
        }

        RectTransform management = CreatePanel("ManagementPanel", contentRoot, new Color(0.36f, 0.23f, 0.14f));
        management.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 250f : shortPortrait ? 312f : 338f;
        VerticalLayoutGroup managementLayout = management.gameObject.AddComponent<VerticalLayoutGroup>();
        managementLayout.padding = wideLayout ? new RectOffset(14, 14, 12, 12) : new RectOffset(16, 16, 14, 14);
        managementLayout.spacing = wideLayout ? 8f : 10f;

        string primaryTitle = F("recommend.checkout", Money(nextItemValue));
        string primaryDetail = F("recommend.checkout_detail", CurrentOrderFocusText());
        string primaryCta = hasSellableStock ? T("action.quick_checkout") : T("action.restock_order");
        Sprite primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconCheckoutOne), coinSprite);
        Color primaryColor = coral;
        UnityEngine.Events.UnityAction primaryAction = () =>
        {
            CompleteOrderManualAndRender();
        };
        string primaryUnavailable = hasSellableStock ? string.Empty : CurrentOrderShortageText();

        bool shouldRecommendRestock = restockIndex >= 0 && (!hasSellableStock || lowStock);
        if (shouldRecommendRestock)
        {
            bool canRestockNow = affordablePriorityRestockAmount > 0;
            bool partialRestock = canRestockNow && affordablePriorityRestockAmount < priorityRestockAmount;
            if (orderHasMissing)
            {
                primaryTitle = restockIndex >= 0 ? F("recommend.restock_focus", ProductName(restockIndex), Money(priorityRestockCost)) : F("recommend.restock_order", Money(orderMissingCost));
                primaryDetail = F("recommend.restock_order_detail", CurrentOrderShortageText());
                primaryCta = restockIndex < 0 ? T("action.full_stock") : !canRestockNow ? ShortCashShort(cheapestRestockCost - data.cash) : RestockButtonTitle(restockIndex, true);
            }
            else
            {
                primaryTitle = partialRestock ? F("recommend.restock_partial", affordablePriorityRestockAmount, Money(affordablePriorityRestockCost)) : F("recommend.restock", Money(priorityRestockCost));
                primaryDetail = restockIndex < 0 ? T("action.full_stock") : !canRestockNow ? F("log.cash_short", Money(cheapestRestockCost - data.cash)) : T("recommend.restock_detail");
                primaryCta = restockIndex < 0 || priorityRestockAmount <= 0 ? T("action.full_stock") : !canRestockNow ? ShortCashShort(cheapestRestockCost - data.cash) : RestockButtonTitle(restockIndex, false);
            }
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconRestockItem), shelfSprite, upgradeSprite);
            primaryColor = blue;
            primaryAction = BuyRestockAll;
            primaryUnavailable = restockIndex < 0 || priorityRestockAmount <= 0 ? T("action.full_stock") : !canRestockNow ? F("log.cash_short", Money(Mathf.Max(0f, cheapestRestockCost - data.cash))) : string.Empty;
        }
        else if (!hasSellableStock && upgradeIndex >= 0 && data.cash >= upgradeCost)
        {
            primaryTitle = F("recommend.upgrade", UpgradeName(upgradeIndex), Money(upgradeCost));
            primaryDetail = UpgradeImpactText(upgradeIndex, primaryIndex);
            primaryCta = F("action.upgrade", Money(upgradeCost));
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconUpgradeProduct), upgradeSprite);
            primaryColor = honey;
            primaryAction = () =>
            {
                BuyUpgrade(upgradeIndex);
            };
            primaryUnavailable = string.Empty;
        }
        else if (!hasSellableStock && ShouldRecommendStaff() && staffIndex >= 0 && data.cash >= staffCost)
        {
            primaryTitle = F("recommend.staff", StaffName(staffIndex), Money(staffCost));
            primaryDetail = StaffAutomationText(staffIndex);
            primaryCta = F("action.train", Money(staffCost));
            primaryIcon = StaffSprite(staffIndex) != null ? StaffSprite(staffIndex) : customerSprite;
            primaryColor = pine;
            primaryAction = () =>
            {
                HireStaff(staffIndex);
            };
            primaryUnavailable = string.Empty;
        }
        else if (upgradeIndex >= 0 && hasSellableStock && data.cash >= upgradeCost * UpgradeGoalRatio)
        {
            primaryTitle = F("recommend.upgrade_goal", UpgradeName(upgradeIndex), Money(upgradeCost));
            primaryDetail = F("recommend.upgrade_goal_detail", UpgradeImpactText(upgradeIndex, primaryIndex), Money(upgradeCost - data.cash));
            primaryCta = T("action.quick_checkout");
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconUpgradeProduct), upgradeSprite);
            primaryColor = honey;
            primaryAction = () =>
            {
                CompleteOrderManualAndRender();
            };
            primaryUnavailable = string.Empty;
        }
        else if (ShouldRecommendStaff() && staffIndex >= 0 && hasSellableStock && data.cash >= staffCost * StaffGoalRatio)
        {
            primaryTitle = F("recommend.staff_goal", StaffName(staffIndex), Money(staffCost));
            primaryDetail = F("recommend.staff_goal_detail", StaffAutomationText(staffIndex), Money(staffCost - data.cash));
            primaryCta = T("action.quick_checkout");
            primaryIcon = StaffSprite(staffIndex) != null ? StaffSprite(staffIndex) : customerSprite;
            primaryColor = pine;
            primaryAction = () =>
            {
                CompleteOrderManualAndRender();
            };
            primaryUnavailable = string.Empty;
        }

        int milestoneIndex = CurrentMilestoneIndex();
        if (milestoneIndex == 0 && hasSellableStock)
        {
            primaryTitle = CurrentMilestoneText();
            primaryDetail = CurrentMilestoneHint();
            primaryCta = T("action.quick_checkout");
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconCheckoutOne), coinSprite);
            primaryColor = coral;
            primaryAction = CompleteOrderManualAndRender;
            primaryUnavailable = string.Empty;
        }
        else if (milestoneIndex == 1 && (!hasSellableStock || orderHasMissing || lowStock))
        {
            bool canRestockNow = restockIndex >= 0 && affordablePriorityRestockAmount > 0;
            bool partialRestock = canRestockNow && affordablePriorityRestockAmount < priorityRestockAmount;
            primaryTitle = CurrentMilestoneText();
            primaryDetail = orderHasMissing ? F("recommend.restock_order_detail", CurrentOrderShortageText()) : restockIndex < 0 ? T("action.full_stock") : !canRestockNow ? F("log.cash_short", Money(Mathf.Max(0f, cheapestRestockCost - data.cash))) : T("recommend.restock_detail");
            primaryCta = restockIndex < 0 ? T("action.full_stock") : canRestockNow ? RestockButtonTitle(restockIndex, orderHasMissing) : hasSellableStock ? T("action.quick_checkout") : ShortCashShort(cheapestRestockCost - data.cash);
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconRestockItem), shelfSprite, upgradeSprite);
            primaryColor = blue;
            if (canRestockNow)
            {
                primaryAction = BuyRestockAll;
            }
            else if (hasSellableStock)
            {
                primaryAction = CompleteOrderManualAndRender;
            }
            else
            {
                primaryAction = BuyRestockAll;
            }

            primaryUnavailable = restockIndex < 0 ? T("action.full_stock") : !canRestockNow && !hasSellableStock ? F("log.cash_short", Money(Mathf.Max(0f, cheapestRestockCost - data.cash))) : string.Empty;
        }
        else if (milestoneIndex == 2 && hasSellableStock)
        {
            primaryTitle = CurrentMilestoneText();
            primaryDetail = CurrentMilestoneHint();
            primaryCta = T("action.quick_checkout");
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconCheckoutOne), coinSprite);
            primaryColor = coral;
            primaryAction = CompleteOrderManualAndRender;
            primaryUnavailable = string.Empty;
        }
        else if (milestoneIndex == 3 && !hasSellableStock && !orderHasMissing && data.upgrades.Length > 2 && data.upgrades[2] < upgrades[2].MaxLevel)
        {
            int priceUpgradeCost = UpgradeCost(2);
            primaryTitle = data.cash >= priceUpgradeCost ? F("recommend.upgrade", UpgradeName(2), Money(priceUpgradeCost)) : F("recommend.upgrade_goal", UpgradeName(2), Money(priceUpgradeCost));
            primaryDetail = data.cash >= priceUpgradeCost ? UpgradeImpactText(2, primaryIndex) : F("recommend.upgrade_goal_detail", UpgradeImpactText(2, primaryIndex), Money(priceUpgradeCost - data.cash));
            primaryCta = data.cash >= priceUpgradeCost ? F("action.upgrade", Money(priceUpgradeCost)) : hasSellableStock ? T("action.quick_checkout") : restockIndex >= 0 ? RestockButtonTitle(restockIndex, orderHasMissing) : T("guide.restock");
            primaryIcon = FirstSprite(Mvp23Sprite(Mvp23IconUpgradeProduct), upgradeSprite);
            primaryColor = honey;
            if (data.cash >= priceUpgradeCost)
            {
                primaryAction = () =>
                {
                    BuyUpgrade(2);
                };
            }
            else if (hasSellableStock)
            {
                primaryAction = CompleteOrderManualAndRender;
            }
            else
            {
                primaryAction = BuyRestockAll;
            }

            primaryUnavailable = !hasSellableStock && data.cash < priceUpgradeCost && restockIndex < 0 ? T("log.empty_shelf") : string.Empty;
        }
        else if (milestoneIndex == 4 && !hasSellableStock && !orderHasMissing && data.staff.Length > 0 && data.staff[0] < staffDefs[0].MaxLevel)
        {
            int cashierCost = StaffCost(0);
            primaryTitle = data.cash >= cashierCost ? F("recommend.staff", StaffName(0), Money(cashierCost)) : F("recommend.staff_goal", StaffName(0), Money(cashierCost));
            primaryDetail = data.cash >= cashierCost ? StaffAutomationText(0) : F("recommend.staff_goal_detail", StaffAutomationText(0), Money(cashierCost - data.cash));
            primaryCta = data.cash >= cashierCost ? F("action.train", Money(cashierCost)) : hasSellableStock ? T("action.quick_checkout") : restockIndex >= 0 ? RestockButtonTitle(restockIndex, orderHasMissing) : T("guide.restock");
            primaryIcon = StaffSprite(0) != null ? StaffSprite(0) : customerSprite;
            primaryColor = pine;
            if (data.cash >= cashierCost)
            {
                primaryAction = () =>
                {
                    HireStaff(0);
                };
            }
            else if (hasSellableStock)
            {
                primaryAction = CompleteOrderManualAndRender;
            }
            else
            {
                primaryAction = BuyRestockAll;
            }

            primaryUnavailable = !hasSellableStock && data.cash < cashierCost && restockIndex < 0 ? T("log.empty_shelf") : string.Empty;
        }

        CreateRecommendationBanner(management, primaryTitle, primaryDetail, primaryIcon, primaryColor);

        RectTransform quickActions = CreateRect("QuickActions", management);
        quickActions.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 102f : shortPortrait ? 126f : 138f;
        HorizontalLayoutGroup quickLayout = quickActions.gameObject.AddComponent<HorizontalLayoutGroup>();
        quickLayout.spacing = wideLayout ? 10f : 12f;
        quickLayout.childAlignment = TextAnchor.MiddleCenter;
        quickLayout.childControlWidth = true;
        quickLayout.childControlHeight = true;
        quickLayout.childForceExpandWidth = true;
        quickLayout.childForceExpandHeight = true;

        bool checkoutEnabled = hasSellableStock;
        bool restockEnabled = restockIndex >= 0 && priorityRestockAmount > 0 && cheapestRestockCost > 0 && data.cash >= cheapestRestockCost;
        bool upgradeEnabled = upgradeIndex >= 0 && data.cash >= upgradeCost;

        CreateQuickActionButton(quickActions, T("action.quick_checkout"), hasSellableStock ? F("action.checkout_one_detail", ProductName(primaryIndex), Money(nextItemValue)) : CurrentOrderShortageText(), FirstSprite(Mvp23Sprite(Mvp23IconCheckoutOne), coinSprite), coral, () =>
        {
            if (!HasSellableStock())
            {
                AddLog(CurrentOrderShortageText());
                RenderAll();
                return;
            }

            CompleteOrderManualAndRender();
        }, checkoutEnabled);

        CreateQuickActionButton(quickActions, RestockButtonTitle(restockIndex, orderHasMissing), RestockAllButtonDetail(restockIndex, priorityRestockAmount, priorityRestockCost, cheapestRestockCost, affordablePriorityRestockAmount, affordablePriorityRestockCost), FirstSprite(Mvp23Sprite(Mvp23IconRestockItem), shelfSprite, upgradeSprite), blue, () =>
        {
            if (restockIndex < 0 || priorityRestockAmount <= 0)
            {
                AddLog(T("action.full_stock"));
                RenderAll();
                return;
            }

            if (cheapestRestockCost <= 0 || data.cash < cheapestRestockCost)
            {
                AddLog(F("log.cash_short", Money(Mathf.Max(0f, cheapestRestockCost - data.cash))));
                RenderAll();
                return;
            }

            BuyRestockAll();
        }, restockEnabled);

        CreateQuickActionButton(quickActions, upgradeIndex >= 0 ? T("tab.upgrades") : T("action.maxed"), UpgradeButtonDetail(upgradeIndex, upgradeCost), FirstSprite(Mvp23Sprite(Mvp23IconUpgradeProduct), upgradeSprite), honey, () =>
        {
            if (upgradeIndex < 0)
            {
                AddLog(T("action.maxed"));
                RenderAll();
                return;
            }

            if (data.cash < upgradeCost)
            {
                AddLog(F("log.upgrade_short", UpgradeName(upgradeIndex), Money(upgradeCost - data.cash)));
                RenderAll();
                return;
            }

            BuyUpgrade(upgradeIndex);
        }, upgradeEnabled);

        RectTransform day = CreatePanel("Day", contentRoot, panel);
        day.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 86f : shortPortrait ? 102f : 112f;
        VerticalLayoutGroup dayLayout = day.gameObject.AddComponent<VerticalLayoutGroup>();
        dayLayout.padding = wideLayout ? new RectOffset(16, 16, 8, 8) : new RectOffset(18, 18, 9, 9);
        dayLayout.spacing = wideLayout ? 3f : 4f;
        CreateText("DayTitle", day, F("milestone.panel_title", data.day), wideLayout ? 18 : shortPortrait ? 20 : 22, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        Text milestone = CreateText("Milestone", day, CurrentMilestoneText(), wideLayout ? 16 : shortPortrait ? 18 : 20, FontStyle.Bold, blue, TextAnchor.MiddleLeft);
        milestone.resizeTextForBestFit = true;
        milestone.resizeTextMinSize = wideLayout ? 11 : 13;
        milestone.resizeTextMaxSize = wideLayout ? 16 : shortPortrait ? 18 : 20;
        CreateProgress(day, MilestoneProgress(CurrentMilestoneIndex()), blue, wideLayout ? 8f : 9f);
        string latestLog = log.Count > 0 ? log[0] : CurrentMilestoneHint();
        Text hint = CreateText("MilestoneHint", day, latestLog, wideLayout ? 14 : shortPortrait ? 16 : 18, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        hint.resizeTextForBestFit = true;
        hint.resizeTextMinSize = wideLayout ? 10 : 12;
        hint.resizeTextMaxSize = wideLayout ? 14 : shortPortrait ? 16 : 18;
    }

    private void CreateCurrentOrderItemGrid(RectTransform parent, int primaryIndex)
    {
        bool shortPortrait = IsShortPortraitScreen();
        RectTransform grid = CreateRect("OrderItems", parent);
        LayoutElement gridLayoutElement = grid.gameObject.AddComponent<LayoutElement>();
        gridLayoutElement.preferredHeight = wideLayout ? 66f : shortPortrait ? 76f : 82f;
        gridLayoutElement.flexibleHeight = 0f;

        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.spacing = wideLayout ? new Vector2(6f, 4f) : new Vector2(8f, 4f);
        gridLayout.cellSize = wideLayout ? new Vector2(248f, 30f) : shortPortrait ? new Vector2(286f, 35f) : new Vector2(312f, 38f);

        for (int i = 0; i < products.Length; i++)
        {
            int quantity = CurrentOrderInitialQuantity(i);
            if (quantity <= 0)
            {
                continue;
            }

            CreateOrderItemButton(grid, i, i == primaryIndex);
        }
    }

    private Button CreateOrderItemButton(RectTransform parent, int productIndex, bool focused)
    {
        int initialQuantity = CurrentOrderInitialQuantity(productIndex);
        int quantity = CurrentOrderQuantity(productIndex);
        int completed = CurrentOrderCompletedQuantity(productIndex);
        int stock = Mathf.FloorToInt(data.stock[productIndex]);
        bool done = initialQuantity > 0 && quantity <= 0;
        bool missing = quantity > 0 && stock <= 0;
        bool low = quantity > 0 && !missing && stock < quantity;
        Color color = focused ? products[productIndex].Accent : missing ? new Color(0.72f, 0.22f, 0.18f) : low ? honey : done ? new Color(0.46f, 0.68f, 0.50f, 0.96f) : new Color(1f, 0.95f, 0.84f, 0.96f);
        Color textColor = focused || missing || low || done ? Color.white : pine;
        Color detailColor = focused || missing || low || done ? new Color(1f, 0.95f, 0.78f) : ink;

        RectTransform row = CreateImagePanel("OrderItemButton", parent, Mvp23Sprite(Mvp23UiOrderItemSlot), color, false);
        if (focused)
        {
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.91f, 0.28f, 0.88f);
            outline.effectDistance = wideLayout ? new Vector2(2f, -2f) : new Vector2(3f, -3f);
            if (orderSelectPulseTimer > 0f)
            {
                row.gameObject.AddComponent<UIFlashTint>().Configure(new Color(1f, 0.88f, 0.25f), orderSelectPulseTimer);
            }
            else if (productIndex == recentSoldProductIndex && salePopTimer > 0f)
            {
                row.gameObject.AddComponent<UIFlashTint>().Configure(coral, salePopTimer);
            }
            else if (productIndex == recentRestockProductIndex && restockPulseTimer > 0f)
            {
                row.gameObject.AddComponent<UIFlashTint>().Configure(blue, restockPulseTimer);
            }
        }
        else if (productIndex == recentSoldProductIndex && salePopTimer > 0f)
        {
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.48f, 0.30f, 0.82f);
            outline.effectDistance = wideLayout ? new Vector2(2f, -2f) : new Vector2(3f, -3f);
            row.gameObject.AddComponent<UIFlashTint>().Configure(coral, salePopTimer);
        }
        else if (productIndex == recentRestockProductIndex && restockPulseTimer > 0f)
        {
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.38f, 0.78f, 1f, 0.82f);
            outline.effectDistance = wideLayout ? new Vector2(2f, -2f) : new Vector2(3f, -3f);
            row.gameObject.AddComponent<UIFlashTint>().Configure(blue, restockPulseTimer);
        }

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(5, 7, 3, 3) : new RectOffset(6, 8, 3, 3);
        layout.spacing = wideLayout ? 4f : 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        RectTransform iconBox = CreatePanel("OrderItemIcon", row, focused || missing || low ? new Color(1f, 1f, 1f, 0.22f) : products[productIndex].Accent);
        iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 24f : 30f;
        if (ProductSprite(productIndex) != null)
        {
            CreateImage("Icon", iconBox, ProductSprite(productIndex), true);
        }

        if (missing && Mvp23Sprite(Mvp23UiStockWarningBadge) != null)
        {
            RectTransform warning = CreateImagePanel("StockWarning", row, Mvp23Sprite(Mvp23UiStockWarningBadge), Color.clear, true);
            LayoutElement warningLayout = warning.gameObject.AddComponent<LayoutElement>();
            warningLayout.preferredWidth = wideLayout ? 22f : 26f;
            warningLayout.flexibleWidth = 0f;
        }

        RectTransform textGroup = CreateRect("OrderItemText", row);
        textGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 0f;
        textLayout.childControlHeight = true;
        textLayout.childControlWidth = true;
        textLayout.childForceExpandHeight = false;

        Text name = CreateText("Name", textGroup, F("order.item", ProductName(productIndex), initialQuantity), wideLayout ? 13 : 15, FontStyle.Bold, textColor, TextAnchor.MiddleLeft);
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = wideLayout ? 9 : 10;
        name.resizeTextMaxSize = wideLayout ? 13 : 15;

        string stockState = done ? T("order.item_done") : missing ? F("order.item_missing", quantity) : low ? F("order.item_low", stock, quantity) : F("order.item_ready", Mathf.Min(stock, quantity), quantity, Money(CurrentOrderItemValue(productIndex)));
        string status = $"{ProductTierShortLabel(productIndex)} · {F("order.item_progress", completed, initialQuantity)} · {stockState}";
        if (productIndex == recentSoldProductIndex && salePopTimer > 0f)
        {
            status = $"{ProductTierShortLabel(productIndex)} · {F("order.item_progress", completed, initialQuantity)} · {F("order.item_sold_feedback", quantity)}";
        }

        Text detail = CreateText("Status", textGroup, focused ? $"{T("order.item_selected")} · {status}" : status, wideLayout ? 10 : 12, FontStyle.Bold, detailColor, TextAnchor.MiddleLeft);
        detail.resizeTextForBestFit = true;
        detail.resizeTextMinSize = wideLayout ? 8 : 9;
        detail.resizeTextMaxSize = wideLayout ? 10 : 12;

        RectTransform lineProgress = CreateProgress(textGroup, initialQuantity <= 0 ? 0f : completed / (float)initialQuantity, focused ? Color.white : products[productIndex].Accent, wideLayout ? 4f : 5f);
        LayoutElement progressLayout = lineProgress.parent != null ? lineProgress.parent.GetComponent<LayoutElement>() : null;
        if (progressLayout != null)
        {
            progressLayout.preferredHeight = wideLayout ? 4f : 5f;
        }

        Button button = row.gameObject.AddComponent<Button>();
        row.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = row.GetComponent<Image>();
        int captured = productIndex;
        button.onClick.AddListener(() => SelectOrderProductAndRender(captured));
        ApplyButtonColor(button, color);
        if (focused && Mvp23Sprite(Mvp23UiCurrentItemFrame) != null)
        {
            RectTransform frame = CreateImagePanel("CurrentItemFrame", row, Mvp23Sprite(Mvp23UiCurrentItemFrame), Color.clear, false);
            Image frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.raycastTarget = false;
            }

            LayoutElement frameLayout = frame.gameObject.AddComponent<LayoutElement>();
            frameLayout.ignoreLayout = true;
            AnchorFill(frame, 0f, 0f, 0f, 0f);
            frame.SetAsLastSibling();
        }

        if (focused && Mvp23Sprite(Mvp23FxItemSelectedGlow) != null)
        {
            RectTransform glow = CreateImagePanel("ItemSelectedGlow", row, Mvp23Sprite(Mvp23FxItemSelectedGlow), Color.clear, false);
            Image glowImage = glow.GetComponent<Image>();
            if (glowImage != null)
            {
                glowImage.raycastTarget = false;
                glowImage.color = new Color(1f, 1f, 1f, 0.78f);
            }

            LayoutElement glowLayout = glow.gameObject.AddComponent<LayoutElement>();
            glowLayout.ignoreLayout = true;
            AnchorFill(glow, -0.02f, -0.04f, -0.02f, -0.04f);
            glow.SetAsLastSibling();
        }

        return button;
    }

    private void CreateGuidePanel()
    {
        RectTransform guide = CreatePanel("GuidePanel", contentRoot, panel);
        guide.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 148f : 178f;
        VerticalLayoutGroup guideLayout = guide.gameObject.AddComponent<VerticalLayoutGroup>();
        guideLayout.padding = wideLayout ? new RectOffset(20, 20, 14, 14) : new RectOffset(24, 24, 18, 18);
        guideLayout.spacing = wideLayout ? 10f : 12f;

        CreateText("GuideTitle", guide, T("guide.title"), wideLayout ? 34 : 40, FontStyle.Bold, pine, TextAnchor.MiddleLeft);

        RectTransform items = CreateRect("GuideItems", guide);
        items.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup row = items.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = wideLayout ? 10f : 12f;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        CreateGuideItem(items, T("guide.sell"), coral, coinSprite);
        CreateGuideItem(items, T("guide.restock"), blue, shelfSprite != null ? shelfSprite : upgradeSprite);
        CreateGuideItem(items, T("guide.grow"), honey, upgradeSprite);
    }

    private void CreateGuideItem(RectTransform parent, string label, Color color, Sprite icon)
    {
        RectTransform item = CreatePanel("GuideItem", parent, color);
        item.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        HorizontalLayoutGroup layout = item.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(12, 12, 9, 9) : new RectOffset(14, 14, 11, 11);
        layout.spacing = wideLayout ? 9f : 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        if (icon != null)
        {
            RectTransform iconBox = CreateImagePanel("GuideIcon", item, icon, Color.clear);
            iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 38f : 44f;
        }

        Text text = CreateText("GuideLabel", item, label, wideLayout ? 26 : 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = wideLayout ? 16 : 19;
        text.resizeTextMaxSize = wideLayout ? 26 : 30;
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private string RestockButtonTitle(int restockIndex, bool orderHasMissing)
    {
        if (restockIndex < 0)
        {
            return T("action.full_stock");
        }

        int cost = PriorityRestockAmount(restockIndex) * UnitCost(restockIndex);
        return orderHasMissing ? F("action.restock_current_missing", ProductName(restockIndex), Money(cost)) : F("action.restock_current_stock", ProductName(restockIndex), Money(cost));
    }

    private string RestockAllButtonDetail(int restockIndex, int amount, int totalCost, int cheapestCost, int affordableAmount, int affordableCost)
    {
        if (restockIndex < 0 || amount <= 0)
        {
            return T("action.full_stock");
        }

        if (cheapestCost <= 0 || affordableAmount <= 0)
        {
            return ShortCashShort(cheapestCost - data.cash);
        }

        int missingOrderCost = CurrentOrderMissingCost();
        if (!CanFulfillCurrentOrder() && missingOrderCost > 0)
        {
            if (data.cash < missingOrderCost)
            {
                return F("action.restock_order_partial_detail", affordableAmount, Money(affordableCost));
            }

            return F("action.restock_order_detail", Money(missingOrderCost));
        }

        if (affordableAmount < amount)
        {
            return F("action.restock_affordable_detail", affordableAmount, Money(affordableCost));
        }

        return F("action.restock_product_detail", ProductName(restockIndex), amount, Money(totalCost));
    }

    private string StockButtonLabel(int productIndex, bool locked, int amount, int cost)
    {
        if (locked)
        {
            return F("stock.unlock_at", ProductName(productIndex), products[productIndex].UnlockLevel);
        }

        if (amount <= 0)
        {
            return T("action.full_stock");
        }

        if (data.cash < cost)
        {
            return ShortCashShort(cost - data.cash);
        }

        return F("action.restock", Money(cost));
    }

    private string UpgradeButtonDetail(int upgradeIndex, int cost)
    {
        if (upgradeIndex < 0)
        {
            return T("action.maxed");
        }

        if (data.cash < cost)
        {
            return ShortCashShort(cost - data.cash);
        }

        return UpgradeImpactText(upgradeIndex, PrimaryProductIndex());
    }

    private string UpgradeButtonLabel(int upgradeIndex, bool maxed, int cost)
    {
        if (maxed)
        {
            return T("action.maxed");
        }

        if (data.cash < cost)
        {
            return ShortCashShort(cost - data.cash);
        }

        return F("action.upgrade", Money(cost));
    }

    private string StaffButtonLabel(bool maxed, int cost)
    {
        if (maxed)
        {
            return T("action.maxed");
        }

        if (data.cash < cost)
        {
            return ShortCashShort(cost - data.cash);
        }

        return F("action.train", Money(cost));
    }

    private void RenderStock()
    {
        CreateSectionTitle(T("stock.title"), F("stock.subtitle", RestockPack()));
        bool shortPortrait = IsShortPortraitScreen();
        for (int i = 0; i < products.Length; i++)
        {
            int index = i;
            ProductDef product = products[i];
            bool locked = !data.unlocked[i];
            RectTransform row = CreatePanel("Product", contentRoot, locked ? new Color(0.88f, 0.86f, 0.82f) : panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 164f : shortPortrait ? 182f : 198f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = wideLayout ? new RectOffset(18, 18, 14, 14) : shortPortrait ? new RectOffset(18, 18, 14, 14) : new RectOffset(20, 20, 16, 16);
            layout.spacing = wideLayout ? 16f : shortPortrait ? 14f : 18f;

            RectTransform swatch = CreatePanel("Swatch", row, locked ? Color.gray : product.Accent);
            swatch.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 108f : shortPortrait ? 110f : 124f;
            if (!locked && ProductSprite(i) != null)
            {
                CreateImage("ProductIcon", swatch, ProductSprite(i), true);
            }
            else
            {
                CreateText("ProductIcon", swatch, locked ? "?" : T("shop.stock_icon"), wideLayout ? 38 : 44, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            }

            RectTransform info = CreateRect("Info", row);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup infoLayout = info.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = wideLayout ? 7f : shortPortrait ? 7f : 9f;
            CreateText("Name", info, locked ? F("stock.unlock_at", ProductName(i), product.UnlockLevel) : ProductName(i), wideLayout ? 30 : shortPortrait ? 32 : 36, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Stock", info, locked ? T("stock.wait_level") : F("stock.stock_count", Mathf.FloorToInt(data.stock[i]), MaxStock(i)), wideLayout ? 26 : shortPortrait ? 29 : 32, FontStyle.Bold, locked ? ink : product.Accent, TextAnchor.MiddleLeft);
            CreateProgress(info, locked ? 0f : data.stock[i] / MaxStock(i), locked ? Color.gray : product.Accent, wideLayout ? 16f : shortPortrait ? 18f : 20f);
            CreateText("Meta", info, locked ? ProductSubtitle(i) : ProductEconomyText(i), wideLayout ? 22 : shortPortrait ? 24 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

            int amount = Mathf.Min(MaxStock(i) - Mathf.FloorToInt(data.stock[i]), RestockPack());
            int cost = amount * UnitCost(i);
            Button button = CreateButton(row, StockButtonLabel(index, locked, amount, cost), pine, () =>
            {
                if (locked)
                {
                    AddLog(F("stock.unlock_at", ProductName(index), product.UnlockLevel));
                    RenderAll();
                    return;
                }

                if (amount <= 0)
                {
                    AddLog(T("action.full_stock"));
                    RenderAll();
                    return;
                }

                if (data.cash < cost)
                {
                    AddLog(F("log.cash_short", Money(cost - data.cash)));
                    RenderAll();
                    return;
                }

                BuyStock(index);
            });
            button.gameObject.GetComponent<LayoutElement>().preferredWidth = wideLayout ? 210f : shortPortrait ? 218f : 250f;
            SetButtonAvailability(button, !locked && amount > 0 && data.cash >= cost, pine);
        }
    }

    private void RenderUpgrades()
    {
        CreateSectionTitle(T("upgrades.title"), T("upgrades.subtitle"));
        bool shortPortrait = IsShortPortraitScreen();
        for (int i = 0; i < upgrades.Length; i++)
        {
            int index = i;
            UpgradeDef upgrade = upgrades[i];
            int level = data.upgrades[i];
            int cost = UpgradeCost(i);
            bool maxed = level >= upgrade.MaxLevel;
            RectTransform row = CreatePanel("Upgrade", contentRoot, panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 174f : shortPortrait ? 194f : 214f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = wideLayout ? new RectOffset(18, 18, 14, 14) : shortPortrait ? new RectOffset(18, 18, 14, 14) : new RectOffset(20, 20, 16, 16);
            layout.spacing = wideLayout ? 18f : shortPortrait ? 16f : 20f;

            RectTransform info = CreateRect("Info", row);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup infoLayout = info.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = wideLayout ? 8f : shortPortrait ? 9f : 11f;
            CreateText("Name", info, $"{UpgradeName(i)}  {F("level.short", level, upgrade.MaxLevel)}", wideLayout ? 31 : shortPortrait ? 34 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Detail", info, UpgradeImpactText(i, PrimaryProductIndex()), wideLayout ? 29 : shortPortrait ? 32 : 35, FontStyle.Bold, honey, TextAnchor.MiddleLeft);
            CreateText("Meta", info, maxed ? T("action.maxed") : F("upgrades.next_cost", Money(cost)), wideLayout ? 22 : shortPortrait ? 24 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

            Button button = CreateButton(row, UpgradeButtonLabel(index, maxed, cost), honey, () =>
            {
                if (maxed)
                {
                    AddLog(T("action.maxed"));
                    RenderAll();
                    return;
                }

                if (data.cash < cost)
                {
                    AddLog(F("log.upgrade_short", UpgradeName(index), Money(cost - data.cash)));
                    RenderAll();
                    return;
                }

                BuyUpgrade(index);
            });
            button.gameObject.GetComponent<LayoutElement>().preferredWidth = wideLayout ? 240f : shortPortrait ? 250f : 280f;
            SetButtonAvailability(button, !maxed && data.cash >= cost, honey);
        }
    }

    private void RenderStaff()
    {
        CreateSectionTitle(T("staff.title"), T("staff.subtitle"));
        bool shortPortrait = IsShortPortraitScreen();
        for (int i = 0; i < staffDefs.Length; i++)
        {
            int index = i;
            StaffDef person = staffDefs[i];
            int level = data.staff[i];
            int cost = StaffCost(i);
            bool maxed = level >= person.MaxLevel;
            RectTransform row = CreatePanel("Staff", contentRoot, panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 182f : shortPortrait ? 204f : 226f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = wideLayout ? new RectOffset(18, 18, 14, 14) : shortPortrait ? new RectOffset(18, 18, 14, 14) : new RectOffset(20, 20, 16, 16);
            layout.spacing = wideLayout ? 18f : shortPortrait ? 16f : 20f;

            RectTransform avatar = CreatePanel("Avatar", row, i == 0 ? pine : i == 1 ? new Color(0.16f, 0.36f, 0.27f) : new Color(0.28f, 0.43f, 0.22f));
            avatar.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 126f : shortPortrait ? 126f : 142f;
            if (StaffSprite(i) != null)
            {
                CreateImage("AvatarArt", avatar, StaffSprite(i), true);
            }
            else
            {
                CreateText("AvatarText", avatar, T("staff.avatar"), wideLayout ? 42 : 48, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            }

            RectTransform info = CreateRect("Info", row);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup infoLayout = info.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = wideLayout ? 8f : shortPortrait ? 9f : 11f;
            CreateText("Name", info, $"{StaffName(i)}  {F("level.short", level, person.MaxLevel)}", wideLayout ? 31 : shortPortrait ? 34 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Detail", info, StaffAutomationText(i), wideLayout ? 29 : shortPortrait ? 32 : 35, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Meta", info, maxed ? T("action.maxed") : F("action.train", Money(cost)), wideLayout ? 22 : shortPortrait ? 24 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

            Button button = CreateButton(row, StaffButtonLabel(maxed, cost), pine, () =>
            {
                if (maxed)
                {
                    AddLog(T("action.maxed"));
                    RenderAll();
                    return;
                }

                if (data.cash < cost)
                {
                    AddLog(F("log.staff_short", StaffName(index), Money(cost - data.cash)));
                    RenderAll();
                    return;
                }

                HireStaff(index);
            });
            button.gameObject.GetComponent<LayoutElement>().preferredWidth = wideLayout ? 240f : shortPortrait ? 250f : 280f;
            SetButtonAvailability(button, !maxed && data.cash >= cost, pine);
        }
    }

    private void RenderSettings()
    {
        CreateSectionTitle(T("settings.title"), T("settings.subtitle"));

        RectTransform languagePanel = CreatePanel("LanguagePanel", contentRoot, panel);
        languagePanel.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 520f : 600f;
        VerticalLayoutGroup languageLayout = languagePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        languageLayout.padding = wideLayout ? new RectOffset(24, 24, 18, 18) : new RectOffset(26, 26, 22, 22);
        languageLayout.spacing = wideLayout ? 12f : 14f;
        CreateText("LanguageTitle", languagePanel, T("settings.language"), wideLayout ? 32 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateText("LanguageCurrent", languagePanel, F("settings.current_language", CurrentLanguageName()), wideLayout ? 24 : 28, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

        RectTransform languageButtons = CreateRect("LanguageButtons", languagePanel);
        languageButtons.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 305f : 360f;
        GridLayoutGroup buttonLayout = languageButtons.gameObject.AddComponent<GridLayoutGroup>();
        buttonLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        buttonLayout.constraintCount = 3;
        buttonLayout.spacing = wideLayout ? new Vector2(10f, 10f) : new Vector2(12f, 12f);
        buttonLayout.cellSize = wideLayout ? new Vector2(318f, 64f) : new Vector2(310f, 76f);

        IReadOnlyList<LocalizationManager.LanguageInfo> languages = LocalizationManager.AvailableLanguages;
        for (int i = 0; i < languages.Count; i++)
        {
            LocalizationManager.LanguageInfo language = languages[i];
            string languageLabel = language.Code == LocalizationManager.CurrentLanguage ? F("settings.current_language", language.Name) : language.Name;
            CreateButton(languageButtons, languageLabel, language.Code == LocalizationManager.CurrentLanguage ? coral : pine, () =>
            {
                if (language.Code == LocalizationManager.CurrentLanguage)
                {
                    AddLog(F("settings.current_language", language.Name));
                    RenderAll();
                    return;
                }

                ChangeLanguage(language.Code, language.Name);
            });
        }

        CreateText("LanguageHint", languagePanel, T("settings.language_hint"), wideLayout ? 22 : 25, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        CreateNewPlayerTestPanel();

        RectTransform resetPanel = CreatePanel("ResetPanel", contentRoot, panel);
        resetPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 190f : 230f;
        VerticalLayoutGroup resetLayout = resetPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        resetLayout.padding = wideLayout ? new RectOffset(24, 24, 18, 18) : new RectOffset(26, 26, 22, 22);
        resetLayout.spacing = wideLayout ? 12f : 14f;
        CreateText("ResetTitle", resetPanel, T("settings.reset_title"), wideLayout ? 32 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateText("ResetBody", resetPanel, T("settings.reset_body"), wideLayout ? 23 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        CreateButton(resetPanel, T("action.reset_save"), coral, ResetSave);
    }

    private void CreateNewPlayerTestPanel()
    {
        bool shortPortrait = IsShortPortraitScreen();
        RectTransform testPanel = CreatePanel("TestPanel", contentRoot, panel);
        testPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 220f : shortPortrait ? 260f : 292f;
        VerticalLayoutGroup layout = testPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(24, 24, 18, 18) : new RectOffset(26, 26, 22, 22);
        layout.spacing = wideLayout ? 10f : 12f;

        CreateText("TestTitle", testPanel, T("settings.test_title"), wideLayout ? 32 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        string firstUpgradeState = data.upgrades.Length > 2 && data.upgrades[2] >= 1 ? T("settings.test_done") : T("settings.test_pending");
        int milestoneIndex = CurrentMilestoneIndex();
        string milestone = milestoneIndex >= 0 ? MilestoneName(milestoneIndex) : T("milestone.all_done");
        Text report = CreateText("TestReport", testPanel, F("settings.test_report", data.completedOrders, data.restockActions, firstUpgradeState, AutoSaleInterval().ToString("0.0", CultureInfo.InvariantCulture), milestone), wideLayout ? 23 : shortPortrait ? 26 : 29, FontStyle.Bold, ink, TextAnchor.MiddleLeft);
        report.resizeTextForBestFit = true;
        report.resizeTextMinSize = wideLayout ? 15 : 18;
        report.resizeTextMaxSize = wideLayout ? 23 : shortPortrait ? 26 : 29;
    }

    private string CurrentLanguageName()
    {
        IReadOnlyList<LocalizationManager.LanguageInfo> languages = LocalizationManager.AvailableLanguages;
        for (int i = 0; i < languages.Count; i++)
        {
            if (languages[i].Code == LocalizationManager.CurrentLanguage)
            {
                return languages[i].Name;
            }
        }

        return LocalizationManager.CurrentLanguage;
    }

    private void ChangeLanguage(string languageCode, string languageName)
    {
        LocalizationManager.SetLanguage(languageCode);
        AddLog(F("log.language_changed", languageName));
        RebuildInterface();
    }

    private void ResetSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        data = new SaveData();
        log.Clear();
        offlineNotice = string.Empty;
        data.pendingOfflineMinutes = 0;
        data.pendingOfflineSold = 0;
        data.pendingOfflineEarned = 0f;
        NormalizeData();
        AddLog(T("log.save_reset"));
        Save();
        RebuildInterface();
    }

    private void RebuildInterface()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        tabButtons.Clear();
        BuildUI();
        RenderAll();
    }

    private void CreateSectionTitle(string title, string subtitle)
    {
        RectTransform header = CreateRect("SectionTitle", contentRoot);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 86f : 104f;
        VerticalLayoutGroup layout = header.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        Text titleText = CreateText("Title", header, title, wideLayout ? 34 : 40, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = wideLayout ? 24 : 28;
        titleText.resizeTextMaxSize = wideLayout ? 34 : 40;

        Text subtitleText = CreateText("Subtitle", header, subtitle, wideLayout ? 23 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        subtitleText.resizeTextForBestFit = true;
        subtitleText.resizeTextMinSize = wideLayout ? 16 : 19;
        subtitleText.resizeTextMaxSize = wideLayout ? 23 : 27;
    }

    private Text CreateResourceChip(RectTransform parent, string label, string value, Sprite icon, Color accent)
    {
        RectTransform chip = CreatePanel("ResourceChip", parent, new Color(0.10f, 0.07f, 0.05f, 0.84f));
        chip.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        HorizontalLayoutGroup layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(10, 10, 7, 7) : new RectOffset(12, 12, 9, 9);
        layout.spacing = wideLayout ? 8f : 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        RectTransform iconBox = CreatePanel("ResourceIcon", chip, accent);
        iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 42f : 50f;
        if (icon != null)
        {
            CreateImage("Icon", iconBox, icon, true);
        }

        Text text = CreateText("Value", chip, $"{label}\n{value}", wideLayout ? 20 : 24, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = wideLayout ? 12 : 15;
        text.resizeTextMaxSize = wideLayout ? 20 : 24;
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return text;
    }

    private Button CreateRoundShortcut(RectTransform parent, Sprite icon, Color color, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel("RoundShortcut", parent, color);
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 66f : 78f;
        Button button = rect.gameObject.AddComponent<Button>();
        rect.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, color);

        if (icon != null)
        {
            RectTransform iconBox = CreateRect("ShortcutIcon", rect);
            iconBox.anchorMin = new Vector2(0.18f, 0.18f);
            iconBox.anchorMax = new Vector2(0.82f, 0.82f);
            iconBox.offsetMin = Vector2.zero;
            iconBox.offsetMax = Vector2.zero;
            CreateImage("Icon", iconBox, icon, true);
        }

        return button;
    }

    private void CreateSheetTab(RectTransform parent, string label, bool active)
    {
        RectTransform tab = CreatePanel("SheetTab", parent, active ? panel : new Color(0.50f, 0.32f, 0.18f));
        tab.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        CreateText("Label", tab, label, wideLayout ? 26 : 31, FontStyle.Bold, active ? pine : Color.white, TextAnchor.MiddleCenter);
    }

    private void CreateRecommendationBanner(RectTransform parent, string title, string detail, Sprite icon, Color color)
    {
        bool shortPortrait = IsShortPortraitScreen();
        RectTransform banner = CreatePanel("RecommendationBanner", parent, new Color(1f, 0.96f, 0.86f, 0.98f));
        banner.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 92f : shortPortrait ? 108f : 118f;
        HorizontalLayoutGroup layout = banner.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(12, 14, 10, 10) : new RectOffset(14, 16, 11, 11);
        layout.spacing = wideLayout ? 10f : 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        RectTransform accent = CreatePanel("RecommendationIcon", banner, color);
        accent.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 66f : shortPortrait ? 76f : 84f;
        if (icon != null)
        {
            CreateImage("Icon", accent, icon, true);
        }

        RectTransform textGroup = CreateRect("RecommendationText", banner);
        textGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = wideLayout ? 3f : 4f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandHeight = false;

        Text titleText = CreateText("Title", textGroup, title, wideLayout ? 23 : shortPortrait ? 26 : 28, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = wideLayout ? 16 : 18;
        titleText.resizeTextMaxSize = wideLayout ? 23 : shortPortrait ? 26 : 28;

        Text detailText = CreateText("Detail", textGroup, detail, wideLayout ? 17 : shortPortrait ? 19 : 21, FontStyle.Bold, ink, TextAnchor.MiddleLeft);
        detailText.resizeTextForBestFit = true;
        detailText.resizeTextMinSize = wideLayout ? 12 : 14;
        detailText.resizeTextMaxSize = wideLayout ? 17 : shortPortrait ? 19 : 21;
    }

    private Button CreateFeaturedActionCard(RectTransform parent, string title, string detail, string cta, Sprite icon, Color color, UnityEngine.Events.UnityAction action)
    {
        bool shortPortrait = IsShortPortraitScreen();
        RectTransform card = CreatePanel("FeaturedActionCard", parent, panel);
        if (upgradePulseTimer > 0f)
        {
            card.gameObject.AddComponent<UIFlashTint>().Configure(honey, upgradePulseTimer);
        }
        else if (restockPulseTimer > 0f)
        {
            card.gameObject.AddComponent<UIFlashTint>().Configure(blue, restockPulseTimer);
        }

        card.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 160f : shortPortrait ? 204f : 224f;
        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(16, 16, 14, 14) : shortPortrait ? new RectOffset(18, 18, 16, 16) : new RectOffset(20, 20, 18, 18);
        layout.spacing = wideLayout ? 14f : 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        RectTransform iconBox = CreatePanel("HomeCardIcon", card, color);
        iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 110f : shortPortrait ? 138f : 150f;
        if (icon != null)
        {
            CreateImage("Icon", iconBox, icon, true);
        }

        RectTransform textGroup = CreateRect("FeaturedActionText", card);
        textGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = wideLayout ? 8f : 10f;
        textLayout.childControlHeight = true;
        textLayout.childControlWidth = true;
        textLayout.childForceExpandHeight = false;
        textLayout.childForceExpandWidth = true;

        Text titleText = CreateText("Title", textGroup, title, wideLayout ? 32 : shortPortrait ? 37 : 40, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = wideLayout ? 22 : 27;
        titleText.resizeTextMaxSize = wideLayout ? 32 : shortPortrait ? 37 : 40;
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.minHeight = wideLayout ? 46f : shortPortrait ? 58f : 64f;
        titleLayout.preferredHeight = wideLayout ? 46f : shortPortrait ? 58f : 64f;
        titleLayout.flexibleHeight = 0f;

        Text detailText = CreateText("Detail", textGroup, detail, wideLayout ? 23 : shortPortrait ? 27 : 29, FontStyle.Bold, ink, TextAnchor.MiddleLeft);
        detailText.resizeTextForBestFit = true;
        detailText.resizeTextMinSize = wideLayout ? 16 : 20;
        detailText.resizeTextMaxSize = wideLayout ? 23 : shortPortrait ? 27 : 29;
        LayoutElement detailLayout = detailText.gameObject.AddComponent<LayoutElement>();
        detailLayout.minHeight = wideLayout ? 64f : shortPortrait ? 82f : 94f;
        detailLayout.preferredHeight = wideLayout ? 64f : shortPortrait ? 82f : 94f;
        detailLayout.flexibleHeight = 0f;

        RectTransform buttonBox = CreatePanel("HomeCardButton", card, color);
        LayoutElement buttonBoxLayout = buttonBox.gameObject.AddComponent<LayoutElement>();
        buttonBoxLayout.minWidth = wideLayout ? 160f : shortPortrait ? 184f : 204f;
        buttonBoxLayout.preferredWidth = wideLayout ? 160f : shortPortrait ? 184f : 204f;
        buttonBoxLayout.flexibleWidth = 0f;
        Text buttonText = CreateText("ButtonLabel", buttonBox, cta, wideLayout ? 25 : shortPortrait ? 29 : 31, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        buttonText.resizeTextForBestFit = true;
        buttonText.resizeTextMinSize = wideLayout ? 17 : 21;
        buttonText.resizeTextMaxSize = wideLayout ? 25 : shortPortrait ? 29 : 31;

        Button button = card.gameObject.AddComponent<Button>();
        card.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = card.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, panel);
        return button;
    }

    private Button CreateQuickActionButton(RectTransform parent, string title, string detail, Sprite icon, Color color, UnityEngine.Events.UnityAction action, bool interactable = true)
    {
        bool shortPortrait = IsShortPortraitScreen();
        Color buttonColor = interactable ? color : new Color(0.34f, 0.34f, 0.31f, 0.96f);
        Color detailColor = interactable ? new Color(1f, 0.92f, 0.74f) : new Color(0.88f, 0.86f, 0.78f);
        RectTransform card = CreatePanel("QuickActionButton", parent, buttonColor);
        LayoutElement cardLayout = card.gameObject.AddComponent<LayoutElement>();
        cardLayout.flexibleWidth = 1f;
        cardLayout.flexibleHeight = 0f;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(8, 8, 7, 7) : shortPortrait ? new RectOffset(9, 9, 8, 8) : new RectOffset(10, 10, 8, 8);
        layout.spacing = wideLayout ? 2f : 3f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform iconBox = CreatePanel("QuickActionIcon", card, interactable ? new Color(1f, 0.95f, 0.80f, 0.22f) : new Color(1f, 1f, 1f, 0.10f));
        LayoutElement iconLayout = iconBox.gameObject.AddComponent<LayoutElement>();
        iconLayout.minHeight = wideLayout ? 30f : shortPortrait ? 34f : 38f;
        iconLayout.preferredHeight = wideLayout ? 30f : shortPortrait ? 34f : 38f;
        iconLayout.flexibleHeight = 0f;
        if (icon != null)
        {
            Image iconImage = CreateImage("Icon", iconBox, icon, true);
            if (!interactable)
            {
                iconImage.color = new Color(1f, 1f, 1f, 0.55f);
            }
        }

        Text titleText = CreateText("Title", card, title, wideLayout ? 18 : shortPortrait ? 20 : 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = wideLayout ? 12 : 14;
        titleText.resizeTextMaxSize = wideLayout ? 18 : shortPortrait ? 20 : 22;
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.minHeight = wideLayout ? 22f : shortPortrait ? 25f : 27f;
        titleLayout.preferredHeight = wideLayout ? 22f : shortPortrait ? 25f : 27f;
        titleLayout.flexibleHeight = 0f;

        Text detailText = CreateText("Detail", card, detail, wideLayout ? 14 : shortPortrait ? 16 : 17, FontStyle.Bold, detailColor, TextAnchor.MiddleCenter);
        detailText.resizeTextForBestFit = true;
        detailText.resizeTextMinSize = wideLayout ? 10 : 12;
        detailText.resizeTextMaxSize = wideLayout ? 14 : shortPortrait ? 16 : 17;
        LayoutElement detailLayout = detailText.gameObject.AddComponent<LayoutElement>();
        detailLayout.minHeight = wideLayout ? 20f : shortPortrait ? 22f : 24f;
        detailLayout.preferredHeight = wideLayout ? 20f : shortPortrait ? 22f : 24f;
        detailLayout.flexibleHeight = 0f;

        Button button = card.gameObject.AddComponent<Button>();
        card.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = card.GetComponent<Image>();
        if (interactable)
        {
            button.onClick.AddListener(action);
        }

        button.interactable = interactable;
        ApplyButtonColor(button, buttonColor);
        return button;
    }

    private void SetButtonAvailability(Button button, bool interactable, Color activeColor)
    {
        Color muted = new Color(0.34f, 0.34f, 0.31f, 0.96f);
        button.interactable = interactable;
        ApplyButtonColor(button, interactable ? activeColor : muted);

        Text[] labels = button.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].color = interactable ? Color.white : new Color(0.88f, 0.86f, 0.78f);
        }
    }

    private Button CreateHomeActionCard(RectTransform parent, string title, string detail, Sprite icon, Color color, UnityEngine.Events.UnityAction action)
    {
        RectTransform card = CreatePanel("HomeActionCard", parent, panel);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 104f : 124f;
        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(14, 14, 12, 12) : new RectOffset(18, 18, 14, 14);
        layout.spacing = wideLayout ? 14f : 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        RectTransform iconBox = CreatePanel("HomeCardIcon", card, color);
        iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 78f : 92f;
        if (icon != null)
        {
            CreateImage("Icon", iconBox, icon, true);
        }

        RectTransform textGroup = CreateRect("HomeCardText", card);
        textGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = wideLayout ? 5f : 7f;
        CreateText("Title", textGroup, title, wideLayout ? 30 : 36, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateText("Detail", textGroup, detail, wideLayout ? 22 : 27, FontStyle.Bold, ink, TextAnchor.MiddleLeft);

        RectTransform buttonBox = CreatePanel("HomeCardButton", card, color);
        buttonBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 126f : 150f;
        CreateText("ButtonLabel", buttonBox, title, wideLayout ? 22 : 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        Button button = card.gameObject.AddComponent<Button>();
        card.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = card.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, panel);
        return button;
    }

    private Text CreateStat(RectTransform parent, string label, Sprite icon)
    {
        RectTransform box = CreatePanel(label, parent, panel);
        box.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        HorizontalLayoutGroup layout = box.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(8, 8, 6, 6) : new RectOffset(12, 12, 9, 9);
        layout.spacing = wideLayout ? 6f : 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        if (icon != null)
        {
            RectTransform iconBox = CreateImagePanel("Icon", box, icon, Color.clear);
            iconBox.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 28f : 46f;
        }

        Text text = CreateText("Text", box, label, wideLayout ? 16 : 30, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = wideLayout ? 10 : 20;
        text.resizeTextMaxSize = wideLayout ? 17 : 30;
        if (box.GetComponent<Shadow>() == null)
        {
            Shadow shadow = box.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.15f, 0.10f, 0.04f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        if (box.GetComponent<UIAppearAnimator>() == null)
        {
            box.gameObject.AddComponent<UIAppearAnimator>();
        }

        return text;
    }

    private ScrollRect CreateScroll(RectTransform parent)
    {
        RectTransform viewport = CreatePanel("ScrollView", parent, new Color(0f, 0f, 0f, 0f));
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.08f;
        scroll.scrollSensitivity = wideLayout ? 32f : 42f;

        RectTransform mask = CreateRect("Viewport", viewport);
        mask.anchorMin = Vector2.zero;
        mask.anchorMax = Vector2.one;
        mask.offsetMin = Vector2.zero;
        mask.offsetMax = Vector2.zero;
        Image maskImage = mask.gameObject.AddComponent<Image>();
        maskImage.color = new Color(1f, 1f, 1f, 0.01f);
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        RectTransform content = CreateRect("Content", mask);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, wideLayout ? 4 : 6);
        layout.spacing = wideLayout ? 9f : 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = mask;
        scroll.content = content;
        return scroll;
    }

    private Button CreateButton(RectTransform parent, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel("Button", parent, color);
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 62f : 70f;
        Button button = rect.gameObject.AddComponent<Button>();
        rect.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, color);

        Text text = CreateText("Label", rect, label, wideLayout ? 24 : 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = wideLayout ? 15 : 18;
        text.resizeTextMaxSize = wideLayout ? 24 : 28;
        text.raycastTarget = false;
        return button;
    }

    private Button CreatePrimaryActionButton(RectTransform parent, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel("Button", parent, color);
        rect.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 92f : 110f;
        Button button = rect.gameObject.AddComponent<Button>();
        rect.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, color);

        Text text = CreateText("Label", rect, label, wideLayout ? 30 : 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = wideLayout ? 18 : 22;
        text.resizeTextMaxSize = wideLayout ? 30 : 36;
        text.raycastTarget = false;
        return button;
    }

    private void AddTabButton(RectTransform parent, Tab tab, string label, bool vertical)
    {
        Button button = CreateNavButton(parent, tab, label, vertical, () =>
        {
            activeTab = tab;
            RenderAll();
        });
        tabButtons[tab] = button;
    }

    private Button CreateNavButton(RectTransform parent, Tab tab, string label, bool vertical, UnityEngine.Events.UnityAction action)
    {
        bool active = activeTab == tab;
        RectTransform rect = CreatePanel("NavButton", parent, active ? NavActiveColor(tab) : inactiveNav);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = vertical ? 64f : 116f;
        layout.flexibleWidth = 1f;

        Button button = rect.gameObject.AddComponent<Button>();
        rect.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, active ? NavActiveColor(tab) : inactiveNav);

        VerticalLayoutGroup buttonLayout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonLayout.padding = vertical ? new RectOffset(7, 7, 5, 5) : new RectOffset(6, 6, 6, 6);
        buttonLayout.spacing = vertical ? 2f : 0f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        RectTransform iconFrame = CreatePanel("NavIconFrame", rect, active ? new Color(1f, 0.93f, 0.62f) : new Color(0.20f, 0.16f, 0.12f));
        LayoutElement iconLayout = iconFrame.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = vertical ? 38f : 84f;
        iconLayout.preferredHeight = vertical ? 30f : 78f;
        Sprite icon = TabIcon(tab);
        if (icon != null)
        {
            Image iconImage = CreateImage("NavIcon", iconFrame, icon, true);
            iconImage.color = active ? Color.white : new Color(1f, 0.92f, 0.72f);
        }

        Text text = CreateText("NavLabel", rect, label, vertical ? 21 : 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = vertical ? 13 : 12;
        text.resizeTextMaxSize = vertical ? 21 : 22;
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = vertical ? 24f : 24f;
        text.raycastTarget = false;

        return button;
    }

    private Sprite TabIcon(Tab tab)
    {
        switch (tab)
        {
            case Tab.Shop:
                return coinSprite;
            case Tab.Stock:
                return shelfSprite != null ? shelfSprite : upgradeSprite;
            case Tab.Upgrades:
                return upgradeSprite;
            case Tab.Staff:
                return customerSprite;
            case Tab.Settings:
                return reputationSprite;
            default:
                return null;
        }
    }

    private Color NavActiveColor(Tab tab)
    {
        switch (tab)
        {
            case Tab.Shop:
                return pine;
            case Tab.Stock:
                return blue;
            case Tab.Upgrades:
                return honey;
            case Tab.Staff:
                return pine;
            case Tab.Settings:
                return new Color(0.34f, 0.42f, 0.30f);
            default:
                return pine;
        }
    }

    private Color NavInactiveIconColor()
    {
        return new Color(1f, 0.90f, 0.68f);
    }

    private Color NavActiveIconFrameColor(Tab tab)
    {
        return Color.Lerp(NavActiveColor(tab), Color.white, 0.72f);
    }

    private void UpdateNavButton(Button button, Tab tab, bool active)
    {
        ApplyButtonColor(button, active ? NavActiveColor(tab) : inactiveNav);

        Transform iconFrame = button.transform.Find("NavIconFrame");
        if (iconFrame != null)
        {
            Image frameImage = iconFrame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.color = active ? NavActiveIconFrameColor(tab) : new Color(0.20f, 0.16f, 0.12f);
            }

            Transform icon = iconFrame.Find("NavIcon");
            if (icon != null)
            {
                Image iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.color = active ? Color.white : NavInactiveIconColor();
                }
            }
        }

        Transform label = button.transform.Find("NavLabel");
        if (label != null)
        {
            Text text = label.GetComponent<Text>();
            if (text != null)
            {
                text.color = active ? Color.white : new Color(1f, 0.88f, 0.66f);
            }
        }
    }

    private void UpdateTabButtons()
    {
        foreach (KeyValuePair<Tab, Button> entry in tabButtons)
        {
            bool active = entry.Key == activeTab;
            UpdateNavButton(entry.Value, entry.Key, active);
        }
    }

    private void ApplyButtonColor(Button button, Color color)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }

        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color;
        colors.selectedColor = color;
        colors.disabledColor = color;
        button.colors = colors;

        UIButtonFeedback feedback = button.GetComponent<UIButtonFeedback>();
        if (feedback != null)
        {
            feedback.SyncRestColor();
        }
    }

    private RectTransform CreateProgress(RectTransform parent, float value, Color color)
    {
        return CreateProgress(parent, value, color, 16f);
    }

    private RectTransform CreateProgress(RectTransform parent, float value, Color color, float height)
    {
        RectTransform track = CreatePanel("Progress", parent, new Color(0.12f, 0.20f, 0.17f, 0.14f));
        track.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        RectTransform fill = CreatePanel("Fill", track, color);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        return fill;
    }

    private Text CreateText(string name, RectTransform parent, string value, int size, FontStyle style, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = CurrentFont();
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = LocalizedAnchor(anchor);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateImagePanel(string name, RectTransform parent, Sprite sprite, Color fallbackColor)
    {
        return CreateImagePanel(name, parent, sprite, fallbackColor, true);
    }

    private RectTransform CreateImagePanel(string name, RectTransform parent, Sprite sprite, Color fallbackColor, bool preserveAspect)
    {
        RectTransform rect = CreatePanel(name, parent, fallbackColor);
        if (sprite != null)
        {
            Image image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
        }

        return rect;
    }

    private Image CreateImage(string name, RectTransform parent, Sprite sprite, bool preserveAspect)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = RoundedPanelSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        if (ShouldStyleSurface(name, color))
        {
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.15f, 0.10f, 0.04f, 0.16f);
            shadow.effectDistance = new Vector2(0f, -5f);
            rect.gameObject.AddComponent<UIAppearAnimator>();
        }

        return rect;
    }

    private bool ShouldStyleSurface(string name, Color color)
    {
        if (color.a <= 0.05f)
        {
            return false;
        }

        return name == "Top" ||
               name == "Tabs" ||
               name == "GameStage" ||
               name == "RoundShortcut" ||
               name == "ManagementPanel" ||
               name == "SheetTabs" ||
               name == "SheetTab" ||
               name == "HomeActionCard" ||
               name == "TestPanel" ||
               name == "RecommendationBanner" ||
               name == "RecommendationIcon" ||
               name == "FeaturedActionCard" ||
               name == "QuickActionButton" ||
               name == "QuickActionIcon" ||
               name == "OrderItemButton" ||
               name == "OrderItemIcon" ||
               name == "HomeCardIcon" ||
               name == "HomeCardButton" ||
               name == "ShopHero" ||
               name == "ShopFront" ||
               name == "GuidePanel" ||
               name == "GuideItem" ||
               name == "Product" ||
               name == "Upgrade" ||
               name == "Staff" ||
               name == "LanguagePanel" ||
               name == "ResetPanel" ||
               name == "Button" ||
               name == "NavButton" ||
               name == "NavIconFrame" ||
               name.StartsWith("Shelf", StringComparison.Ordinal) ||
               name == "Swatch" ||
               name == "Avatar";
    }

    private Sprite RoundedPanelSprite()
    {
        if (roundedPanelSprite != null)
        {
            return roundedPanelSprite;
        }

        const int size = 48;
        const int radius = 10;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0;
                int dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0;
                bool outside = dx * dx + dy * dy > radius * radius;
                texture.SetPixel(x, y, outside ? clear : white);
            }
        }

        texture.Apply();
        roundedPanelSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedPanelSprite;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void AnchorTop(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -top - height);
        rect.offsetMax = new Vector2(0f, -top);
    }

    private static void AnchorBottom(RectTransform rect, float bottom, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(0f, bottom);
        rect.offsetMax = new Vector2(0f, bottom + height);
    }

    private static void AnchorLeft(RectTransform rect, float left, float width)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = new Vector2(left + width, 0f);
    }

    private static void AnchorFill(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Clear(Transform target)
    {
        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Destroy(target.GetChild(i).gameObject);
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }
}
