using System;
using System.Collections.Generic;
using System.Globalization;
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
        public long lastSavedUnix;
        public float[] stock = Array.Empty<float>();
        public int[] sold = Array.Empty<int>();
        public int[] currentOrder = Array.Empty<int>();
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
    private const int StaffRecommendationMinSold = 30;
    private const int MilestoneCount = 5;
    private readonly ProductDef[] products =
    {
        new ProductDef("product.rice_ball.name", "product.rice_ball.subtitle", 6f, 11f, 1.35f, 1, 18, 2, new Color(0.94f, 0.48f, 0.32f)),
        new ProductDef("product.sparkling_water.name", "product.sparkling_water.subtitle", 9f, 18f, 1.18f, 1, 16, 2, new Color(0.18f, 0.58f, 0.72f)),
        new ProductDef("product.bread.name", "product.bread.subtitle", 11f, 22f, 1.05f, 1, 14, 2, new Color(0.85f, 0.56f, 0.27f)),
        new ProductDef("product.coffee.name", "product.coffee.subtitle", 14f, 30f, 0.95f, 2, 12, 2, new Color(0.54f, 0.34f, 0.22f)),
        new ProductDef("product.lunch_box.name", "product.lunch_box.subtitle", 24f, 52f, 0.78f, 3, 10, 2, new Color(0.89f, 0.64f, 0.22f)),
        new ProductDef("product.dessert.name", "product.dessert.subtitle", 30f, 68f, 0.64f, 4, 9, 2, new Color(0.84f, 0.48f, 0.64f)),
        new ProductDef("product.flower.name", "product.flower.subtitle", 48f, 118f, 0.48f, 6, 8, 1, new Color(0.78f, 0.38f, 0.62f)),
        new ProductDef("product.gift_box.name", "product.gift_box.subtitle", 72f, 180f, 0.34f, 8, 6, 1, new Color(0.62f, 0.46f, 0.86f))
    };

    private readonly UpgradeDef[] upgrades =
    {
        new UpgradeDef("upgrade.shelf.name", "upgrade.shelf.detail", 260f, 8),
        new UpgradeDef("upgrade.signboard.name", "upgrade.signboard.detail", 300f, 10),
        new UpgradeDef("upgrade.fridge.name", "upgrade.fridge.detail", 220f, 7),
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
    private RectTransform sceneProductMotion = null!;
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
    private float salePopTimer;
    private float restockPulseTimer;
    private float upgradePulseTimer;
    private float displayedAutoSaleProgress;

    public void ConfigureArt(Sprite shopFront, Sprite shelf, Sprite counter, Sprite coin, Sprite reputation, Sprite customer, Sprite upgrade, Sprite[] productsArt, Sprite[] staffArt, Font defaultUiFont, Font rtlFont)
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
        mainFont = defaultUiFont;
        arabicFont = rtlFont;
    }

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        try
        {
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

        if (restockPulseTimer > 0f)
        {
            restockPulseTimer = Mathf.Max(0f, restockPulseTimer - Time.deltaTime);
        }

        if (upgradePulseTimer > 0f)
        {
            upgradePulseTimer = Mathf.Max(0f, upgradePulseTimer - Time.deltaTime);
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
        data.unlocked = Resize(data.unlocked, products.Length);
        data.upgrades = Resize(data.upgrades, upgrades.Length);
        data.staff = Resize(data.staff, staffDefs.Length);
        data.milestonesClaimed = Resize(data.milestonesClaimed, MilestoneCount);

        data.level = Mathf.Max(1, data.level);
        data.cash = Mathf.Max(0f, data.cash);
        data.day = Mathf.Max(1, data.day);
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
                    data.stock[i] = Mathf.Min(8f, MaxStock(i));
                }
            }
        }

        EnsureCurrentOrder();
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
        float elapsed = Mathf.Clamp((float)(now - data.lastSavedUnix), 0f, OfflineCapSeconds);
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
                return data.completedOrders >= 3;
            case 1:
                return data.restockActions >= 1;
            case 2:
                return data.upgrades.Length > 2 && data.upgrades[2] >= 1;
            case 3:
                return data.staff.Length > 0 && data.staff[0] >= 1;
            case 4:
                return data.level >= 3 || (data.unlocked.Length > 1 && data.unlocked[1]);
            default:
                return false;
        }
    }

    private float MilestoneProgress(int index)
    {
        switch (index)
        {
            case 0:
                return Mathf.Clamp01(data.completedOrders / 3f);
            case 1:
                return data.restockActions >= 1 ? 1f : 0f;
            case 2:
                return data.upgrades.Length > 2 && data.upgrades[2] >= 1 ? 1f : 0f;
            case 3:
                return data.staff.Length > 0 && data.staff[0] >= 1 ? 1f : 0f;
            case 4:
                return Mathf.Clamp01(data.level / 3f);
            default:
                return 1f;
        }
    }

    private int MilestoneReward(int index)
    {
        switch (index)
        {
            case 0:
                return 40;
            case 1:
                return 60;
            case 2:
                return 90;
            case 3:
                return 120;
            case 4:
                return 150;
            default:
                return 0;
        }
    }

    private string MilestoneName(int index)
    {
        switch (index)
        {
            case 0:
                return T("milestone.order_3.name");
            case 1:
                return T("milestone.restock_once.name");
            case 2:
                return T("milestone.price_upgrade.name");
            case 3:
                return T("milestone.cashier.name");
            case 4:
                return T("milestone.level_3.name");
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
                return F("milestone.order_3", data.completedOrders, reward);
            case 1:
                return F("milestone.restock_once", reward);
            case 2:
                return F("milestone.price_upgrade", reward);
            case 3:
                return F("milestone.cashier", reward);
            case 4:
                return F("milestone.level_3", data.level, reward);
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
                return T("milestone.hint.upgrade");
            case 3:
                return T("milestone.hint.cashier");
            case 4:
                return T("milestone.hint.level_3");
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

        EnsureCurrentOrder();
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
        int earned = CurrentOrderItemValue(productIndex);
        data.currentOrder[productIndex] = Mathf.Max(0, data.currentOrder[productIndex] - 1);
        data.stock[productIndex] -= 1f;
        data.sold[productIndex] += 1;

        data.cash += earned;
        data.totalEarned += earned;
        data.soldToday += 1;
        data.xp += 10f + Mathf.Floor(earned / 14f);
        data.reputation += 0.026f * (1f + StaffLevel(2) * 0.12f + StaffLevel(5) * 0.05f);
        salePopMessage = $"+{Money(earned)}";
        salePopTimer = manual ? 1.15f : 0.8f;

        bool orderDone = CurrentOrderItemCount() <= 0;
        if (manual || UnityEngine.Random.value < 0.14f)
        {
            AddLog(F("log.sold", ProductName(productIndex), Money(earned)));
        }

        if (orderDone)
        {
            data.completedOrders += 1;
            data.xp += 12f + Mathf.Floor(earned / 16f);
            AddLog(F("log.order_completed", orderText, Money(earned)));
            GenerateCurrentOrder();
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

    private void EnsureCurrentOrder()
    {
        if (data.currentOrder == null || data.currentOrder.Length != products.Length)
        {
            data.currentOrder = Resize(data.currentOrder, products.Length);
        }

        if (CurrentOrderItemCount() <= 0 || !CurrentOrderUsesUnlockedProducts())
        {
            GenerateCurrentOrder();
        }
    }

    private void GenerateCurrentOrder()
    {
        for (int i = 0; i < data.currentOrder.Length; i++)
        {
            data.currentOrder[i] = 0;
        }

        int unlockedCount = UnlockedProductCount();
        if (unlockedCount <= 0)
        {
            data.currentOrder[0] = 1;
            return;
        }

        int maxKinds = Mathf.Min(unlockedCount, MaxOrderKinds());
        int targetKinds = Mathf.Clamp(1 + (UnityEngine.Random.value < ComboOrderChance() ? 1 : 0) + (UnityEngine.Random.value < ComboOrderChance() * 0.45f ? 1 : 0), 1, maxKinds);
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
            int quantity = UnityEngine.Random.Range(1, maxQty + 1);
            data.currentOrder[index] = Mathf.Max(data.currentOrder[index], quantity);
        }

        if (CurrentOrderItemCount() <= 0)
        {
            int fallback = FirstUnlockedProductIndex();
            data.currentOrder[fallback] = 1;
        }
    }

    private int ChooseOrderProductIndex()
    {
        float total = 0f;
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i] && data.currentOrder[i] <= 0)
            {
                total += products[i].Demand;
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

            roll -= products[i].Demand;
            if (roll <= 0f)
            {
                return i;
            }
        }

        return 0;
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
            if (CurrentOrderQuantity(i) > 0)
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

    private int CurrentOrderItemCount()
    {
        int count = 0;
        for (int i = 0; i < products.Length; i++)
        {
            count += CurrentOrderQuantity(i);
        }

        return count;
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
            int quantity = CurrentOrderQuantity(i);
            if (quantity > 0)
            {
                parts.Add(F("order.item", ProductName(i), quantity));
            }
        }

        return parts.Count > 0 ? string.Join(" + ", parts) : T("order.waiting");
    }

    private string CurrentOrderShortageText()
    {
        EnsureCurrentOrder();
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

    private int CurrentOrderServeIndex()
    {
        EnsureCurrentOrder();
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
        float staffSpeed = StaffLevel(0) * 0.08f + StaffLevel(5) * 0.035f;
        float upgradeSpeed = data.upgrades[1] * 0.055f;
        float duration = 9.5f / (1f + staffSpeed + upgradeSpeed);
        return Mathf.Clamp(duration, 3.2f, 12f);
    }

    private float OrderValueMultiplier()
    {
        return 1f + StaffLevel(3) * 0.018f + StaffLevel(5) * 0.025f;
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
        float staffSpeed = cashierLevel * 0.08f + managerLevel * 0.035f;
        float upgradeSpeed = data.upgrades[1] * 0.055f;
        float duration = Mathf.Clamp(9.5f / (1f + staffSpeed + upgradeSpeed), 3.2f, 12f);
        return 1f / duration;
    }

    private float AutoSaleInterval()
    {
        return 1f / Mathf.Max(0.01f, TrafficPerSecond());
    }

    private bool HasSellableStock()
    {
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
        int bestCost = int.MaxValue;
        for (int i = 0; i < staffDefs.Length; i++)
        {
            if (data.staff[i] >= staffDefs[i].MaxLevel)
            {
                continue;
            }

            int cost = StaffCost(i);
            if (data.cash >= cost)
            {
                return i;
            }

            if (cost < bestCost)
            {
                bestCost = cost;
                bestIndex = i;
            }
        }

        return bestIndex;
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
        AddLog(F("log.restocked", ProductName(productIndex), amount));
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
        data.restockActions += 1;
        AddLog(F("log.restocked", ProductName(index), bought));
        CheckMilestones();
        RenderAll();
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
                float nextDuration = Mathf.Clamp(9.5f / (1f + StaffLevel(0) * 0.08f + StaffLevel(5) * 0.035f + (data.upgrades[1] + 1) * 0.055f), 3.2f, 12f);
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
        return F("stock.profit_line", Money(Mathf.Max(0, price - cost)), Money(price), Money(cost));
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
                return F("staff.promoter.automation", "16%");
            case 3:
                return F("staff.merchandiser.automation", Mathf.RoundToInt(ComboOrderChance() * 100f), Mathf.RoundToInt(Mathf.Clamp01(ComboOrderChance() + 0.018f) * 100f));
            case 4:
                return F("staff.purchaser.automation", Money(UnitCost(PrimaryProductIndex())), Money(Mathf.Max(1, Mathf.FloorToInt(products[PrimaryProductIndex()].Cost * Mathf.Pow(0.94f, data.upgrades[3]) * (1f - (StaffLevel(4) + 1) * 0.025f)))));
            case 5:
                return F("staff.manager.automation", Mathf.RoundToInt(OrderValueMultiplier() * 100f), Mathf.RoundToInt((OrderValueMultiplier() + 0.025f) * 100f));
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
        float headerHeight = shortPortrait ? 166f : 184f;
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
        metricRow.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 52f : 70f;
        HorizontalLayoutGroup metricLayout = metricRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metricLayout.spacing = compact ? 8f : 10f;
        metricLayout.childAlignment = TextAnchor.MiddleCenter;
        metricLayout.childControlWidth = true;
        metricLayout.childControlHeight = true;
        metricLayout.childForceExpandWidth = true;
        metricLayout.childForceExpandHeight = true;

        cashText = CreateHeaderChip(metricRow, "HeaderCash", coinSprite, honey, compact ? 210f : 300f, compact ? 25 : 34);
        newItemsText = CreateHeaderChip(metricRow, "HeaderStock", shelfSprite != null ? shelfSprite : counterSprite, blue, compact ? 190f : 260f, compact ? 22 : 29);
        totalText = CreateHeaderChip(metricRow, "HeaderIncome", customerSprite, coral, compact ? 210f : 300f, compact ? 22 : 29);

        RectTransform timerRow = CreatePanel("HeaderTimer", top, new Color(0.08f, 0.18f, 0.15f, 0.96f));
        timerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = compact ? 54f : 68f;
        HorizontalLayoutGroup timerLayout = timerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        timerLayout.padding = compact ? new RectOffset(12, 12, 9, 9) : new RectOffset(14, 14, 12, 12);
        timerLayout.spacing = compact ? 12f : 16f;
        timerLayout.childAlignment = TextAnchor.MiddleCenter;
        timerLayout.childControlWidth = true;
        timerLayout.childControlHeight = true;
        timerLayout.childForceExpandWidth = false;

        queueText = CreateText("AutoTimer", timerRow, "", compact ? 28 : 38, FontStyle.Bold, coral, TextAnchor.MiddleLeft);
        LayoutElement timerTextLayout = queueText.gameObject.AddComponent<LayoutElement>();
        timerTextLayout.preferredWidth = compact ? 330f : 470f;
        timerTextLayout.flexibleWidth = 0f;
        queueText.resizeTextForBestFit = true;
        queueText.resizeTextMinSize = compact ? 17 : 20;
        queueText.resizeTextMaxSize = compact ? 28 : 38;
        headerAutoFill = CreateProgress(timerRow, 0f, coral, compact ? 26f : 34f);
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
        CreateText("Cap", card, F("offline.cap", Mathf.FloorToInt(OfflineCapSeconds / 3600f)), wideLayout ? 22 : 26, FontStyle.Normal, new Color(0.34f, 0.28f, 0.18f), TextAnchor.MiddleCenter);
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
        if (sceneProductMotion != null)
        {
            float idlePulse = 1f + Mathf.Sin(time * 3.2f) * 0.012f;
            float salePulse = 1f + saleBurst * 0.10f + restockBurst * 0.14f + upgradeBurst * 0.05f;
            sceneProductMotion.localScale = Vector3.one * idlePulse * salePulse;
        }

        if (sceneCashierGlow != null)
        {
            float alpha = 0.18f + Mathf.Sin(time * 5.0f) * 0.06f + saleBurst * 0.28f + upgradeBurst * 0.14f;
            Color color = sceneCashierGlow.color;
            color.a = Mathf.Clamp01(alpha);
            sceneCashierGlow.color = color;
        }

        if (sceneCustomerGlow != null)
        {
            float alpha = 0.14f + Mathf.Sin(time * 4.1f + 1.3f) * 0.05f + saleBurst * 0.22f + restockBurst * 0.16f;
            Color color = sceneCustomerGlow.color;
            color.a = Mathf.Clamp01(alpha);
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
    }

    private static void SetTextIfChanged(Text text, string value)
    {
        if (text != null && text.text != value)
        {
            text.text = value;
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
        sceneProductMotion = null!;
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
        int orderMissingCost = CurrentOrderMissingCost();
        bool orderHasMissing = orderMissingCost > 0;
        bool lowStock = hasSellableStock && LowestUnlockedStockRatio() <= LowStockRatio;

        RectTransform stage = CreatePanel("GameStage", contentRoot, new Color(0.16f, 0.36f, 0.32f));
        stage.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 520f : shortPortrait ? 620f : 680f;

        RectTransform stageArt = CreateImagePanel("ShopFront", stage, shopFrontSprite, new Color(0.18f, 0.32f, 0.30f), false);
        AnchorFill(stageArt, 0f, 0f, 0f, 0f);

        RectTransform shade = CreatePanel("StageShade", stage, new Color(0.02f, 0.08f, 0.07f, 0.36f));
        AnchorFill(shade, 0f, 0f, 0f, 0f);

        if (shelfSprite != null || counterSprite != null)
        {
            RectTransform overlay = CreateRect("ShopOverlay", stage);
            overlay.anchorMin = new Vector2(0.05f, 0.36f);
            overlay.anchorMax = new Vector2(0.95f, 0.84f);
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            HorizontalLayoutGroup overlayLayout = overlay.gameObject.AddComponent<HorizontalLayoutGroup>();
            overlayLayout.spacing = 16f;
            overlayLayout.childControlWidth = true;
            overlayLayout.childControlHeight = true;
            overlayLayout.childForceExpandWidth = true;
            overlayLayout.childForceExpandHeight = true;
            CreateImagePanel("ShelfArt", overlay, shelfSprite, new Color(0.62f, 0.42f, 0.24f, 0.32f));
            CreateImagePanel("CounterArt", overlay, counterSprite, new Color(0.22f, 0.38f, 0.34f, 0.32f));
        }

        RectTransform customerPoint = CreatePanel("CustomerPulse", stage, new Color(0.95f, 0.78f, 0.32f, 0.16f));
        customerPoint.anchorMin = new Vector2(0.08f, 0.20f);
        customerPoint.anchorMax = new Vector2(0.20f, 0.36f);
        customerPoint.offsetMin = Vector2.zero;
        customerPoint.offsetMax = Vector2.zero;
        sceneCustomerGlow = customerPoint.GetComponent<Image>();

        RectTransform cashierPoint = CreatePanel("CashierPulse", stage, new Color(0.94f, 0.65f, 0.20f, 0.18f));
        cashierPoint.anchorMin = new Vector2(0.74f, 0.24f);
        cashierPoint.anchorMax = new Vector2(0.90f, 0.44f);
        cashierPoint.offsetMin = Vector2.zero;
        cashierPoint.offsetMax = Vector2.zero;
        sceneCashierGlow = cashierPoint.GetComponent<Image>();

        RectTransform productIcon = CreatePanel("SceneProduct", stage, products[primaryIndex].Accent);
        productIcon.anchorMin = new Vector2(0.10f, 0.07f);
        productIcon.anchorMax = new Vector2(0.28f, 0.31f);
        productIcon.offsetMin = Vector2.zero;
        productIcon.offsetMax = Vector2.zero;
        sceneProductMotion = productIcon;
        Image productImage = productIcon.GetComponent<Image>();
        if (productImage != null && !hasSellableStock)
        {
            productImage.color = new Color(products[primaryIndex].Accent.r * 0.55f, products[primaryIndex].Accent.g * 0.55f, products[primaryIndex].Accent.b * 0.55f, 0.72f);
        }
        else if (productImage != null && lowStock)
        {
            productImage.color = Color.Lerp(products[primaryIndex].Accent, honey, 0.42f);
        }
        if (ProductSprite(primaryIndex) != null)
        {
            Image productArt = CreateImage("ProductArt", productIcon, ProductSprite(primaryIndex), true);
            if (!hasSellableStock)
            {
                productArt.color = new Color(0.82f, 0.82f, 0.82f, 0.70f);
            }
            else if (lowStock)
            {
                productArt.color = new Color(1f, 0.90f, 0.72f, 0.96f);
            }
        }

        if (!hasSellableStock || lowStock)
        {
            RectTransform stockHint = CreatePanel("StockStateHint", stage, lowStock ? new Color(0.42f, 0.24f, 0.06f, 0.84f) : new Color(0.03f, 0.10f, 0.09f, 0.82f));
            stockHint.anchorMin = new Vector2(0.08f, 0.72f);
            stockHint.anchorMax = new Vector2(0.92f, 0.92f);
            stockHint.offsetMin = Vector2.zero;
            stockHint.offsetMax = Vector2.zero;
            Text hintText = CreateText("Hint", stockHint, lowStock ? T("shop.low_stock_stage_hint") : CurrentOrderShortageText(), wideLayout ? 30 : shortPortrait ? 35 : 39, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            hintText.resizeTextForBestFit = true;
            hintText.resizeTextMinSize = wideLayout ? 18 : 22;
            hintText.resizeTextMaxSize = wideLayout ? 30 : shortPortrait ? 35 : 39;
        }

        displayedAutoSaleProgress = hasSellableStock ? Mathf.Clamp01(data.queue) : 0f;
        salePopText = CreateText("SalePop", stage, salePopMessage, wideLayout ? 42 : shortPortrait ? 48 : 54, FontStyle.Bold, honey, TextAnchor.MiddleRight);
        LayoutElement salePopLayout = salePopText.gameObject.AddComponent<LayoutElement>();
        salePopLayout.ignoreLayout = true;
        salePopRectTransform = salePopText.GetComponent<RectTransform>();
        salePopRectTransform.anchorMin = new Vector2(0.50f, 0.50f);
        salePopRectTransform.anchorMax = new Vector2(0.96f, 0.78f);
        salePopRectTransform.offsetMin = Vector2.zero;
        salePopRectTransform.offsetMax = Vector2.zero;
        salePopText.gameObject.SetActive(salePopTimer > 0f && !string.IsNullOrEmpty(salePopMessage));

        RectTransform orderCard = CreatePanel("CurrentOrder", stage, new Color(1f, 0.97f, 0.88f, 0.94f));
        orderCard.anchorMin = new Vector2(0.05f, 0.04f);
        orderCard.anchorMax = new Vector2(0.95f, 0.27f);
        orderCard.offsetMin = Vector2.zero;
        orderCard.offsetMax = Vector2.zero;
        VerticalLayoutGroup orderLayout = orderCard.gameObject.AddComponent<VerticalLayoutGroup>();
        orderLayout.padding = wideLayout ? new RectOffset(18, 18, 10, 10) : new RectOffset(22, 22, 12, 12);
        orderLayout.spacing = wideLayout ? 5f : 7f;
        orderLayout.childControlWidth = true;
        orderLayout.childControlHeight = true;
        orderLayout.childForceExpandWidth = true;
        orderLayout.childForceExpandHeight = false;
        Text orderTitle = CreateText("OrderTitle", orderCard, F("order.current", CurrentOrderSummary(), Money(CurrentOrderValue())), wideLayout ? 27 : shortPortrait ? 31 : 35, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        orderTitle.resizeTextForBestFit = true;
        orderTitle.resizeTextMinSize = wideLayout ? 17 : 21;
        orderTitle.resizeTextMaxSize = wideLayout ? 27 : shortPortrait ? 31 : 35;
        Text orderFocus = CreateText("OrderFocus", orderCard, CurrentOrderFocusText(), wideLayout ? 23 : shortPortrait ? 27 : 30, FontStyle.Bold, products[primaryIndex].Accent, TextAnchor.MiddleLeft);
        orderFocus.resizeTextForBestFit = true;
        orderFocus.resizeTextMinSize = wideLayout ? 16 : 19;
        orderFocus.resizeTextMaxSize = wideLayout ? 23 : shortPortrait ? 27 : 30;
        Text orderState = CreateText("OrderState", orderCard, hasSellableStock ? F("order.checkout_state", NextAutoSaleSeconds().ToString("0.0", CultureInfo.InvariantCulture)) : CurrentOrderShortageText(), wideLayout ? 22 : shortPortrait ? 25 : 28, FontStyle.Bold, hasSellableStock ? coral : blue, TextAnchor.MiddleLeft);
        orderState.resizeTextForBestFit = true;
        orderState.resizeTextMinSize = wideLayout ? 15 : 18;
        orderState.resizeTextMaxSize = wideLayout ? 22 : shortPortrait ? 25 : 28;
        autoSaleFill = CreateProgress(orderCard, data.queue, coral, wideLayout ? 16f : 20f);

        RectTransform management = CreatePanel("ManagementPanel", contentRoot, new Color(0.36f, 0.23f, 0.14f));
        management.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 360f : shortPortrait ? 470f : 510f;
        VerticalLayoutGroup managementLayout = management.gameObject.AddComponent<VerticalLayoutGroup>();
        managementLayout.padding = wideLayout ? new RectOffset(16, 16, 16, 16) : new RectOffset(20, 20, 20, 20);
        managementLayout.spacing = wideLayout ? 12f : 16f;

        string primaryTitle = F("recommend.checkout", Money(nextItemValue));
        string primaryDetail = F("recommend.checkout_detail", CurrentOrderFocusText());
        string primaryCta = hasSellableStock ? T("action.quick_checkout") : T("action.restock_order");
        Sprite primaryIcon = coinSprite;
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
                primaryTitle = F("recommend.restock_order", Money(orderMissingCost));
                primaryDetail = F("recommend.restock_order_detail", CurrentOrderShortageText());
                primaryCta = restockIndex < 0 ? T("action.full_stock") : !canRestockNow ? ShortCashShort(cheapestRestockCost - data.cash) : data.cash < orderMissingCost ? F("action.restock_order_partial_detail", affordablePriorityRestockAmount, Money(affordablePriorityRestockCost)) : T("action.restock_order");
            }
            else
            {
                primaryTitle = partialRestock ? F("recommend.restock_partial", affordablePriorityRestockAmount, Money(affordablePriorityRestockCost)) : F("recommend.restock", Money(priorityRestockCost));
                primaryDetail = restockIndex < 0 ? T("action.full_stock") : !canRestockNow ? F("log.cash_short", Money(cheapestRestockCost - data.cash)) : T("recommend.restock_detail");
                primaryCta = restockIndex < 0 || priorityRestockAmount <= 0 ? T("action.full_stock") : !canRestockNow ? ShortCashShort(cheapestRestockCost - data.cash) : T("guide.restock");
            }
            primaryIcon = shelfSprite != null ? shelfSprite : upgradeSprite;
            primaryColor = blue;
            primaryAction = BuyRestockAll;
            primaryUnavailable = restockIndex < 0 || priorityRestockAmount <= 0 ? T("action.full_stock") : !canRestockNow ? F("log.cash_short", Money(Mathf.Max(0f, cheapestRestockCost - data.cash))) : string.Empty;
        }
        else if (upgradeIndex >= 0 && data.cash >= upgradeCost)
        {
            primaryTitle = F("recommend.upgrade", UpgradeName(upgradeIndex), Money(upgradeCost));
            primaryDetail = UpgradeImpactText(upgradeIndex, primaryIndex);
            primaryCta = F("action.upgrade", Money(upgradeCost));
            primaryIcon = upgradeSprite;
            primaryColor = honey;
            primaryAction = () =>
            {
                BuyUpgrade(upgradeIndex);
            };
            primaryUnavailable = string.Empty;
        }
        else if (ShouldRecommendStaff() && staffIndex >= 0 && data.cash >= staffCost)
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
            primaryIcon = upgradeSprite;
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
            primaryIcon = coinSprite;
            primaryColor = coral;
            primaryAction = CompleteOrderManualAndRender;
            primaryUnavailable = string.Empty;
        }
        else if (milestoneIndex == 1)
        {
            bool canRestockNow = restockIndex >= 0 && affordablePriorityRestockAmount > 0;
            bool partialRestock = canRestockNow && affordablePriorityRestockAmount < priorityRestockAmount;
            primaryTitle = CurrentMilestoneText();
            primaryDetail = orderHasMissing ? F("recommend.restock_order_detail", CurrentOrderShortageText()) : restockIndex < 0 ? T("action.full_stock") : !canRestockNow ? F("log.cash_short", Money(Mathf.Max(0f, cheapestRestockCost - data.cash))) : T("recommend.restock_detail");
            primaryCta = restockIndex < 0 ? T("action.full_stock") : canRestockNow ? (orderHasMissing ? (data.cash < orderMissingCost ? F("action.restock_order_partial_detail", affordablePriorityRestockAmount, Money(affordablePriorityRestockCost)) : T("action.restock_order")) : partialRestock ? F("action.restock_affordable_detail", affordablePriorityRestockAmount, Money(affordablePriorityRestockCost)) : T("guide.restock")) : hasSellableStock ? T("action.quick_checkout") : ShortCashShort(cheapestRestockCost - data.cash);
            primaryIcon = shelfSprite != null ? shelfSprite : upgradeSprite;
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
        else if (milestoneIndex == 2 && data.upgrades.Length > 2 && data.upgrades[2] < upgrades[2].MaxLevel)
        {
            int priceUpgradeCost = UpgradeCost(2);
            primaryTitle = data.cash >= priceUpgradeCost ? F("recommend.upgrade", UpgradeName(2), Money(priceUpgradeCost)) : F("recommend.upgrade_goal", UpgradeName(2), Money(priceUpgradeCost));
            primaryDetail = data.cash >= priceUpgradeCost ? UpgradeImpactText(2, primaryIndex) : F("recommend.upgrade_goal_detail", UpgradeImpactText(2, primaryIndex), Money(priceUpgradeCost - data.cash));
            primaryCta = data.cash >= priceUpgradeCost ? F("action.upgrade", Money(priceUpgradeCost)) : hasSellableStock ? T("action.quick_checkout") : T("guide.restock");
            primaryIcon = upgradeSprite;
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
        else if (milestoneIndex == 3 && data.staff.Length > 0 && data.staff[0] < staffDefs[0].MaxLevel)
        {
            int cashierCost = StaffCost(0);
            primaryTitle = data.cash >= cashierCost ? F("recommend.staff", StaffName(0), Money(cashierCost)) : F("recommend.staff_goal", StaffName(0), Money(cashierCost));
            primaryDetail = data.cash >= cashierCost ? StaffAutomationText(0) : F("recommend.staff_goal_detail", StaffAutomationText(0), Money(cashierCost - data.cash));
            primaryCta = data.cash >= cashierCost ? F("action.train", Money(cashierCost)) : hasSellableStock ? T("action.quick_checkout") : T("guide.restock");
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

        CreateFeaturedActionCard(management, primaryTitle, primaryDetail, primaryCta, primaryIcon, primaryColor, () =>
        {
            if (!string.IsNullOrEmpty(primaryUnavailable))
            {
                AddLog(primaryUnavailable);
                RenderAll();
                return;
            }

            primaryAction();
        });

        RectTransform quickActions = CreateRect("QuickActions", management);
        quickActions.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 124f : shortPortrait ? 164f : 184f;
        HorizontalLayoutGroup quickLayout = quickActions.gameObject.AddComponent<HorizontalLayoutGroup>();
        quickLayout.spacing = wideLayout ? 10f : 12f;
        quickLayout.childAlignment = TextAnchor.MiddleCenter;
        quickLayout.childControlWidth = true;
        quickLayout.childControlHeight = true;
        quickLayout.childForceExpandWidth = true;
        quickLayout.childForceExpandHeight = true;

        CreateQuickActionButton(quickActions, T("action.quick_checkout"), hasSellableStock ? F("action.checkout_one_detail", ProductName(primaryIndex), Money(nextItemValue)) : CurrentOrderShortageText(), coinSprite, coral, () =>
        {
            if (!HasSellableStock())
            {
                AddLog(CurrentOrderShortageText());
                RenderAll();
                return;
            }

            CompleteOrderManualAndRender();
        });

        CreateQuickActionButton(quickActions, orderHasMissing ? T("action.restock_order") : T("action.restock_all"), RestockAllButtonDetail(restockIndex, priorityRestockAmount, priorityRestockCost, cheapestRestockCost, affordablePriorityRestockAmount, affordablePriorityRestockCost), shelfSprite != null ? shelfSprite : upgradeSprite, blue, () =>
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
        });

        CreateQuickActionButton(quickActions, upgradeIndex >= 0 ? T("tab.upgrades") : T("action.maxed"), UpgradeButtonDetail(upgradeIndex, upgradeCost), upgradeSprite, honey, () =>
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
        });

        RectTransform day = CreatePanel("Day", contentRoot, panel);
        day.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 134f : shortPortrait ? 158f : 176f;
        VerticalLayoutGroup dayLayout = day.gameObject.AddComponent<VerticalLayoutGroup>();
        dayLayout.padding = wideLayout ? new RectOffset(18, 18, 12, 12) : new RectOffset(22, 22, 14, 14);
        dayLayout.spacing = wideLayout ? 7f : 9f;
        CreateText("DayTitle", day, F("milestone.panel_title", data.day), wideLayout ? 30 : shortPortrait ? 32 : 36, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        Text milestone = CreateText("Milestone", day, CurrentMilestoneText(), wideLayout ? 25 : shortPortrait ? 28 : 31, FontStyle.Bold, blue, TextAnchor.MiddleLeft);
        milestone.resizeTextForBestFit = true;
        milestone.resizeTextMinSize = wideLayout ? 18 : 21;
        milestone.resizeTextMaxSize = wideLayout ? 25 : shortPortrait ? 28 : 31;
        CreateProgress(day, MilestoneProgress(CurrentMilestoneIndex()), blue, wideLayout ? 18f : 22f);
        Text hint = CreateText("MilestoneHint", day, CurrentMilestoneHint(), wideLayout ? 22 : shortPortrait ? 24 : 26, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        hint.resizeTextForBestFit = true;
        hint.resizeTextMinSize = wideLayout ? 16 : 19;
        hint.resizeTextMaxSize = wideLayout ? 22 : shortPortrait ? 24 : 26;
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

        RectTransform resetPanel = CreatePanel("ResetPanel", contentRoot, panel);
        resetPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 190f : 230f;
        VerticalLayoutGroup resetLayout = resetPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        resetLayout.padding = wideLayout ? new RectOffset(24, 24, 18, 18) : new RectOffset(26, 26, 22, 22);
        resetLayout.spacing = wideLayout ? 12f : 14f;
        CreateText("ResetTitle", resetPanel, T("settings.reset_title"), wideLayout ? 32 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateText("ResetBody", resetPanel, T("settings.reset_body"), wideLayout ? 23 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        CreateButton(resetPanel, T("action.reset_save"), coral, ResetSave);
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

    private Button CreateQuickActionButton(RectTransform parent, string title, string detail, Sprite icon, Color color, UnityEngine.Events.UnityAction action)
    {
        bool shortPortrait = IsShortPortraitScreen();
        RectTransform card = CreatePanel("QuickActionButton", parent, color);
        LayoutElement cardLayout = card.gameObject.AddComponent<LayoutElement>();
        cardLayout.flexibleWidth = 1f;
        cardLayout.flexibleHeight = 0f;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = wideLayout ? new RectOffset(9, 9, 8, 8) : shortPortrait ? new RectOffset(10, 10, 9, 9) : new RectOffset(11, 11, 10, 10);
        layout.spacing = wideLayout ? 4f : 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform iconBox = CreatePanel("QuickActionIcon", card, new Color(1f, 0.95f, 0.80f, 0.22f));
        LayoutElement iconLayout = iconBox.gameObject.AddComponent<LayoutElement>();
        iconLayout.minHeight = wideLayout ? 42f : shortPortrait ? 48f : 56f;
        iconLayout.preferredHeight = wideLayout ? 42f : shortPortrait ? 48f : 56f;
        iconLayout.flexibleHeight = 0f;
        if (icon != null)
        {
            CreateImage("Icon", iconBox, icon, true);
        }

        Text titleText = CreateText("Title", card, title, wideLayout ? 22 : shortPortrait ? 25 : 27, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = wideLayout ? 15 : 18;
        titleText.resizeTextMaxSize = wideLayout ? 22 : shortPortrait ? 25 : 27;
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.minHeight = wideLayout ? 28f : shortPortrait ? 32f : 36f;
        titleLayout.preferredHeight = wideLayout ? 28f : shortPortrait ? 32f : 36f;
        titleLayout.flexibleHeight = 0f;

        Text detailText = CreateText("Detail", card, detail, wideLayout ? 18 : shortPortrait ? 21 : 23, FontStyle.Bold, new Color(1f, 0.92f, 0.74f), TextAnchor.MiddleCenter);
        detailText.resizeTextForBestFit = true;
        detailText.resizeTextMinSize = wideLayout ? 13 : 16;
        detailText.resizeTextMaxSize = wideLayout ? 18 : shortPortrait ? 21 : 23;
        LayoutElement detailLayout = detailText.gameObject.AddComponent<LayoutElement>();
        detailLayout.minHeight = wideLayout ? 24f : shortPortrait ? 28f : 32f;
        detailLayout.preferredHeight = wideLayout ? 24f : shortPortrait ? 28f : 32f;
        detailLayout.flexibleHeight = 0f;

        Button button = card.gameObject.AddComponent<Button>();
        card.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = card.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, color);
        return button;
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
               name == "FeaturedActionCard" ||
               name == "QuickActionButton" ||
               name == "QuickActionIcon" ||
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
