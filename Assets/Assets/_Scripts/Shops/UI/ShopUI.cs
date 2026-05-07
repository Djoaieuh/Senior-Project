using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Main shop panel controller. Attach directly to ShopPanel.
/// ShopPanel must stay active in the scene at all times —
/// visibility is handled via CanvasGroup (alpha/interactable), not SetActive.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Shop Header")]
    public TextMeshProUGUI shopNameText;

    [Header("Trade Rows")]
    [Tooltip("Parent transform for spawned TradeRow prefabs (Vertical Layout Group)")]
    public Transform tradeRowContainer;
    public GameObject tradeRowPrefab;
    public GameObject itemSlotPrefab;
    public Sprite coinSprite;

    [Header("Pagination")]
    public Button prevPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageText;

    // ── Constants ──────────────────────────────────────────────────────────
    private const int TradesPerPage = 6;

    // ── Runtime ────────────────────────────────────────────────────────────
    private CanvasGroup canvasGroup;
    private ShopManager currentShop;
    private List<TradeRuntimeState> allStates = new List<TradeRuntimeState>();
    private int currentPage = 0;

    // ══════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Hide();

        ShopRegistry.OnShopOpened += HandleShopOpened;
        ShopRegistry.OnShopClosed += HandleShopClosed;
    }

    private void OnDestroy()
    {
        ShopRegistry.OnShopOpened -= HandleShopOpened;
        ShopRegistry.OnShopClosed -= HandleShopClosed;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SHOW / HIDE  — CanvasGroup instead of SetActive so Awake always runs
    // ══════════════════════════════════════════════════════════════════════

    private void Show()
    {
        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SHOP EVENTS
    // ══════════════════════════════════════════════════════════════════════

    private void HandleShopOpened(ShopManager manager)
    {
        currentShop = manager;
        currentShop.OnShopStateChanged += Refresh;

        currentPage = 0;
        shopNameText.text = manager.ShopData.shopName;
        Show();
        Refresh();
    }

    private void HandleShopClosed()
    {
        if (currentShop != null)
            currentShop.OnShopStateChanged -= Refresh;

        currentShop = null;
        Hide();
    }

    // ══════════════════════════════════════════════════════════════════════
    // REFRESH
    // ══════════════════════════════════════════════════════════════════════

    private void Refresh()
    {
        if (currentShop == null) return;

        allStates = currentShop.GetAllTradeStates();

        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allStates.Count / TradesPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        prevPageButton.interactable = currentPage > 0;
        nextPageButton.interactable = currentPage < totalPages - 1;
        pageText.text = totalPages > 1 ? $"{currentPage + 1} / {totalPages}" : "";

        foreach (Transform child in tradeRowContainer)
            Destroy(child.gameObject);

        int start = currentPage * TradesPerPage;
        int end   = Mathf.Min(start + TradesPerPage, allStates.Count);

        for (int i = start; i < end; i++)
        {
            GameObject rowGO = Instantiate(tradeRowPrefab, tradeRowContainer);
            rowGO.GetComponent<TradeRowUI>().Setup(allStates[i], coinSprite, itemSlotPrefab, OnTradeClicked);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // BUTTON CALLBACKS
    // ══════════════════════════════════════════════════════════════════════

    private void OnTradeClicked(TradeData trade)
    {
        currentShop?.ExecuteTrade(trade);
    }

    public void OnPrevPage()    { currentPage--; Refresh(); }
    public void OnNextPage()    { currentPage++; Refresh(); }
    public void OnCloseButton() => ShopRegistry._instance?.CloseShop();
}