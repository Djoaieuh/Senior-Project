using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Controls one trade row in the shop UI.
/// Prefab: TradeRowPrefab
///
/// Layout (left → right):
///   [GiveSlotsContainer]  [ArrowImage]  [ReceiveSlotsContainer]  [StockText]  [TradeButton]
/// </summary>
public class TradeRowUI : MonoBehaviour
{
    [Header("Give Side")]
    [Tooltip("Horizontal Layout Group — ItemSlot prefabs are spawned here")]
    public Transform giveSlotsContainer;

    [Header("Receive Side")]
    [Tooltip("Horizontal Layout Group — ItemSlot prefabs are spawned here")]
    public Transform receiveSlotsContainer;

    [Header("Trade Button")]
    public Button tradeButton;
    public TextMeshProUGUI tradeButtonText;

    // ── Set by ShopUI ──────────────────────────────────────────────────────
    private TradeData tradeData;
    private Action<TradeData> onTradeClicked;

    // ══════════════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by ShopUI for every visible trade on the current page.
    /// </summary>
    /// <param name="state">Evaluated runtime state of this trade.</param>
    /// <param name="coinSprite">Sprite to use for money slots.</param>
    /// <param name="itemSlotPrefab">Prefab to instantiate for each slot.</param>
    /// <param name="callback">Invoked when the player clicks Trade.</param>
    public void Setup(TradeRuntimeState state, Sprite coinSprite, GameObject itemSlotPrefab, Action<TradeData> callback)
    {
        tradeData      = state.trade;
        onTradeClicked = callback;

        BuildSlots(state.trade, coinSprite, itemSlotPrefab);
        SetupButton(state);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private void BuildSlots(TradeData trade, Sprite coinSprite, GameObject slotPrefab)
    {
        ClearContainer(giveSlotsContainer);
        ClearContainer(receiveSlotsContainer);

        // ── Give side ──────────────────────────────────────────────────
        if (trade.giveMoney > 0)
            SpawnSlot(giveSlotsContainer, coinSprite, trade.giveMoney.ToString(), slotPrefab);

        foreach (var entry in trade.giveItems)
        {
            if (entry.item == null) continue;
            Sprite icon = MasterItemDatabase.Instance != null
                ? MasterItemDatabase.Instance.GetIcon(entry.item.itemID)
                : null;
            SpawnSlot(giveSlotsContainer, icon, $"x{entry.quantity}", slotPrefab);
        }

        // ── Receive side ───────────────────────────────────────────────
        if (trade.receiveMoney > 0)
            SpawnSlot(receiveSlotsContainer, coinSprite, trade.receiveMoney.ToString(), slotPrefab);

        foreach (var entry in trade.receiveItems)
        {
            if (entry.item == null) continue;
            Sprite icon = MasterItemDatabase.Instance != null
                ? MasterItemDatabase.Instance.GetIcon(entry.item.itemID)
                : null;
            SpawnSlot(receiveSlotsContainer, icon, $"x{entry.quantity}", slotPrefab);
        }
    }

    private void SetupButton(TradeRuntimeState state)
    {
        tradeButton.interactable = state.canExecute;
        tradeButton.onClick.RemoveAllListeners();
        tradeButton.onClick.AddListener(() => onTradeClicked?.Invoke(tradeData));

        if (!state.isUnlocked)
            tradeButtonText.text = "Locked";
        else if (!state.isInStock)
            tradeButtonText.text = "Sold Out";
        else if (!state.canAfford)
            tradeButtonText.text = "Can't Afford";
        else
            tradeButtonText.text = "Trade";
    }

    private void SpawnSlot(Transform container, Sprite sprite, string label, GameObject slotPrefab)
    {
        GameObject go  = Instantiate(slotPrefab, container);
        ItemSlotUI slot = go.GetComponent<ItemSlotUI>();
        if (slot != null)
            slot.Setup(sprite, label);
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }
}