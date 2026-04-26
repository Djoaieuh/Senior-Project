using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════════════════════════════
// CONDITION GROUP  (supports AND / OR between individual conditions)
// ══════════════════════════════════════════════════════════════════════════════

public enum ConditionGroupLogic
{
    All, // Every condition must be met (AND)
    Any  // At least one condition must be met (OR)
}

/// <summary>
/// A group of unlock conditions for a Trade, combined with AND or OR logic.
/// </summary>
[System.Serializable]
public class TradeUnlockConditionGroup
{
    [Tooltip("ALL = every condition must be met (AND logic)\nANY = at least one must be met (OR logic)")]
    public ConditionGroupLogic logic = ConditionGroupLogic.All;

    [Tooltip("List of conditions. Leave empty = trade is always visible.")]
    public List<ShopUnlockCondition> conditions = new List<ShopUnlockCondition>();

    /// <summary>Returns true if the group is empty (no conditions = always unlocked).</summary>
    public bool IsAlwaysUnlocked() => conditions == null || conditions.Count == 0;

    public bool IsMet(ShopSaveData shopSave)
    {
        if (IsAlwaysUnlocked()) return true;

        if (logic == ConditionGroupLogic.All)
        {
            foreach (var c in conditions)
                if (!c.IsMet(shopSave)) return false;
            return true;
        }
        else // Any
        {
            foreach (var c in conditions)
                if (c.IsMet(shopSave)) return true;
            return false;
        }
    }
}

/// <summary>
/// Same as TradeUnlockConditionGroup but used on the Shop itself for map visibility.
/// </summary>
[System.Serializable]
public class ShopUnlockConditionGroup
{
    [Tooltip("ALL = every condition must be met (AND logic)\nANY = at least one must be met (OR logic)")]
    public ConditionGroupLogic logic = ConditionGroupLogic.All;

    [Tooltip("List of conditions. Leave empty = shop is always visible.")]
    public List<ShopUnlockCondition> conditions = new List<ShopUnlockCondition>();

    public bool IsAlwaysVisible() => conditions == null || conditions.Count == 0;

    public bool IsMet(ShopSaveData shopSave)
    {
        if (IsAlwaysVisible()) return true;

        if (logic == ConditionGroupLogic.All)
        {
            foreach (var c in conditions)
                if (!c.IsMet(shopSave)) return false;
            return true;
        }
        else
        {
            foreach (var c in conditions)
                if (c.IsMet(shopSave)) return true;
            return false;
        }
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// CONDITION TYPES
// ══════════════════════════════════════════════════════════════════════════════

public enum ShopConditionType
{
    [Tooltip("No condition — always met.")]
    None,

    [Tooltip("A specific map location must be unlocked.")]
    LocationUnlocked,

    [Tooltip("Player must own at least one of a specific item.")]
    HasItem,

    [Tooltip("Player must have performed at least N trades total in this shop.")]
    TotalTradesInShop,

    [Tooltip("Player must have performed a specific trade at least N times.")]
    SpecificTradeCount,

    [Tooltip("A named quest/story flag must be set to true.")]
    QuestFlag,

    [Tooltip("Player must have a minimum amount of money.")]
    HasMinimumMoney,
}

/// <summary>
/// A single unlock condition. Pick a type and fill in the relevant fields.
/// Unused fields for the chosen type are ignored.
/// </summary>
[System.Serializable]
public class ShopUnlockCondition
{
    [Tooltip("What kind of condition is this?")]
    public ShopConditionType type = ShopConditionType.None;

    // ── LocationUnlocked ──────────────────────────────────────────────────
    [Tooltip("LocationUnlocked: The locationID that must be unlocked on the map.")]
    public string requiredLocationID = "";

    // ── HasItem ───────────────────────────────────────────────────────────
    [Tooltip("HasItem: The item the player must own.")]
    public Item requiredItem;

    [Tooltip("HasItem: Minimum quantity required (default 1).")]
    [Min(1)]
    public int requiredItemQuantity = 1;

    // ── TotalTradesInShop ─────────────────────────────────────────────────
    [Tooltip("TotalTradesInShop: Minimum number of ANY trades done in this shop.")]
    [Min(1)]
    public int requiredTradeCount = 1;

    // ── SpecificTradeCount ────────────────────────────────────────────────
    [Tooltip("SpecificTradeCount: The tradeID of the specific trade to track.")]
    public string requiredTradeID = "";

    [Tooltip("SpecificTradeCount: How many times that specific trade must have been done.")]
    [Min(1)]
    public int requiredSpecificTradeCount = 1;

    // ── QuestFlag ─────────────────────────────────────────────────────────
    [Tooltip("QuestFlag: Location ID that owns this flag (leave blank for global flags).")]
    public string questFlagLocationID = "";

    [Tooltip("QuestFlag: The flag name that must be TRUE.")]
    public string questFlagName = "";

    // ── HasMinimumMoney ───────────────────────────────────────────────────
    [Tooltip("HasMinimumMoney: Player must have at least this many coins.")]
    [Min(0)]
    public int requiredMoney = 0;

    /// <summary>
    /// Evaluates this condition. shopSave may be null for shop-level visibility checks
    /// before any interaction has occurred.
    /// </summary>
    public bool IsMet(ShopSaveData shopSave)
    {
        switch (type)
        {
            case ShopConditionType.None:
                return true;

            case ShopConditionType.LocationUnlocked:
                if (string.IsNullOrEmpty(requiredLocationID)) return true;
                return GameManager._instance?.Map?.IsLocationUnlocked(requiredLocationID) ?? false;

            case ShopConditionType.HasItem:
                if (requiredItem == null) return true;
                return GameManager._instance?.Inventory?.HasItem(requiredItem.itemID, requiredItemQuantity) ?? false;

            case ShopConditionType.TotalTradesInShop:
                if (shopSave == null) return false;
                return shopSave.totalTradesDone >= requiredTradeCount;

            case ShopConditionType.SpecificTradeCount:
                if (shopSave == null || string.IsNullOrEmpty(requiredTradeID)) return false;
                shopSave.tradeCompletionCounts.TryGetValue(requiredTradeID, out int count);
                return count >= requiredSpecificTradeCount;

            case ShopConditionType.QuestFlag:
                if (string.IsNullOrEmpty(questFlagName)) return true;
                return GameManager._instance?.Map?.GetLocationFlag(questFlagLocationID, questFlagName, false) ?? false;

            case ShopConditionType.HasMinimumMoney:
                return GameManager._instance?.CanAfford(requiredMoney) ?? false;

            default:
                return false;
        }
    }

    /// <summary>Human-readable summary shown in the inspector and UI.</summary>
    public string GetDescription()
    {
        switch (type)
        {
            case ShopConditionType.None:                return "Always unlocked";
            case ShopConditionType.LocationUnlocked:    return $"Unlock area: {requiredLocationID}";
            case ShopConditionType.HasItem:             return $"Own {requiredItemQuantity}x {requiredItem?.itemName ?? "?"}";
            case ShopConditionType.TotalTradesInShop:   return $"Complete {requiredTradeCount} trades here";
            case ShopConditionType.SpecificTradeCount:  return $"Do trade '{requiredTradeID}' {requiredSpecificTradeCount}x";
            case ShopConditionType.QuestFlag:           return $"Quest: {questFlagName}";
            case ShopConditionType.HasMinimumMoney:     return $"Have {requiredMoney} coins";
            default:                                    return "Unknown condition";
        }
    }
}