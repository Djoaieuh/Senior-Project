using System;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════════════════════════════
// SHOP SAVE DATA  — lives inside the main SaveData, serialised via Newtonsoft
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Everything about a single shop that must survive a save/load cycle.
/// </summary>
[Serializable]
public class ShopSaveData
{
    /// <summary>Matches ShopData.locationID — used as the key in the save dictionary.</summary>
    public string shopID;

    /// <summary>Total number of any trade completed in this shop.</summary>
    public int totalTradesDone = 0;

    /// <summary>Per-trade completion counts. Key = TradeData.tradeID.</summary>
    public Dictionary<string, int> tradeCompletionCounts = new Dictionary<string, int>();

    /// <summary>Stock state per trade. Key = TradeData.tradeID.</summary>
    public Dictionary<string, TradeSaveData> tradeStates = new Dictionary<string, TradeSaveData>();
}

/// <summary>
/// Persisted state for a single trade (stock + restock timer).
/// The restock cooldown uses GlobalTimer seconds — not real-world time —
/// so it only counts down while the player is actively fishing.
/// </summary>
[Serializable]
public class TradeSaveData
{
    public string tradeID;

    /// <summary>How many uses remain in the current stock cycle. -1 = unlimited.</summary>
    public int stockRemaining = -1;

    /// <summary>
    /// GlobalTimer.ElapsedSeconds snapshot taken when the restock timer started.
    /// 0 = timer not active (stock is full or trade has no limit).
    /// </summary>
    public double restockStartSnapshot = 0.0;
}