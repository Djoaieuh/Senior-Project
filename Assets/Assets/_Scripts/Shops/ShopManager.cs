using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Pure C# runtime brain for a single shop session.
/// Instantiated by ShopRegistry when a shop is opened.
///
/// Stock cooldowns tick using GlobalTimer (active fishing time only),
/// not real-world wall-clock time.
/// </summary>
public class ShopManager
{
    // ── State ──────────────────────────────────────────────────────────────
    private readonly ShopData shopData;
    private ShopSaveData saveData;

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Fired after any trade executes — UI listens to refresh.</summary>
    public event Action OnShopStateChanged;

    // ══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ══════════════════════════════════════════════════════════════════════

    public ShopManager(ShopData data, ShopSaveData existingSave)
    {
        shopData = data;
        saveData = existingSave ?? new ShopSaveData { shopID = data.locationID };
        EnsureTradeStatesExist();
    }

    // ══════════════════════════════════════════════════════════════════════
    // PUBLIC QUERIES
    // ══════════════════════════════════════════════════════════════════════

    public IReadOnlyList<TradeData> AllTrades => shopData.trades;
    public ShopData ShopData => shopData;

    public List<TradeRuntimeState> GetAllTradeStates()
    {
        var result = new List<TradeRuntimeState>();
        foreach (var trade in shopData.trades)
            result.Add(EvaluateTrade(trade));
        return result;
    }

    public TradeRuntimeState EvaluateTrade(TradeData trade)
    {
        bool unlocked  = trade.unlockConditions.IsMet(saveData);
        bool canAfford = unlocked && PlayerCanAffordTrade(trade);
        bool inStock   = unlocked && IsInStock(trade);
        int  stockLeft = GetStockRemaining(trade);
        double restockSecsRemaining = GetRestockSecondsRemaining(trade);

        return new TradeRuntimeState
        {
            trade                  = trade,
            isUnlocked             = unlocked,
            canAfford              = canAfford,
            isInStock              = inStock,
            stockLeft              = stockLeft,
            restockSecondsRemaining = restockSecsRemaining,
            canExecute             = unlocked && canAfford && inStock
        };
    }

    // ══════════════════════════════════════════════════════════════════════
    // EXECUTE TRADE
    // ══════════════════════════════════════════════════════════════════════

    public TradeResult ExecuteTrade(TradeData trade)
    {
        if (!trade.unlockConditions.IsMet(saveData)) return TradeResult.Locked;
        if (!IsInStock(trade))                        return TradeResult.OutOfStock;
        if (!PlayerCanAffordTrade(trade))             return TradeResult.CannotAfford;

        // ── Deduct what the player gives ──────────────────────────────
        if (trade.giveMoney > 0)
            GameManager._instance.RemoveMoney(trade.giveMoney);

        foreach (var entry in trade.giveItems)
            GameManager._instance.Inventory.RemoveItem(entry.item.itemID, entry.quantity);

        // ── Give what the player receives ─────────────────────────────
        if (trade.receiveMoney > 0)
            GameManager._instance.AddMoney(trade.receiveMoney);

        foreach (var entry in trade.receiveItems)
            GiveItemToPlayer(entry);

        // ── Update stock ──────────────────────────────────────────────
        ConsumeStock(trade);

        // ── Update completion tracking ────────────────────────────────
        saveData.totalTradesDone++;
        saveData.tradeCompletionCounts.TryGetValue(trade.tradeID, out int prev);
        saveData.tradeCompletionCounts[trade.tradeID] = prev + 1;

        // ── Journal: record received items ────────────────────────────
        foreach (var entry in trade.receiveItems)
        {
            if (entry.item != null)
                GameManager._instance.Journal.RecordAcquisition(
                    entry.item, shopData.locationID, shopData.shopName,
                    AcquisitionMethod.Bought);
        }

        OnShopStateChanged?.Invoke();
        return TradeResult.Success;
    }

    // ══════════════════════════════════════════════════════════════════════
    // SAVE / LOAD
    // ══════════════════════════════════════════════════════════════════════

    public ShopSaveData GetSaveData() => saveData;

    // ══════════════════════════════════════════════════════════════════════
    // STOCK — uses GlobalTimer, not wall-clock
    // ══════════════════════════════════════════════════════════════════════

    private bool IsInStock(TradeData trade)
    {
        if (!trade.hasStockLimit) return true;

        TradeSaveData ts = GetOrCreateTradeState(trade);

        // Restock timer active? Check via GlobalTimer
        if (ts.restockStartSnapshot > 0.0 && GlobalTimer.Instance != null)
        {
            if (GlobalTimer.Instance.HasElapsed(ts.restockStartSnapshot, trade.restockCooldownSeconds))
            {
                // Restock!
                ts.stockRemaining      = trade.stockLimit;
                ts.restockStartSnapshot = 0.0;
            }
        }

        return ts.stockRemaining > 0;
    }

    private int GetStockRemaining(TradeData trade)
    {
        if (!trade.hasStockLimit) return -1;
        return GetOrCreateTradeState(trade).stockRemaining;
    }

    /// <summary>Returns remaining restock time in active-fishing seconds. 0 = done or N/A.</summary>
    private double GetRestockSecondsRemaining(TradeData trade)
    {
        if (!trade.hasStockLimit) return 0.0;

        TradeSaveData ts = GetOrCreateTradeState(trade);
        if (ts.restockStartSnapshot <= 0.0 || ts.stockRemaining > 0) return 0.0;
        if (GlobalTimer.Instance == null) return 0.0;

        return GlobalTimer.Instance.SecondsRemaining(ts.restockStartSnapshot, trade.restockCooldownSeconds);
    }

    private void ConsumeStock(TradeData trade)
    {
        if (!trade.hasStockLimit) return;

        TradeSaveData ts = GetOrCreateTradeState(trade);
        if (ts.stockRemaining <= 0) return;

        ts.stockRemaining--;

        // Start restock timer on first purchase (snapshot current fishing time)
        if (ts.restockStartSnapshot <= 0.0 && GlobalTimer.Instance != null)
            ts.restockStartSnapshot = GlobalTimer.Instance.ElapsedSeconds;
    }

    // ══════════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private bool PlayerCanAffordTrade(TradeData trade)
    {
        if (GameManager._instance.Money < trade.giveMoney) return false;

        foreach (var entry in trade.giveItems)
        {
            if (entry.item == null) continue;
            if (!GameManager._instance.Inventory.HasItem(entry.item.itemID, entry.quantity))
                return false;
        }
        return true;
    }

    private TradeSaveData GetOrCreateTradeState(TradeData trade)
    {
        if (!saveData.tradeStates.TryGetValue(trade.tradeID, out TradeSaveData ts))
        {
            ts = new TradeSaveData
            {
                tradeID             = trade.tradeID,
                stockRemaining      = trade.hasStockLimit ? trade.stockLimit : -1,
                restockStartSnapshot = 0.0
            };
            saveData.tradeStates[trade.tradeID] = ts;
        }
        return ts;
    }

    private void EnsureTradeStatesExist()
    {
        foreach (var trade in shopData.trades)
        {
            if (string.IsNullOrEmpty(trade.tradeID))
                trade.tradeID = $"{shopData.locationID}_{trade.tradeName}".Replace(" ", "_").ToLower();

            if (!saveData.tradeStates.ContainsKey(trade.tradeID))
            {
                saveData.tradeStates[trade.tradeID] = new TradeSaveData
                {
                    tradeID             = trade.tradeID,
                    stockRemaining      = trade.hasStockLimit ? trade.stockLimit : -1,
                    restockStartSnapshot = 0.0
                };
            }
        }
    }

    private void GiveItemToPlayer(TradeItemEntry entry)
    {
        if (entry.item == null) return;
        if (entry.item is CatchableItem catchable)
            GameManager._instance.Inventory.AddCaughtItem(catchable, entry.quantity);
        // Extend here for other item types as needed
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// RESULT / STATE TYPES
// ══════════════════════════════════════════════════════════════════════════════

public enum TradeResult
{
    Success,
    CannotAfford,
    OutOfStock,
    Locked
}

/// <summary>
/// Full evaluated state of a trade at a moment in time.
/// restockSecondsRemaining is in active-fishing seconds (GlobalTimer).
/// </summary>
public struct TradeRuntimeState
{
    public TradeData trade;
    public bool      isUnlocked;
    public bool      canAfford;
    public bool      isInStock;
    public int       stockLeft;                  // -1 = unlimited
    public double    restockSecondsRemaining;    // active-fishing seconds until restock
    public bool      canExecute;
}