using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Drives the shop overlay UI. Lives on the shop canvas root.
/// AUTO-FINDS child elements by name — a designer just needs to name things correctly.
///
/// NAMING CONVENTIONS:
///   ShopNameText       — TextMeshProUGUI — shop title
///   ShopDescText       — TextMeshProUGUI — shop subtitle / description
///   ShopIcon           — Image           — shop portrait icon
///   PlayerMoneyText    — TextMeshProUGUI — displays player's coin balance
///   TradeListContainer — Transform       — scrollable list parent for trade rows
///   CloseButton        — Button          — closes the shop
///   ConfirmPanel       — GameObject      — confirmation popup (child of this canvas)
/// </summary>
public class ShopUI : MonoBehaviour
{
    // ── Auto-found references ──────────────────────────────────────────────
    [Header("Auto-Found (Name children correctly — see summary above)")]
    [SerializeField] private TextMeshProUGUI shopNameText;
    [SerializeField] private TextMeshProUGUI shopDescText;
    [SerializeField] private Image           shopIcon;
    [SerializeField] private TextMeshProUGUI playerMoneyText;
    [SerializeField] private Transform       tradeListContainer;
    [SerializeField] private Button          closeButton;
    [SerializeField] private GameObject      confirmPanel;

    [Header("Prefabs")]
    [Tooltip("The row prefab spawned for each trade. Must have a TradeRowUI component.")]
    [SerializeField] private GameObject tradeRowPrefab;

    [Header("Debug")]
    [SerializeField] private bool showAutoFindLogs = true;

    // ── Runtime ────────────────────────────────────────────────────────────
    private ShopManager activeManager;
    private readonly List<TradeRowUI> spawnedRows = new List<TradeRowUI>();
    private TradeRuntimeState pendingTrade;

    // ══════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        AutoFindReferences();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ShopRegistry.OnShopOpened += HandleShopOpened;
        ShopRegistry.OnShopClosed += HandleShopClosed;
    }

    private void OnDisable()
    {
        ShopRegistry.OnShopOpened -= HandleShopOpened;
        ShopRegistry.OnShopClosed -= HandleShopClosed;
    }

    // ══════════════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ══════════════════════════════════════════════════════════════════════

    private void HandleShopOpened(ShopManager manager)
    {
        activeManager = manager;
        activeManager.OnShopStateChanged += RefreshTrades;
        gameObject.SetActive(true);
        PopulateShopHeader();
        RefreshTrades();
        RefreshMoney();
    }

    private void HandleShopClosed()
    {
        if (activeManager != null)
            activeManager.OnShopStateChanged -= RefreshTrades;
        activeManager = null;
        gameObject.SetActive(false);
        ClearRows();
    }

    // ══════════════════════════════════════════════════════════════════════
    // POPULATE
    // ══════════════════════════════════════════════════════════════════════

    private void PopulateShopHeader()
    {
        ShopData data = activeManager.ShopData;
        if (shopNameText != null) shopNameText.text = data.shopName;
        if (shopDescText  != null) shopDescText.text  = data.shopDescription;
        if (shopIcon      != null)
        {
            shopIcon.sprite = data.shopIcon;
            shopIcon.gameObject.SetActive(data.shopIcon != null);
        }
    }

    private void RefreshTrades()
    {
        ClearRows();
        if (tradeListContainer == null || tradeRowPrefab == null) return;

        List<TradeRuntimeState> states = activeManager.GetAllTradeStates();
        foreach (TradeRuntimeState state in states)
        {
            // Skip locked trades that have no conditions shown — fully hidden
            if (!state.isUnlocked && state.trade.unlockConditions.IsAlwaysUnlocked()) continue;

            GameObject rowObj = Instantiate(tradeRowPrefab, tradeListContainer);
            TradeRowUI row    = rowObj.GetComponent<TradeRowUI>();

            if (row != null)
            {
                row.Setup(state, OnTradeRowClicked);
                spawnedRows.Add(row);
            }
        }
    }

    private void RefreshMoney()
    {
        if (playerMoneyText != null)
            playerMoneyText.text = $"{GameManager._instance.Money}g";
    }

    // ══════════════════════════════════════════════════════════════════════
    // CONFIRM PANEL
    // ══════════════════════════════════════════════════════════════════════

    private void OnTradeRowClicked(TradeRuntimeState state)
    {
        if (!state.canExecute) return;
        pendingTrade = state;
        ShowConfirmPanel(state);
    }

    private void ShowConfirmPanel(TradeRuntimeState state)
    {
        if (confirmPanel == null) return;

        ShopConfirmPanel panel = confirmPanel.GetComponent<ShopConfirmPanel>();
        if (panel != null)
            panel.Show(state, OnConfirmTrade, OnCancelTrade);
        else
            confirmPanel.SetActive(true);
    }

    private void OnConfirmTrade()
    {
        if (activeManager == null) return;

        TradeResult result = activeManager.ExecuteTrade(pendingTrade.trade);

        if (result != TradeResult.Success)
            Debug.LogWarning($"[ShopUI] Trade failed: {result}");

        RefreshMoney();
        HideConfirmPanel();
    }

    private void OnCancelTrade()
    {
        HideConfirmPanel();
    }

    private void HideConfirmPanel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    // CLOSE BUTTON
    // ══════════════════════════════════════════════════════════════════════

    public void OnCloseButtonClicked()
    {
        ShopRegistry._instance?.CloseShop();
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row.gameObject);
        spawnedRows.Clear();
    }

    // ══════════════════════════════════════════════════════════════════════
    // AUTO-FIND
    // ══════════════════════════════════════════════════════════════════════

    private void AutoFindReferences()
    {
        AutoFindTMP(ref shopNameText,     "ShopNameText");
        AutoFindTMP(ref shopDescText,     "ShopDescText");
        AutoFindImg(ref shopIcon,         "ShopIcon");
        AutoFindTMP(ref playerMoneyText,  "PlayerMoneyText");
        AutoFindTransform(ref tradeListContainer, "TradeListContainer");
        AutoFindButton(ref closeButton,   "CloseButton");
        AutoFindGO(ref confirmPanel,      "ConfirmPanel");

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (tradeRowPrefab == null)
        {
            tradeRowPrefab = Resources.Load<GameObject>("Prefabs/TradeRow");
            if (showAutoFindLogs && tradeRowPrefab != null)
                Debug.Log("[ShopUI] Auto-loaded TradeRow prefab from Resources/Prefabs/TradeRow");
            else if (tradeRowPrefab == null)
                Debug.LogWarning("[ShopUI] Could not find TradeRow prefab. Assign it in the inspector or place it at Resources/Prefabs/TradeRow");
        }
    }

    private void AutoFindTMP(ref TextMeshProUGUI field, string childName)
    {
        if (field != null) return;
        Transform t = FindDeep(transform, childName);
        if (t != null) field = t.GetComponent<TextMeshProUGUI>();
        if (showAutoFindLogs && field != null) Debug.Log($"[ShopUI] Auto-found {childName}");
        else if (field == null) Debug.LogWarning($"[ShopUI] Could not find '{childName}' (TextMeshProUGUI)");
    }

    private void AutoFindImg(ref Image field, string childName)
    {
        if (field != null) return;
        Transform t = FindDeep(transform, childName);
        if (t != null) field = t.GetComponent<Image>();
        if (showAutoFindLogs && field != null) Debug.Log($"[ShopUI] Auto-found {childName}");
    }

    private void AutoFindButton(ref Button field, string childName)
    {
        if (field != null) return;
        Transform t = FindDeep(transform, childName);
        if (t != null) field = t.GetComponent<Button>();
        if (showAutoFindLogs && field != null) Debug.Log($"[ShopUI] Auto-found {childName}");
    }

    private void AutoFindTransform(ref Transform field, string childName)
    {
        if (field != null) return;
        field = FindDeep(transform, childName);
        if (showAutoFindLogs && field != null) Debug.Log($"[ShopUI] Auto-found {childName}");
        else if (field == null) Debug.LogWarning($"[ShopUI] Could not find '{childName}' (Transform)");
    }

    private void AutoFindGO(ref GameObject field, string childName)
    {
        if (field != null) return;
        Transform t = FindDeep(transform, childName);
        if (t != null) field = t.gameObject;
        if (showAutoFindLogs && field != null)
        {
            Debug.Log($"[ShopUI] Auto-found {childName}");
            field.SetActive(false);
        }
    }

    /// <summary>Recursive deep search for a child by name.</summary>
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