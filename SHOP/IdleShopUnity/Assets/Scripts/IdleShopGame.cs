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
        public Color Accent;

        public ProductDef(string nameKey, string subtitleKey, float cost, float price, float demand, int unlockLevel, int baseMaxStock, Color accent)
        {
            NameKey = nameKey;
            SubtitleKey = subtitleKey;
            Cost = cost;
            Price = price;
            Demand = demand;
            UnlockLevel = unlockLevel;
            BaseMaxStock = baseMaxStock;
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
        public float cash = 120f;
        public float reputation;
        public float xp;
        public float totalEarned;
        public float dayProgress;
        public float queue;
        public int level = 1;
        public int day = 1;
        public int soldToday;
        public long lastSavedUnix;
        public float[] stock = Array.Empty<float>();
        public int[] sold = Array.Empty<int>();
        public bool[] unlocked = Array.Empty<bool>();
        public int[] upgrades = Array.Empty<int>();
        public int[] staff = Array.Empty<int>();
    }

    private const string SaveKey = "PocketShop.Unity.Save.V1";
    private const float DayLength = 90f;
    private readonly ProductDef[] products =
    {
        new ProductDef("product.rice_ball.name", "product.rice_ball.subtitle", 6f, 11f, 1.4f, 1, 18, new Color(0.94f, 0.48f, 0.32f)),
        new ProductDef("product.sparkling_water.name", "product.sparkling_water.subtitle", 9f, 18f, 1.15f, 2, 14, new Color(0.18f, 0.58f, 0.72f)),
        new ProductDef("product.lunch_box.name", "product.lunch_box.subtitle", 18f, 34f, 0.85f, 4, 10, new Color(0.89f, 0.64f, 0.22f)),
        new ProductDef("product.flower.name", "product.flower.subtitle", 32f, 62f, 0.52f, 6, 8, new Color(0.78f, 0.38f, 0.62f))
    };

    private readonly UpgradeDef[] upgrades =
    {
        new UpgradeDef("upgrade.shelf.name", "upgrade.shelf.detail", 90f, 8),
        new UpgradeDef("upgrade.signboard.name", "upgrade.signboard.detail", 120f, 10),
        new UpgradeDef("upgrade.fridge.name", "upgrade.fridge.detail", 180f, 7),
        new UpgradeDef("upgrade.express.name", "upgrade.express.detail", 240f, 7)
    };

    private readonly StaffDef[] staffDefs =
    {
        new StaffDef("staff.cashier.name", "staff.cashier.detail", 140f, 10),
        new StaffDef("staff.stocker.name", "staff.stocker.detail", 170f, 10),
        new StaffDef("staff.promoter.name", "staff.promoter.detail", 210f, 10)
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
    private Text autoSaleText = null!;
    private Text autoIntervalText = null!;
    private RectTransform autoSaleFill = null!;
    private Text offlineText = null!;
    private LayoutElement offlineLayout = null!;
    private readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
    private float renderTimer;
    private float saveTimer;
    private bool contentDirty;
    private bool wideLayout;
    private Vector2Int layoutScreenSize;
    private string offlineNotice = string.Empty;

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
        renderTimer += Time.deltaTime;
        saveTimer += Time.deltaTime;

        if (renderTimer >= 0.35f)
        {
            RenderHeader();
            if (contentDirty && activeTab == Tab.Shop)
            {
                RenderTab();
                contentDirty = false;
            }

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

    private void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            }
            catch
            {
                data = new SaveData();
            }
        }

        NormalizeData();
        if (log.Count == 0)
        {
            AddLog(T("log.opening"));
        }
    }

    private void NormalizeData()
    {
        data.stock = Resize(data.stock, products.Length);
        data.sold = Resize(data.sold, products.Length);
        data.unlocked = Resize(data.unlocked, products.Length);
        data.upgrades = Resize(data.upgrades, upgrades.Length);
        data.staff = Resize(data.staff, staffDefs.Length);

        data.level = Mathf.Max(1, data.level);
        data.cash = Mathf.Max(0f, data.cash);
        data.day = Mathf.Max(1, data.day);
        data.unlocked[0] = true;

        for (int i = 0; i < products.Length; i++)
        {
            if (data.level >= products[i].UnlockLevel)
            {
                data.unlocked[i] = true;
            }
        }

        if (data.stock[0] <= 0f && data.totalEarned <= 0.01f)
        {
            data.stock[0] = 8f;
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
        float elapsed = Mathf.Clamp((float)(now - data.lastSavedUnix), 0f, 8f * 60f * 60f);
        if (elapsed < 20f)
        {
            return;
        }

        float before = data.cash;
        Simulate(elapsed, true);
        float gained = Mathf.Max(0f, data.cash - before);
        if (gained > 0f)
        {
            offlineNotice = F("log.offline_income", Mathf.FloorToInt(elapsed / 60f), Money(gained));
            AddLog(offlineNotice);
        }
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

        data.queue += seconds * TrafficPerSecond();
        int served = 0;
        int serveLimit = offline ? 12000 : 35;
        while (data.queue >= 1f && served < serveLimit)
        {
            data.queue -= 1f;
            bool sold = SellOne(false);
            if (!sold && offline)
            {
                break;
            }

            served++;
        }
    }

    private bool SellOne(bool manual)
    {
        int productIndex = ChooseProductIndex();
        if (productIndex < 0)
        {
            if (manual)
            {
                AddLog(T("log.empty_shelf"));
            }

            data.reputation = Mathf.Max(0f, data.reputation - 0.02f);
            return false;
        }

        data.stock[productIndex] -= 1f;
        data.sold[productIndex] += 1;
        int earned = SalePrice(productIndex);
        data.cash += earned;
        data.totalEarned += earned;
        data.soldToday += 1;
        data.xp += 12f + Mathf.Floor(earned / 10f);
        data.reputation += 0.018f * (1f + data.staff[2] * 0.16f);

        if (manual || UnityEngine.Random.value < 0.14f)
        {
            AddLog(F("log.sold", ProductName(productIndex), Money(earned)));
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

        contentDirty = true;
        return true;
    }

    private int ChooseProductIndex()
    {
        float total = 0f;
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i] && data.stock[i] >= 1f)
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
            if (!data.unlocked[i] || data.stock[i] < 1f)
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

    private float TrafficPerSecond()
    {
        return 0.24f + data.level * 0.025f + data.upgrades[1] * 0.055f + data.staff[0] * 0.07f + data.reputation * 0.006f;
    }

    private float AutoSaleInterval()
    {
        return 1f / Mathf.Max(0.01f, TrafficPerSecond());
    }

    private bool HasSellableStock()
    {
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i] && data.stock[i] >= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private float NextAutoSaleSeconds()
    {
        if (!HasSellableStock())
        {
            return -1f;
        }

        float rate = Mathf.Max(0.01f, TrafficPerSecond());
        return Mathf.Max(0f, (1f - Mathf.Clamp01(data.queue)) / rate);
    }

    private float XpNeeded()
    {
        return 95f + data.level * 38f;
    }

    private int MaxStock(int productIndex)
    {
        return products[productIndex].BaseMaxStock + data.upgrades[0] * 5;
    }

    private int SalePrice(int productIndex)
    {
        return Mathf.FloorToInt(products[productIndex].Price * (1f + data.upgrades[2] * 0.065f));
    }

    private int UnitCost(int productIndex)
    {
        return Mathf.Max(1, Mathf.FloorToInt(products[productIndex].Cost * (1f - data.upgrades[3] * 0.045f)));
    }

    private int RestockPack()
    {
        return 5 + data.staff[1] * 2;
    }

    private int PrimaryProductIndex()
    {
        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i] && data.stock[i] >= 1f)
            {
                return i;
            }
        }

        for (int i = 0; i < products.Length; i++)
        {
            if (data.unlocked[i])
            {
                return i;
            }
        }

        return 0;
    }

    private int BestRestockIndex()
    {
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

            int capacity = MaxStock(i) - Mathf.FloorToInt(data.stock[i]);
            int amount = Mathf.Min(capacity, RestockPack());
            if (amount <= 0)
            {
                continue;
            }

            totalAmount += amount;
            totalCost += amount * UnitCost(i);
        }
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
        int bestIndex = -1;
        int bestCost = int.MaxValue;
        for (int i = 0; i < upgrades.Length; i++)
        {
            if (data.upgrades[i] >= upgrades[i].MaxLevel)
            {
                continue;
            }

            int cost = UpgradeCost(i);
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

    private int UpgradeCost(int upgradeIndex)
    {
        return Mathf.FloorToInt(upgrades[upgradeIndex].BaseCost * Mathf.Pow(1.72f, data.upgrades[upgradeIndex]));
    }

    private int StaffCost(int staffIndex)
    {
        return Mathf.FloorToInt(staffDefs[staffIndex].BaseCost * Mathf.Pow(1.82f, data.staff[staffIndex]));
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
        AddLog(F("log.restocked", ProductName(productIndex), amount));
        RenderAll();
    }

    private void BuyRestockAll()
    {
        int bought = 0;
        while (data.cash >= 1f)
        {
            int index = BestRestockIndex();
            if (index < 0)
            {
                break;
            }

            int unitCost = UnitCost(index);
            if (data.cash < unitCost)
            {
                break;
            }

            data.cash -= unitCost;
            data.stock[index] += 1f;
            bought++;
        }

        if (bought <= 0)
        {
            int cheapest = CheapestRestockCost();
            AddLog(cheapest > 0 ? F("log.cash_short", Money(cheapest - data.cash)) : T("action.full_stock"));
            RenderAll();
            return;
        }

        AddLog(F("log.restocked_all", bought));
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
        AddLog(F("log.upgraded", UpgradeName(upgradeIndex), data.upgrades[upgradeIndex]));
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
        data.queue += 5f + data.staff[2];
        data.reputation += 0.08f;
        AddLog(T("log.advertised"));
        RenderAll();
    }

    private void AddLog(string message)
    {
        log.Insert(0, message);
        contentDirty = true;
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

    private string StaffName(int index)
    {
        return T(staffDefs[index].NameKey);
    }

    private string StaffDetail(int index)
    {
        return T(staffDefs[index].DetailKey);
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
        safe.gameObject.AddComponent<SafeAreaFitter>().SetPadding(wideLayout ? 16f : 18f, wideLayout ? 16f : 18f, 10f, 12f);

        if (wideLayout)
        {
            BuildWideShell(safe);
        }
        else
        {
            BuildPortraitShell(safe);
        }

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
        const float headerHeight = 112f;
        const float navHeight = 104f;
        const float gap = 10f;

        RectTransform header = CreateHeader(safe, headerHeight, false);
        AnchorTop(header, 0f, headerHeight);

        RectTransform stats = CreateStats(safe, false);
        stats.gameObject.SetActive(false);

        RectTransform offline = CreateOfflineNotice(safe);
        AnchorTop(offline, headerHeight + gap, 44f);

        RectTransform nav = CreateNavigation(safe, false);
        AnchorBottom(nav, 0f, navHeight);

        ScrollRect scroll = CreateScroll(safe);
        AnchorFill(scroll.GetComponent<RectTransform>(), 0f, 0f, headerHeight + gap, navHeight + gap);
        contentRoot = scroll.content;
    }

    private void BuildWideShell(RectTransform safe)
    {
        const float navWidth = 132f;
        const float gap = 10f;
        const float headerHeight = 76f;

        RectTransform nav = CreateNavigation(safe, true);
        AnchorLeft(nav, 0f, navWidth);

        RectTransform main = CreateRect("Main", safe);
        AnchorFill(main, navWidth + gap, 0f, 0f, 0f);

        RectTransform header = CreateHeader(main, headerHeight, true);
        AnchorTop(header, 0f, headerHeight);

        RectTransform stats = CreateStats(main, true);
        stats.gameObject.SetActive(false);

        RectTransform offline = CreateOfflineNotice(main);
        AnchorTop(offline, headerHeight + gap, 36f);

        ScrollRect scroll = CreateScroll(main);
        AnchorFill(scroll.GetComponent<RectTransform>(), 0f, 0f, headerHeight + gap, 0f);
        contentRoot = scroll.content;
    }

    private RectTransform CreateHeader(RectTransform parent, float height, bool compact)
    {
        RectTransform top = CreatePanel("Top", parent, pine);
        top.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        HorizontalLayoutGroup topLayout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = compact ? new RectOffset(18, 18, 10, 10) : new RectOffset(24, 24, 14, 14);
        topLayout.spacing = compact ? 14f : 18f;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = true;
        topLayout.childForceExpandWidth = true;

        RectTransform titleGroup = CreateRect("TitleGroup", top);
        titleGroup.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup titleLayout = titleGroup.gameObject.AddComponent<VerticalLayoutGroup>();
        titleLayout.childControlHeight = true;
        titleLayout.childForceExpandHeight = false;
        titleText = CreateText("Title", titleGroup, T("app.title"), compact ? 36 : 50, FontStyle.Bold, panel, TextAnchor.MiddleLeft);
        CreateText("Subtitle", titleGroup, T("app.subtitle"), compact ? 20 : 28, FontStyle.Bold, new Color(0.76f, 0.90f, 0.80f), TextAnchor.MiddleLeft);

        queueText = CreateText("Queue", top, "", compact ? 26 : 34, FontStyle.Bold, panel, TextAnchor.MiddleRight);
        queueText.gameObject.AddComponent<LayoutElement>().preferredWidth = compact ? 240f : 300f;
        return top;
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
            tabsLayoutElement.preferredHeight = 104f;
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
        contentDirty = false;
    }

    private void RenderHeader()
    {
        int unlockedCount = 0;
        for (int i = 0; i < data.unlocked.Length; i++)
        {
            if (data.unlocked[i])
            {
                unlockedCount++;
            }
        }

        titleText.text = F("header.level", T("app.title"), data.level);
        cashText.text = $"{T("stat.cash")}\n{Money(data.cash)}";
        reputationText.text = $"{T("stat.reputation")}\n{Compact(data.reputation)}";
        totalText.text = $"{T("stat.total")}\n{Money(data.totalEarned)}";
        newItemsText.text = $"{T("stat.new_items")}\n{unlockedCount}/{products.Length}";
        queueText.text = $"{T("stat.cash")}\n{Money(data.cash)}";
        offlineText.text = offlineNotice;
        bool hasOfflineNotice = !string.IsNullOrEmpty(offlineNotice);
        offlineText.gameObject.SetActive(hasOfflineNotice);
        offlineLayout.preferredHeight = hasOfflineNotice ? 42f : 0f;
        UpdateAutoSaleIndicator();
        UpdateTabButtons();
    }

    private void UpdateAutoSaleIndicator()
    {
        if (autoSaleText == null || autoSaleFill == null)
        {
            return;
        }

        if (!HasSellableStock())
        {
            autoSaleText.text = T("shop.waiting_restock");
            autoSaleFill.anchorMax = new Vector2(0f, 1f);
        }
        else
        {
            string seconds = NextAutoSaleSeconds().ToString("0.0", CultureInfo.InvariantCulture);
            autoSaleText.text = F("shop.auto_timer", seconds);
            autoSaleFill.anchorMax = new Vector2(Mathf.Clamp01(data.queue), 1f);
        }

        autoSaleFill.offsetMin = Vector2.zero;
        autoSaleFill.offsetMax = Vector2.zero;
        if (autoIntervalText != null)
        {
            autoIntervalText.text = F("shop.auto_interval", AutoSaleInterval().ToString("0.0", CultureInfo.InvariantCulture));
        }
    }

    private void RenderTab()
    {
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
        CalculateRestockAllPlan(out int restockAllAmount, out int restockAllCost);
        int cheapestRestockCost = CheapestRestockCost();
        int autoIncomePerMinute = Mathf.FloorToInt(TrafficPerSecond() * 60f * SalePrice(primaryIndex));
        int upgradeCost = upgradeIndex >= 0 ? UpgradeCost(upgradeIndex) : 0;

        RectTransform stage = CreatePanel("GameStage", contentRoot, new Color(0.16f, 0.36f, 0.32f));
        stage.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 600f : 780f;

        RectTransform stageArt = CreateImagePanel("ShopFront", stage, shopFrontSprite, new Color(0.18f, 0.32f, 0.30f), false);
        AnchorFill(stageArt, 0f, 0f, 0f, 0f);

        RectTransform shade = CreatePanel("StageShade", stage, new Color(0.02f, 0.08f, 0.07f, 0.36f));
        AnchorFill(shade, 0f, 0f, 0f, 0f);

        RectTransform resourceRow = CreateRect("StageResources", stage);
        resourceRow.anchorMin = new Vector2(0f, 1f);
        resourceRow.anchorMax = new Vector2(1f, 1f);
        resourceRow.pivot = new Vector2(0.5f, 1f);
        resourceRow.offsetMin = new Vector2(wideLayout ? 18f : 20f, wideLayout ? -84f : -104f);
        resourceRow.offsetMax = new Vector2(wideLayout ? -100f : -118f, wideLayout ? -16f : -20f);
        HorizontalLayoutGroup resourceLayout = resourceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        resourceLayout.spacing = wideLayout ? 10f : 12f;
        resourceLayout.childControlWidth = true;
        resourceLayout.childControlHeight = true;
        resourceLayout.childForceExpandWidth = true;
        resourceLayout.childForceExpandHeight = true;
        CreateResourceChip(resourceRow, T("stat.cash"), Money(data.cash), coinSprite, honey);
        CreateResourceChip(resourceRow, T("stat.reputation"), Compact(data.reputation), reputationSprite, coral);
        CreateResourceChip(resourceRow, T("stat.total"), Money(autoIncomePerMinute), customerSprite, blue);

        RectTransform rightRail = CreateRect("StageRail", stage);
        rightRail.anchorMin = new Vector2(1f, 0.36f);
        rightRail.anchorMax = new Vector2(1f, 0.82f);
        rightRail.pivot = new Vector2(1f, 0.5f);
        rightRail.offsetMin = new Vector2(wideLayout ? -94f : -108f, 0f);
        rightRail.offsetMax = new Vector2(wideLayout ? -18f : -20f, 0f);
        VerticalLayoutGroup railLayout = rightRail.gameObject.AddComponent<VerticalLayoutGroup>();
        railLayout.spacing = wideLayout ? 10f : 12f;
        railLayout.childControlWidth = true;
        railLayout.childControlHeight = true;
        railLayout.childForceExpandWidth = true;
        railLayout.childForceExpandHeight = true;
        CreateRoundShortcut(rightRail, coinSprite, coral, () =>
        {
            SellOne(true);
            RenderAll();
        }).interactable = data.stock[primaryIndex] >= 1f;
        CreateRoundShortcut(rightRail, shelfSprite != null ? shelfSprite : upgradeSprite, blue, BuyRestockAll).interactable = restockIndex >= 0 && restockAllAmount > 0 && cheapestRestockCost > 0 && data.cash >= cheapestRestockCost;
        CreateRoundShortcut(rightRail, upgradeSprite, honey, () =>
        {
            if (upgradeIndex >= 0)
            {
                BuyUpgrade(upgradeIndex);
            }
        }).interactable = upgradeIndex >= 0 && data.cash >= upgradeCost;
        CreateRoundShortcut(rightRail, reputationSprite, pine, Advertise);

        if (shelfSprite != null || counterSprite != null)
        {
            RectTransform overlay = CreateRect("ShopOverlay", stage);
            overlay.anchorMin = new Vector2(0.05f, 0.20f);
            overlay.anchorMax = new Vector2(0.84f, 0.48f);
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

        RectTransform focus = CreatePanel("FocusProduct", stage, new Color(1.0f, 0.94f, 0.82f, 0.94f));
        focus.anchorMin = new Vector2(0.04f, 0.04f);
        focus.anchorMax = new Vector2(0.96f, 0.22f);
        focus.offsetMin = Vector2.zero;
        focus.offsetMax = Vector2.zero;
        HorizontalLayoutGroup focusLayout = focus.gameObject.AddComponent<HorizontalLayoutGroup>();
        focusLayout.padding = wideLayout ? new RectOffset(16, 16, 14, 14) : new RectOffset(20, 20, 18, 18);
        focusLayout.spacing = wideLayout ? 16f : 20f;
        focusLayout.childAlignment = TextAnchor.MiddleCenter;
        focusLayout.childControlWidth = true;
        focusLayout.childControlHeight = true;

        RectTransform productIcon = CreatePanel("FocusIcon", focus, products[primaryIndex].Accent);
        productIcon.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 110f : 130f;
        if (ProductSprite(primaryIndex) != null)
        {
            CreateImage("FocusProductArt", productIcon, ProductSprite(primaryIndex), true);
        }

        RectTransform focusInfo = CreateRect("FocusInfo", focus);
        focusInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup focusInfoLayout = focusInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        focusInfoLayout.spacing = wideLayout ? 8f : 10f;
        CreateText("FocusName", focusInfo, $"{ProductName(primaryIndex)}  Lv.{data.level}", wideLayout ? 36 : 44, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        autoSaleText = CreateText("AutoSaleTimer", focusInfo, HasSellableStock() ? F("shop.auto_timer", NextAutoSaleSeconds().ToString("0.0", CultureInfo.InvariantCulture)) : T("shop.waiting_restock"), wideLayout ? 29 : 35, FontStyle.Bold, coral, TextAnchor.MiddleLeft);
        autoSaleFill = CreateProgress(focusInfo, HasSellableStock() ? Mathf.Clamp01(data.queue) : 0f, products[primaryIndex].Accent);
        autoIntervalText = CreateText("AutoSaleInterval", focusInfo, $"{F("shop.auto_interval", AutoSaleInterval().ToString("0.0", CultureInfo.InvariantCulture))} · {F("stock.stock_count", Mathf.FloorToInt(data.stock[primaryIndex]), MaxStock(primaryIndex))}", wideLayout ? 22 : 27, FontStyle.Bold, ink, TextAnchor.MiddleLeft);

        RectTransform management = CreatePanel("ManagementPanel", contentRoot, new Color(0.36f, 0.23f, 0.14f));
        management.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 500f : 610f;
        VerticalLayoutGroup managementLayout = management.gameObject.AddComponent<VerticalLayoutGroup>();
        managementLayout.padding = wideLayout ? new RectOffset(18, 18, 18, 18) : new RectOffset(22, 22, 22, 22);
        managementLayout.spacing = wideLayout ? 12f : 14f;

        RectTransform tabStrip = CreatePanel("SheetTabs", management, new Color(0.22f, 0.13f, 0.08f));
        tabStrip.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 70f : 84f;
        HorizontalLayoutGroup tabLayout = tabStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding = new RectOffset(8, 8, 8, 8);
        tabLayout.spacing = 8f;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;
        CreateSheetTab(tabStrip, T("guide.sell"), true);
        CreateSheetTab(tabStrip, T("guide.restock"), false);
        CreateSheetTab(tabStrip, T("guide.grow"), false);

        RectTransform cards = CreateRect("HomeCards", management);
        cards.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup cardLayout = cards.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.spacing = wideLayout ? 12f : 14f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = true;

        Button checkoutButton = CreateHomeActionCard(cards, T("action.manual_boost"), $"+{Money(SalePrice(primaryIndex))} · {ProductName(primaryIndex)}", coinSprite, coral, () =>
        {
            SellOne(true);
            RenderAll();
        });
        checkoutButton.interactable = data.stock[primaryIndex] >= 1f;

        Button restockButton = CreateHomeActionCard(cards, restockIndex >= 0 ? T("action.restock_all") : T("action.full_stock"), restockIndex >= 0 ? F("action.restock_all_detail", restockAllAmount, Money(restockAllCost)) : T("action.full_stock"), shelfSprite != null ? shelfSprite : upgradeSprite, blue, () =>
        {
            BuyRestockAll();
        });
        restockButton.interactable = restockIndex >= 0 && restockAllAmount > 0 && cheapestRestockCost > 0 && data.cash >= cheapestRestockCost;

        Button upgradeButton = CreateHomeActionCard(cards, upgradeIndex >= 0 ? UpgradeName(upgradeIndex) : T("action.maxed"), upgradeIndex >= 0 ? F("action.upgrade", Money(upgradeCost)) : T("action.maxed"), upgradeSprite, honey, () =>
        {
            if (upgradeIndex >= 0)
            {
                BuyUpgrade(upgradeIndex);
            }
        });
        upgradeButton.interactable = upgradeIndex >= 0 && data.cash >= upgradeCost;

        RectTransform day = CreatePanel("Day", contentRoot, panel);
        day.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 180f : 210f;
        VerticalLayoutGroup dayLayout = day.gameObject.AddComponent<VerticalLayoutGroup>();
        dayLayout.padding = wideLayout ? new RectOffset(18, 18, 12, 12) : new RectOffset(22, 22, 16, 16);
        dayLayout.spacing = wideLayout ? 8f : 10f;
        CreateText("DayTitle", day, F("day.title", data.day), wideLayout ? 32 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateProgress(day, data.dayProgress, honey);
        foreach (string item in log)
        {
            CreateText("Log", day, item, wideLayout ? 25 : 30, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
        }
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

    private void RenderStock()
    {
        CreateSectionTitle(T("stock.title"), F("stock.subtitle", RestockPack()));
        for (int i = 0; i < products.Length; i++)
        {
            int index = i;
            ProductDef product = products[i];
            bool locked = !data.unlocked[i];
            RectTransform row = CreatePanel("Product", contentRoot, locked ? new Color(0.88f, 0.86f, 0.82f) : panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 172f : 198f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = wideLayout ? new RectOffset(18, 18, 14, 14) : new RectOffset(20, 20, 16, 16);
            layout.spacing = wideLayout ? 16f : 18f;

            RectTransform swatch = CreatePanel("Swatch", row, locked ? Color.gray : product.Accent);
            swatch.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 108f : 124f;
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
            infoLayout.spacing = wideLayout ? 7f : 9f;
            CreateText("Name", info, locked ? F("stock.unlock_at", ProductName(i), product.UnlockLevel) : ProductName(i), wideLayout ? 30 : 36, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Meta", info, locked ? ProductSubtitle(i) : F("stock.price_cost", Money(SalePrice(i)), Money(UnitCost(i))), wideLayout ? 23 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
            CreateProgress(info, locked ? 0f : data.stock[i] / MaxStock(i), product.Accent);
            CreateText("Stock", info, locked ? T("stock.wait_level") : F("stock.stock_count", Mathf.FloorToInt(data.stock[i]), MaxStock(i)), wideLayout ? 22 : 26, FontStyle.Bold, ink, TextAnchor.MiddleLeft);

            int amount = Mathf.Min(MaxStock(i) - Mathf.FloorToInt(data.stock[i]), RestockPack());
            int cost = amount * UnitCost(i);
            Button button = CreateButton(row, locked ? T("action.locked") : amount <= 0 ? T("action.full_stock") : F("action.restock", Money(cost)), pine, () => BuyStock(index));
            button.interactable = !locked && amount > 0 && data.cash >= cost;
            button.gameObject.GetComponent<LayoutElement>().preferredWidth = wideLayout ? 210f : 250f;
        }
    }

    private void RenderUpgrades()
    {
        CreateSectionTitle(T("upgrades.title"), T("upgrades.subtitle"));
        for (int i = 0; i < upgrades.Length; i++)
        {
            int index = i;
            UpgradeDef upgrade = upgrades[i];
            int level = data.upgrades[i];
            int cost = UpgradeCost(i);
            bool maxed = level >= upgrade.MaxLevel;
            RectTransform row = CreatePanel("Upgrade", contentRoot, panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 164f : 188f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = wideLayout ? new RectOffset(18, 18, 14, 14) : new RectOffset(20, 20, 16, 16);
            layout.spacing = wideLayout ? 18f : 20f;

            RectTransform info = CreateRect("Info", row);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup infoLayout = info.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = wideLayout ? 7f : 10f;
            CreateText("Name", info, $"{UpgradeName(i)}  {F("level.short", level, upgrade.MaxLevel)}", wideLayout ? 31 : 37, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Detail", info, UpgradeDetail(i), wideLayout ? 24 : 28, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

            Button button = CreateButton(row, maxed ? T("action.maxed") : F("action.upgrade", Money(cost)), honey, () => BuyUpgrade(index));
            button.interactable = !maxed && data.cash >= cost;
            button.gameObject.GetComponent<LayoutElement>().preferredWidth = wideLayout ? 240f : 280f;
        }
    }

    private void RenderStaff()
    {
        CreateSectionTitle(T("staff.title"), T("staff.subtitle"));
        for (int i = 0; i < staffDefs.Length; i++)
        {
            int index = i;
            StaffDef person = staffDefs[i];
            int level = data.staff[i];
            int cost = StaffCost(i);
            bool maxed = level >= person.MaxLevel;
            RectTransform row = CreatePanel("Staff", contentRoot, panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 176f : 202f;
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = wideLayout ? new RectOffset(18, 18, 14, 14) : new RectOffset(20, 20, 16, 16);
            layout.spacing = wideLayout ? 18f : 20f;

            RectTransform avatar = CreatePanel("Avatar", row, i == 0 ? coral : i == 1 ? blue : honey);
            avatar.gameObject.AddComponent<LayoutElement>().preferredWidth = wideLayout ? 126f : 142f;
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
            infoLayout.spacing = wideLayout ? 7f : 10f;
            CreateText("Name", info, $"{StaffName(i)}  {F("level.short", level, person.MaxLevel)}", wideLayout ? 31 : 37, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
            CreateText("Detail", info, StaffDetail(i), wideLayout ? 24 : 28, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

            Button button = CreateButton(row, maxed ? T("action.maxed") : F("action.train", Money(cost)), pine, () => HireStaff(index));
            button.interactable = !maxed && data.cash >= cost;
            button.gameObject.GetComponent<LayoutElement>().preferredWidth = wideLayout ? 240f : 280f;
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
            Button button = CreateButton(languageButtons, language.Name, language.Code == LocalizationManager.CurrentLanguage ? coral : pine, () => ChangeLanguage(language.Code, language.Name));
            button.interactable = language.Code != LocalizationManager.CurrentLanguage;
        }

        CreateText("LanguageHint", languagePanel, T("settings.language_hint"), wideLayout ? 22 : 25, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

        RectTransform futurePanel = CreatePanel("FuturePanel", contentRoot, new Color(0.83f, 0.91f, 0.82f));
        futurePanel.gameObject.AddComponent<LayoutElement>().preferredHeight = wideLayout ? 180f : 210f;
        VerticalLayoutGroup futureLayout = futurePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        futureLayout.padding = wideLayout ? new RectOffset(24, 24, 18, 18) : new RectOffset(26, 26, 22, 22);
        futureLayout.spacing = wideLayout ? 10f : 12f;
        CreateText("FutureTitle", futurePanel, T("settings.extensibility_title"), wideLayout ? 32 : 38, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateText("FutureBody", futurePanel, T("settings.extensibility_body"), wideLayout ? 24 : 28, FontStyle.Normal, ink, TextAnchor.MiddleLeft);

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
        CreateText("Title", header, title, wideLayout ? 34 : 40, FontStyle.Bold, pine, TextAnchor.MiddleLeft);
        CreateText("Subtitle", header, subtitle, wideLayout ? 23 : 27, FontStyle.Normal, ink, TextAnchor.MiddleLeft);
    }

    private void CreateResourceChip(RectTransform parent, string label, string value, Sprite icon, Color accent)
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
        layout.preferredHeight = vertical ? 64f : 86f;
        layout.flexibleWidth = 1f;

        Button button = rect.gameObject.AddComponent<Button>();
        rect.gameObject.AddComponent<UIButtonFeedback>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        ApplyButtonColor(button, active ? NavActiveColor(tab) : inactiveNav);

        VerticalLayoutGroup buttonLayout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonLayout.padding = vertical ? new RectOffset(7, 7, 5, 5) : new RectOffset(6, 6, 7, 7);
        buttonLayout.spacing = vertical ? 2f : 4f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        RectTransform iconFrame = CreatePanel("NavIconFrame", rect, active ? new Color(1f, 0.93f, 0.62f) : new Color(0.20f, 0.16f, 0.12f));
        LayoutElement iconLayout = iconFrame.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = vertical ? 38f : 48f;
        iconLayout.preferredHeight = vertical ? 30f : 42f;
        Sprite icon = TabIcon(tab);
        if (icon != null)
        {
            Image iconImage = CreateImage("NavIcon", iconFrame, icon, true);
            iconImage.color = active ? Color.white : new Color(1f, 0.92f, 0.72f);
        }

        Text text = CreateText("NavLabel", rect, label, vertical ? 21 : 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = vertical ? 13 : 16;
        text.resizeTextMaxSize = vertical ? 21 : 26;
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = vertical ? 24f : 30f;
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
                return coral;
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

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.08f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        colors.selectedColor = color;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.38f);
        button.colors = colors;
    }

    private RectTransform CreateProgress(RectTransform parent, float value, Color color)
    {
        RectTransform track = CreatePanel("Progress", parent, new Color(0.12f, 0.20f, 0.17f, 0.14f));
        track.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
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
               name == "ResourceChip" ||
               name == "ResourceIcon" ||
               name == "RoundShortcut" ||
               name == "ManagementPanel" ||
               name == "SheetTabs" ||
               name == "SheetTab" ||
               name == "HomeActionCard" ||
               name == "HomeCardIcon" ||
               name == "HomeCardButton" ||
               name == "ShopHero" ||
               name == "ShopFront" ||
               name == "FocusProduct" ||
               name == "FocusIcon" ||
               name == "GuidePanel" ||
               name == "GuideItem" ||
               name == "Day" ||
               name == "Product" ||
               name == "Upgrade" ||
               name == "Staff" ||
               name == "LanguagePanel" ||
               name == "FuturePanel" ||
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
