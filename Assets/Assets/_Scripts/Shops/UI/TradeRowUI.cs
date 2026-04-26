using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Drives a single trade row in the shop list.
///
/// NAMING CONVENTIONS for child objects:
///   TradeNameText      — TextMeshProUGUI — trade label
///   GiveSection        — Transform       — parent for "give" item icons + money
///   ReceiveSection     — Transform       — parent for "receive" item icons + money
///   TradeButton        — Button          — click to initiate trade
///   TradeButtonText    — TextMeshProUGUI — button label text
///   StockText          — TextMeshProUGUI — shows remaining stock / restock timer
///   LockedOverlay      — GameObject      — shown when trade is locked
///   ConditionHintText  — TextMeshProUGUI — shows lock reason
///   ItemIconPrefab     — auto-loaded from Resources/Prefabs/TradeItemIcon
/// </summary>
public class TradeRowUI : MonoBehaviour
{
    [Header("Auto-Found Children (name them correctly)")]
    [SerializeField] private TextMeshProUGUI tradeNameText;
    [SerializeField] private Transform       giveSection;
    [SerializeField] private Transform       receiveSection;
    [SerializeField] private Button          tradeButton;
    [SerializeField] private TextMeshProUGUI tradeButtonText;
    [SerializeField] private TextMeshProUGUI stockText;
    [SerializeField] private GameObject      lockedOverlay;
    [SerializeField] private TextMeshProUGUI conditionHintText;

    [Header("Prefabs (auto-loaded if not assigned)")]
    [SerializeField] private GameObject itemIconPrefab;

    [Header("Colors")]
    [SerializeField] private Color canTradeColor   = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color cannotTradeColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color outOfStockColor  = new Color(0.9f, 0.5f, 0.1f);

    [Header("Debug")]
    [SerializeField] private bool showAutoFindLogs = false;

    // ── Runtime ────────────────────────────────────────────────────────────
    private TradeRuntimeState currentState;
    private Action<TradeRuntimeState> onClickCallback;
    private readonly List<GameObject> spawnedIcons = new List<GameObject>();

    // ══════════════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        AutoFindReferences();
    }

    /// <summary>
    /// Called by ShopUI to configure this row.
    /// </summary>
    public void Setup(TradeRuntimeState state, Action<TradeRuntimeState> onClick)
    {
        currentState    = state;
        onClickCallback = onClick;
        Refresh();
    }

    public void Refresh()
    {
        TradeData trade = currentState.trade;

        // ── Name ──────────────────────────────────────────────────────
        if (tradeNameText != null)
            tradeNameText.text = trade.tradeName;

        // ── Give / Receive sections ───────────────────────────────────
        PopulateSection(giveSection,    trade.giveItems,    trade.giveMoney);
        PopulateSection(receiveSection, trade.receiveItems, trade.receiveMoney);

        // ── Lock state ────────────────────────────────────────────────
        bool locked = !currentState.isUnlocked;
        if (lockedOverlay != null)
            lockedOverlay.SetActive(locked);

        if (conditionHintText != null)
        {
            if (locked)
            {
                conditionHintText.gameObject.SetActive(true);
                conditionHintText.text = BuildConditionHint(trade);
            }
            else
            {
                conditionHintText.gameObject.SetActive(false);
            }
        }

        // ── Stock text ────────────────────────────────────────────────
        if (stockText != null)
        {
            if (!trade.hasStockLimit)
            {
                stockText.gameObject.SetActive(false);
            }
            else
            {
                stockText.gameObject.SetActive(true);
                string timer = FormatFishingTime(currentState.restockSecondsRemaining);
                stockText.text = $"Restocks in {timer} (fishing time)";
            }
        }

        // ── Button ────────────────────────────────────────────────────
        if (tradeButton != null)
        {
            tradeButton.interactable = currentState.canExecute;

            ColorBlock cb = tradeButton.colors;
            if (!currentState.isInStock)
                cb.normalColor = outOfStockColor;
            else if (currentState.canExecute)
                cb.normalColor = canTradeColor;
            else
                cb.normalColor = cannotTradeColor;
            tradeButton.colors = cb;
        }

        if (tradeButtonText != null)
        {
            if (!currentState.isUnlocked)         tradeButtonText.text = "Locked";
            else if (!currentState.isInStock)     tradeButtonText.text = "Out of Stock";
            else if (!currentState.canAfford)     tradeButtonText.text = "Can't Afford";
            else                                  tradeButtonText.text = "Trade";
        }
    }
    
    private string FormatFishingTime(double seconds)
    {
        System.TimeSpan t = System.TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
        if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m {t.Seconds}s";
        return $"{(int)t.TotalSeconds}s";
    }

    // ══════════════════════════════════════════════════════════════════════
    // BUTTON CLICK
    // ══════════════════════════════════════════════════════════════════════

    public void OnTradeButtonClicked()
    {
        onClickCallback?.Invoke(currentState);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SECTION POPULATE
    // ══════════════════════════════════════════════════════════════════════

    private void PopulateSection(Transform section, List<TradeItemEntry> items, int money)
    {
        if (section == null) return;

        // Clear old icons
        foreach (var go in spawnedIcons)
            if (go != null) Destroy(go);
        spawnedIcons.Clear();

        // Money entry
        if (money > 0)
            SpawnMoneyIcon(section, money);

        // Item entries
        foreach (var entry in items)
            SpawnItemIcon(section, entry);
    }

    private void SpawnItemIcon(Transform parent, TradeItemEntry entry)
    {
        if (entry.item == null) return;
        if (itemIconPrefab == null) return;

        GameObject icon = Instantiate(itemIconPrefab, parent);
        spawnedIcons.Add(icon);

        TradeItemIconUI iconUI = icon.GetComponent<TradeItemIconUI>();
        if (iconUI != null)
            iconUI.Setup(entry.item.icon, entry.item.itemName, entry.quantity);
    }

    private void SpawnMoneyIcon(Transform parent, int amount)
    {
        if (itemIconPrefab == null) return;

        GameObject icon = Instantiate(itemIconPrefab, parent);
        spawnedIcons.Add(icon);

        TradeItemIconUI iconUI = icon.GetComponent<TradeItemIconUI>();
        if (iconUI != null)
            iconUI.SetupMoney(amount);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private string BuildConditionHint(TradeData trade)
    {
        var hints = new System.Text.StringBuilder();
        string logicLabel = trade.unlockConditions.logic == ConditionGroupLogic.All ? "AND" : "OR";

        List<ShopUnlockCondition> conditions = trade.unlockConditions.conditions;
        for (int i = 0; i < conditions.Count; i++)
        {
            hints.Append(conditions[i].GetDescription());
            if (i < conditions.Count - 1)
                hints.Append($" {logicLabel} ");
        }
        return hints.ToString();
    }

    private string FormatTime(System.TimeSpan span)
    {
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes}m {span.Seconds}s";
        return $"{span.Seconds}s";
    }

    // ══════════════════════════════════════════════════════════════════════
    // AUTO-FIND
    // ══════════════════════════════════════════════════════════════════════

    private void AutoFindReferences()
    {
        AutoFindTMP(ref tradeNameText,    "TradeNameText");
        AutoFindTransform(ref giveSection,    "GiveSection");
        AutoFindTransform(ref receiveSection, "ReceiveSection");
        AutoFindButton(ref tradeButton,   "TradeButton");
        AutoFindTMP(ref tradeButtonText,  "TradeButtonText");
        AutoFindTMP(ref stockText,        "StockText");
        AutoFindGO(ref lockedOverlay,     "LockedOverlay");
        AutoFindTMP(ref conditionHintText,"ConditionHintText");

        if (tradeButton != null)
            tradeButton.onClick.AddListener(OnTradeButtonClicked);

        if (itemIconPrefab == null)
        {
            itemIconPrefab = Resources.Load<GameObject>("Prefabs/TradeItemIcon");
            if (showAutoFindLogs && itemIconPrefab != null)
                Debug.Log("[TradeRowUI] Auto-loaded TradeItemIcon prefab");
        }
    }

    private void AutoFindTMP(ref TextMeshProUGUI f, string n)
    {
        if (f != null) return;
        Transform t = FindDeep(transform, n);
        if (t != null) f = t.GetComponent<TextMeshProUGUI>();
    }

    private void AutoFindTransform(ref Transform f, string n)
    {
        if (f != null) return;
        f = FindDeep(transform, n);
    }

    private void AutoFindButton(ref Button f, string n)
    {
        if (f != null) return;
        Transform t = FindDeep(transform, n);
        if (t != null) f = t.GetComponent<Button>();
    }

    private void AutoFindGO(ref GameObject f, string n)
    {
        if (f != null) return;
        Transform t = FindDeep(transform, n);
        if (t != null) { f = t.gameObject; f.SetActive(false); }
    }

    private Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}