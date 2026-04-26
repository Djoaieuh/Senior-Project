using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A single trade offered by a shop.
/// Left side = what the PLAYER gives. Right side = what the PLAYER receives.
/// Examples:
///   Sell fish:   playerGives=[3x Carp]         playerReceives=[15 coins]
///   Buy item:    playerGives=[30 coins]         playerReceives=[1 Iron, 3 Coal]
///   Upgrade:     playerGives=[1 Great Rod, 50c] playerReceives=[1 Greater Rod]
/// </summary>
[System.Serializable]
public class TradeData
{
    [Header("Trade Identity")]
    [Tooltip("Short label shown in the UI for this trade (e.g. 'Sell Carp', 'Buy Iron Bundle')")]
    public string tradeName = "New Trade";

    [Tooltip("Optional longer description for flavour or clarity")]
    [TextArea(2, 3)]
    public string tradeDescription = "";

    // ── What the player gives ──────────────────────────────────────────────
    [Header("Player Gives (Left Side)")]
    [Tooltip("Money the player must pay. 0 = no money required.")]
    public int giveMoney = 0;

    [Tooltip("Items the player must hand over.")]
    public List<TradeItemEntry> giveItems = new List<TradeItemEntry>();

    // ── What the player receives ───────────────────────────────────────────
    [Header("Player Receives (Right Side)")]
    [Tooltip("Money the player receives. 0 = no money given.")]
    public int receiveMoney = 0;

    [Tooltip("Items the player receives.")]
    public List<TradeItemEntry> receiveItems = new List<TradeItemEntry>();

    // ── Unlock Conditions ──────────────────────────────────────────────────
    [Header("Unlock Conditions")]
    [Tooltip("Leave empty = always visible. Add conditions to hide/lock this trade.")]
    public TradeUnlockConditionGroup unlockConditions = new TradeUnlockConditionGroup();

    // ── Stock Limits ───────────────────────────────────────────────────────
    [Header("Stock Limits")]
    [Tooltip("Does this trade have a limited number of uses before it needs to restock?")]
    public bool hasStockLimit = false;

    [Tooltip("How many times this trade can be performed before it runs out.")]
    [Min(1)]
    public int stockLimit = 1;

    [Tooltip("How many real-time SECONDS until stock fully replenishes after first purchase.")]
    [Min(1)]
    public float restockCooldownSeconds = 3600f; // 1 hour default

    // ── Runtime ID ────────────────────────────────────────────────────────
    [Tooltip("Unique ID for this trade. Auto-generated from shop name + index. DO NOT edit manually.")]
    public string tradeID = "";

    /// <summary>
    /// Returns true if this trade requires no items AND no money on either side (misconfigured).
    /// </summary>
    public bool IsValid() =>
        (giveMoney > 0 || giveItems.Count > 0) &&
        (receiveMoney > 0 || receiveItems.Count > 0);
}

/// <summary>
/// One item slot in a trade (item reference + quantity).
/// </summary>
[System.Serializable]
public class TradeItemEntry
{
    [Tooltip("The item involved in this trade slot.")]
    public Item item;

    [Tooltip("How many of this item are required/given.")]
    [Min(1)]
    public int quantity = 1;
}