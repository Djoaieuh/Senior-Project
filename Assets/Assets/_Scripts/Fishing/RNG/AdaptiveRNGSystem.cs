using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Adaptive RNG System with pity mechanics and luck scaling
/// Ensures fair distribution over sessions while preventing frustration
/// </summary>
public class AdaptiveRNGSystem : MonoBehaviour
{
    [Header("Luck Scaling Configuration")]
    [Tooltip("Luck keyframes - defines how rarity odds change with luck stat")]
    [SerializeField] private LuckKeyframe[] luckKeyframes = new LuckKeyframe[]
    {
        new LuckKeyframe(0f,    new float[] { 60.0f, 30.0f, 7.5f, 2.0f, 0.5f }),    // 0 Luck
        new LuckKeyframe(250f,  new float[] { 36.3f, 50.0f, 10.0f, 3.0f, 0.7f }),   // 250 Luck
        new LuckKeyframe(750f,  new float[] { 20.0f, 28.0f, 40.0f, 10.0f, 2.0f }),  // 750 Luck
        new LuckKeyframe(1500f, new float[] { 20.0f, 20.0f, 25.0f, 30.0f, 5.0f }),  // 1500 Luck
        new LuckKeyframe(3000f, new float[] { 20.0f, 20.0f, 20.0f, 20.0f, 20.0f })  // 3000 Luck
    };
    
    [Header("Pity System Configuration")]
    [Tooltip("Maximum boost multiplier (default 5x)")]
    [Range(1f, 10f)]
    [SerializeField] private float maxBoostMultiplier = 5f;
    
    [Tooltip("When to start anticipation boosting (0.9 = 90% of target)")]
    [Range(0f, 1f)]
    [SerializeField] private float anticipationStartRatio = 0.9f;
    
    [Header("Item Boost Settings")]
    [Tooltip("Item boost rate BEFORE hitting target (gentle)")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float itemPreTargetBoostRate = 0.05f;
    
    [Tooltip("Item boost rate AFTER hitting target (aggressive)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float itemPostTargetBoostRate = 0.25f;
    
    [Header("Rarity Boost Settings")]
    [Tooltip("Rarity boost rate BEFORE hitting target (gentle)")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float rarityPreTargetBoostRate = 0.03f;
    
    [Tooltip("Rarity boost rate AFTER hitting target (aggressive)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float rarityPostTargetBoostRate = 0.2f;
    
    [Header("Item Type Boost Settings")]
    [Tooltip("Item Type boost rate BEFORE hitting target (gentle)")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float itemTypePreTargetBoostRate = 0.04f;
    
    [Tooltip("Item Type boost rate AFTER hitting target (aggressive)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float itemTypePostTargetBoostRate = 0.15f;
    
    [Header("Debug")]
    [SerializeField] public bool showDebugLogs = true;
    [SerializeField] private bool enablePitySystem = true;
    
    public bool IsPitySystemEnabled => enablePitySystem;
    
    // Fishing table state
    private FishingTable currentFishingTable;
    private Dictionary<string, CatchableItem> itemLookup = new Dictionary<string, CatchableItem>();
    
    // Tracking systems
    private int globalRollCounter = 0;
    private Dictionary<ItemRarity, RarityTracker> rarityTrackers = new Dictionary<ItemRarity, RarityTracker>();
    private Dictionary<ItemType, ItemTypeTracker> itemTypeTrackers = new Dictionary<ItemType, ItemTypeTracker>();
    private Dictionary<string, ItemStats> itemStatsTracker = new Dictionary<string, ItemStats>();
    
    private void OnEnable()
    {
        FishingEvents.OnEquippedGearChanged += HandleGearChanged;
    }
    
    private void OnDisable()
    {
        FishingEvents.OnEquippedGearChanged -= HandleGearChanged;
    }
    
    /// <summary>
    /// Handle gear changed - rebuild fishing table
    /// </summary>
    private void HandleGearChanged()
    {
        if (showDebugLogs)
            Debug.Log("[AdaptiveRNG] Gear changed - rebuilding fishing table");
        
        FishingManager fishingManager = GetComponent<FishingManager>();
        if (fishingManager != null)
        {
            FishingPool pool = fishingManager.GetFishingPool();
            EquippedGearInventory gear = GameManager._instance.EquippedGear;
            
            if (pool != null && gear != null)
            {
                BuildFishingTable(pool, gear);
            }
        }
    }
    
    /// <summary>
    /// Build fishing table from pool and gear
    /// </summary>
    public void BuildFishingTable(FishingPool pool, EquippedGearInventory gear)
    {
        if (pool == null)
        {
            Debug.LogError("[AdaptiveRNG] Cannot build fishing table - pool is null!");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log("[AdaptiveRNG] Building fishing table...");
        
        // Reset all state
        globalRollCounter = 0;
        rarityTrackers.Clear();
        itemTypeTrackers.Clear();
        itemStatsTracker.Clear();
        itemLookup.Clear();
        
        // Create new fishing table
        currentFishingTable = new FishingTable();
        
        // Step 1: Get rarity weights based on luck
        float totalLuck = gear != null ? gear.GetTotalLuck() : 0f;
        Dictionary<ItemRarity, float> rarityWeights = CalculateRarityWeights(totalLuck);
        
        // Step 2: Get all catchable items from pool
        List<CatchableItem> allItems = pool.GetAllItems();
        
        // Step 3: Filter by bait
        BaitType currentBait = gear != null ? gear.CurrentBaitType : BaitType.None;
        List<CatchableItem> filteredItems = FilterByBait(allItems, currentBait);
        
        // Step 4: Build fishing table entries
        foreach (CatchableItem item in filteredItems)
        {
            FishingTableEntry entry = CreateTableEntry(item, rarityWeights);
            currentFishingTable.AddEntry(entry);
            itemLookup[item.itemID] = item;
        }
        
        // Step 5: Discover and initialize rarity trackers
        InitializeRarityTrackers(rarityWeights);
        
        // Step 6: Discover and initialize item type trackers
        InitializeItemTypeTrackers();
        
        // Step 7: Calculate total combined weight
        currentFishingTable.CalculateTotalWeight();
        
        // Step 8: Calculate expected frequencies for rarities
        CalculateRarityFrequencies();
        
        // Step 9: Calculate expected frequencies for item types
        CalculateItemTypeFrequencies();
        
        // Step 10: Calculate targets for items (within their rarity)
        CalculateItemTargets();
        
        // Step 11: Initialize item stats tracking
        foreach (var entry in currentFishingTable.entries)
        {
            itemStatsTracker[entry.itemID] = new ItemStats
            {
                itemName = entry.itemName,
                belongsToRarity = entry.rarity,
                baseWeight = entry.baseWeight,
                currentItemBoost = 1f,
                targetRollsWithinRarity = entry.targetRollsWithinRarity,
                catchCount = 0
            };
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[AdaptiveRNG] Fishing table built!");
            Debug.Log($"  Total Luck: {totalLuck}");
            Debug.Log($"  Items: {currentFishingTable.entries.Count}");
            Debug.Log($"  Rarities: {rarityTrackers.Count}");
            Debug.Log($"  Item Types: {itemTypeTrackers.Count}");
            Debug.Log($"  Total combined weight: {currentFishingTable.totalCombinedWeight:F4}");
        }
    }
    
    /// <summary>
    /// Initialize rarity trackers for all present rarities
    /// </summary>
    private void InitializeRarityTrackers(Dictionary<ItemRarity, float> rarityWeights)
    {
        var uniqueRarities = currentFishingTable.entries
            .Select(e => e.rarity)
            .Distinct();
        
        foreach (var rarity in uniqueRarities)
        {
            float totalItemWeight = currentFishingTable.entries
                .Where(e => e.rarity == rarity)
                .Sum(e => e.baseWeight);
            
            rarityTrackers[rarity] = new RarityTracker
            {
                rarity = rarity,
                baseWeight = rarityWeights[rarity],
                currentWeight = rarityWeights[rarity],
                totalItemWeight = totalItemWeight,
                rollCount = 0,
                expectedFrequency = 0f, // Calculated later
                hitCount = 0
            };
        }
    }
    
    /// <summary>
    /// Initialize item type trackers for all present types
    /// </summary>
    private void InitializeItemTypeTrackers()
    {
        HashSet<ItemType> discoveredTypes = new HashSet<ItemType>();
        
        foreach (var entry in currentFishingTable.entries)
        {
            CatchableItem item = itemLookup[entry.itemID];
            
            if (item.itemType1 != ItemType.Null)
                discoveredTypes.Add(item.itemType1);
            
            if (item.itemType2 != ItemType.Null)
                discoveredTypes.Add(item.itemType2);
        }
        
        foreach (var type in discoveredTypes)
        {
            float totalWeight = 0f;
            
            foreach (var entry in currentFishingTable.entries)
            {
                CatchableItem item = itemLookup[entry.itemID];
                
                if (item.itemType1 == type || item.itemType2 == type)
                {
                    totalWeight += entry.totalWeight;
                }
            }
            
            itemTypeTrackers[type] = new ItemTypeTracker
            {
                type = type,
                totalWeight = totalWeight,
                currentBoost = 1f,
                expectedFrequency = 0f, // Calculated later
                hitCount = 0
            };
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[AdaptiveRNG] Discovered item types: {string.Join(", ", discoveredTypes)}");
        }
    }
    
    /// <summary>
    /// Calculate expected frequencies for rarities (FLOAT precision)
    /// </summary>
    private void CalculateRarityFrequencies()
    {
        float totalRarityWeight = rarityTrackers.Values.Sum(r => r.baseWeight);
        
        foreach (var tracker in rarityTrackers.Values)
        {
            if (tracker.baseWeight > 0)
            {
                // Expected frequency = 100 / percent (e.g., 100 / 30 = 3.333...)
                tracker.expectedFrequency = totalRarityWeight / tracker.baseWeight;
            }
            else
            {
                tracker.expectedFrequency = float.MaxValue;
            }
        }
    }
    
    /// <summary>
    /// Calculate expected frequencies for item types (FLOAT precision)
    /// </summary>
    private void CalculateItemTypeFrequencies()
    {
        float totalWeight = currentFishingTable.totalCombinedWeight;
        
        foreach (var tracker in itemTypeTrackers.Values)
        {
            if (tracker.totalWeight > 0)
            {
                // Expected frequency = total / type's weight
                tracker.expectedFrequency = totalWeight / tracker.totalWeight;
            }
            else
            {
                tracker.expectedFrequency = float.MaxValue;
            }
        }
    }
    
    /// <summary>
    /// Calculate targets for items (within their rarity)
    /// </summary>
    private void CalculateItemTargets()
    {
        foreach (var entry in currentFishingTable.entries)
        {
            RarityTracker rarityTracker = rarityTrackers[entry.rarity];
            
            if (entry.baseWeight > 0)
            {
                entry.targetRollsWithinRarity = Mathf.RoundToInt(rarityTracker.totalItemWeight / entry.baseWeight);
            }
            else
            {
                entry.targetRollsWithinRarity = int.MaxValue;
            }
        }
    }
    
    /// <summary>
    /// Calculate rarity weights based on luck using keyframe interpolation
    /// </summary>
    private Dictionary<ItemRarity, float> CalculateRarityWeights(float luck)
    {
        LuckKeyframe lowerKeyframe = luckKeyframes[0];
        LuckKeyframe upperKeyframe = luckKeyframes[luckKeyframes.Length - 1];
        
        for (int i = 0; i < luckKeyframes.Length - 1; i++)
        {
            if (luck >= luckKeyframes[i].luckValue && luck <= luckKeyframes[i + 1].luckValue)
            {
                lowerKeyframe = luckKeyframes[i];
                upperKeyframe = luckKeyframes[i + 1];
                break;
            }
        }
        
        float t = 0f;
        if (upperKeyframe.luckValue != lowerKeyframe.luckValue)
        {
            t = (luck - lowerKeyframe.luckValue) / (upperKeyframe.luckValue - lowerKeyframe.luckValue);
        }
        
        Dictionary<ItemRarity, float> weights = new Dictionary<ItemRarity, float>();
        
        weights[ItemRarity.Common] = Mathf.Lerp(lowerKeyframe.rarityWeights[0], upperKeyframe.rarityWeights[0], t);
        weights[ItemRarity.Uncommon] = Mathf.Lerp(lowerKeyframe.rarityWeights[1], upperKeyframe.rarityWeights[1], t);
        weights[ItemRarity.Rare] = Mathf.Lerp(lowerKeyframe.rarityWeights[2], upperKeyframe.rarityWeights[2], t);
        weights[ItemRarity.Extraordinary] = Mathf.Lerp(lowerKeyframe.rarityWeights[3], upperKeyframe.rarityWeights[3], t);
        weights[ItemRarity.Mythical] = Mathf.Lerp(lowerKeyframe.rarityWeights[4], upperKeyframe.rarityWeights[4], t);
        
        return weights;
    }
    
    /// <summary>
    /// Filter items by bait conditions
    /// </summary>
    private List<CatchableItem> FilterByBait(List<CatchableItem> items, BaitType currentBait)
    {
        List<CatchableItem> filtered = new List<CatchableItem>();
        
        foreach (CatchableItem item in items)
        {
            if (item.baitConditions != null && item.baitConditions.Count > 0)
            {
                if (item.baitConditions.Contains(currentBait))
                {
                    filtered.Add(item);
                }
            }
            else
            {
                filtered.Add(item);
            }
        }
        
        return filtered;
    }
    
    /// <summary>
    /// Create a fishing table entry for an item
    /// </summary>
    private FishingTableEntry CreateTableEntry(CatchableItem item, Dictionary<ItemRarity, float> rarityWeights)
    {
        FishingTableEntry entry = new FishingTableEntry
        {
            itemID = item.itemID,
            itemName = item.itemName,
            rarity = item.rarity,
            baseWeight = item.spawnProbabilityWeight,
            currentWeight = item.spawnProbabilityWeight,
            rarityWeight = rarityWeights[item.rarity],
            totalWeight = item.spawnProbabilityWeight * (rarityWeights[item.rarity] / 100f),
            targetRollsWithinRarity = 0,
            itemReference = item
        };
        
        return entry;
    }
    
    /// <summary>
    /// Perform a single roll with adaptive pity system
    /// </summary>
    public CatchableItem PerformRoll()
    {
        if (currentFishingTable == null || currentFishingTable.entries.Count == 0)
        {
            Debug.LogError("[AdaptiveRNG] Cannot perform roll - fishing table not built!");
            return null;
        }
        
        globalRollCounter++;
        
        if (showDebugLogs)
            Debug.Log($"[AdaptiveRNG] === ROLL #{globalRollCounter} ===");
        
        // Step 2: Calculate boosts (if pity system enabled)
        if (enablePitySystem)
        {
            CalculateRarityBoosts();
            CalculateItemTypeBoosts();
        }
        
        // Step 3: Roll for rarity
        ItemRarity rolledRarity = RollForRarity();
        
        if (showDebugLogs)
            Debug.Log($"  [RARITY ROLLED] {rolledRarity}");
        
        // Step 4: Increment that rarity's roll counter
        rarityTrackers[rolledRarity].rollCount++;
        rarityTrackers[rolledRarity].hitCount++;
        
        // Step 5: Calculate item boosts (using rarity's roll count)
        if (enablePitySystem)
        {
            CalculateItemBoosts(rolledRarity);
        }
        
        // Step 6: Apply item type boosts to items
        ApplyItemTypeBoosts();
        
        // Step 7: Roll within rarity
        CatchableItem caughtItem = RollWithinRarity(rolledRarity);
        
        // Step 8: Update tracking
        if (caughtItem != null)
        {
            UpdateCatchTracking(caughtItem);
        }
        
        return caughtItem;
    }
    
    /// <summary>
    /// Calculate rarity boosts based on global counter
    /// FIXED: Use float-based expected frequency to avoid rounding errors
    /// </summary>
    private void CalculateRarityBoosts()
    {
        foreach (var tracker in rarityTrackers.Values)
        {
            // Calculate expected hits by this roll
            float expectedHits = globalRollCounter / tracker.expectedFrequency;
            
            // How far behind/ahead are we? (in terms of hits)
            float hitsDeviation = expectedHits - tracker.hitCount;
            
            if (hitsDeviation > 0.01f) // We're behind (need more hits)
            {
                // Convert hits deviation back to rolls
                float rollsDeviation = hitsDeviation * tracker.expectedFrequency;
                
                // Calculate anticipation threshold in rolls
                float anticipationThresholdRolls = tracker.expectedFrequency * (1f - anticipationStartRatio);
                
                if (rollsDeviation >= tracker.expectedFrequency) // Past due by at least 1 full cycle
                {
                    // POST-TARGET: Aggressive boost
                    float rollsOverdue = rollsDeviation - tracker.expectedFrequency;
                    float boost = CalculateBoost(Mathf.FloorToInt(rollsOverdue), rarityPostTargetBoostRate);
                    tracker.currentWeight = tracker.baseWeight * Mathf.Min(boost, maxBoostMultiplier);
                    
                    if (showDebugLogs && boost > 1.01f)
                        Debug.Log($"  [RARITY POST-BOOST] {tracker.rarity}: {boost:F2}x (overdue by {rollsOverdue:F1} rolls)");
                }
                else if (rollsDeviation >= anticipationThresholdRolls) // In anticipation range
                {
                    // PRE-TARGET: Gentle boost
                    float rollsIntoAnticipation = rollsDeviation - anticipationThresholdRolls;
                    float boost = CalculateBoost(Mathf.FloorToInt(rollsIntoAnticipation), rarityPreTargetBoostRate);
                    tracker.currentWeight = tracker.baseWeight * Mathf.Min(boost, maxBoostMultiplier);
                    
                    if (showDebugLogs && boost > 1.01f)
                        Debug.Log($"  [RARITY PRE-BOOST] {tracker.rarity}: {boost:F2}x");
                }
                else
                {
                    // Within acceptable range - no boost
                    tracker.currentWeight = tracker.baseWeight;
                }
            }
            else
            {
                // On target or ahead - no boost
                tracker.currentWeight = tracker.baseWeight;
            }
        }
    }
    
    /// <summary>
    /// Calculate item type boosts based on global counter
    /// FIXED: Use float-based expected frequency to avoid rounding errors
    /// </summary>
    private void CalculateItemTypeBoosts()
    {
        foreach (var tracker in itemTypeTrackers.Values)
        {
            // Calculate expected hits by this roll
            float expectedHits = globalRollCounter / tracker.expectedFrequency;
            
            // How far behind/ahead are we?
            float hitsDeviation = expectedHits - tracker.hitCount;
            
            if (hitsDeviation > 0.01f) // We're behind
            {
                // Convert to rolls
                float rollsDeviation = hitsDeviation * tracker.expectedFrequency;
                
                // Calculate anticipation threshold
                float anticipationThresholdRolls = tracker.expectedFrequency * (1f - anticipationStartRatio);
                
                if (rollsDeviation >= tracker.expectedFrequency) // Past due
                {
                    // POST-TARGET
                    float rollsOverdue = rollsDeviation - tracker.expectedFrequency;
                    tracker.currentBoost = Mathf.Min(
                        CalculateBoost(Mathf.FloorToInt(rollsOverdue), itemTypePostTargetBoostRate),
                        maxBoostMultiplier
                    );
                    
                    if (showDebugLogs && tracker.currentBoost > 1.01f)
                        Debug.Log($"  [TYPE POST-BOOST] {tracker.type}: {tracker.currentBoost:F2}x");
                }
                else if (rollsDeviation >= anticipationThresholdRolls) // Anticipation
                {
                    // PRE-TARGET
                    float rollsIntoAnticipation = rollsDeviation - anticipationThresholdRolls;
                    tracker.currentBoost = Mathf.Min(
                        CalculateBoost(Mathf.FloorToInt(rollsIntoAnticipation), itemTypePreTargetBoostRate),
                        maxBoostMultiplier
                    );
                    
                    if (showDebugLogs && tracker.currentBoost > 1.01f)
                        Debug.Log($"  [TYPE PRE-BOOST] {tracker.type}: {tracker.currentBoost:F2}x");
                }
                else
                {
                    tracker.currentBoost = 1f;
                }
            }
            else
            {
                tracker.currentBoost = 1f;
            }
        }
    }
    
    /// <summary>
    /// Calculate item boosts based on rarity's roll counter
    /// </summary>
    private void CalculateItemBoosts(ItemRarity rarity)
    {
        RarityTracker rarityTracker = rarityTrackers[rarity];
        int rarityRollCount = rarityTracker.rollCount;
        
        var itemsInRarity = currentFishingTable.entries.Where(e => e.rarity == rarity);
        
        foreach (var entry in itemsInRarity)
        {
            ItemStats stats = itemStatsTracker[entry.itemID];
            int nextExpectedRoll = stats.targetRollsWithinRarity * (stats.catchCount + 1);
            
            if (rarityRollCount >= nextExpectedRoll)
            {
                // POST-TARGET
                int rollsOverdue = rarityRollCount - nextExpectedRoll;
                stats.currentItemBoost = Mathf.Min(
                    CalculateBoost(rollsOverdue, itemPostTargetBoostRate),
                    maxBoostMultiplier
                );
                
                if (showDebugLogs && stats.currentItemBoost > 1.01f)
                    Debug.Log($"  [ITEM POST-BOOST] {entry.itemName}: {stats.currentItemBoost:F2}x");
            }
            else
            {
                // Check anticipation
                int rollsUntilExpected = nextExpectedRoll - rarityRollCount;
                float anticipationThreshold = stats.targetRollsWithinRarity * (1f - anticipationStartRatio);
                
                if (rollsUntilExpected <= anticipationThreshold)
                {
                    // PRE-TARGET
                    int rollsIntoAnticipation = Mathf.FloorToInt(anticipationThreshold) - rollsUntilExpected;
                    stats.currentItemBoost = Mathf.Min(
                        CalculateBoost(rollsIntoAnticipation, itemPreTargetBoostRate),
                        maxBoostMultiplier
                    );
                    
                    if (showDebugLogs && stats.currentItemBoost > 1.01f)
                        Debug.Log($"  [ITEM PRE-BOOST] {entry.itemName}: {stats.currentItemBoost:F2}x");
                }
                else
                {
                    stats.currentItemBoost = 1f;
                }
            }
        }
    }
    
    /// <summary>
    /// Apply item type boosts to items (multiplicative)
    /// </summary>
    private void ApplyItemTypeBoosts()
    {
        foreach (var entry in currentFishingTable.entries)
        {
            CatchableItem item = itemLookup[entry.itemID];
            ItemStats stats = itemStatsTracker[entry.itemID];
            
            // Start with base weight × item boost
            float finalWeight = entry.baseWeight * stats.currentItemBoost;
            
            // Multiply by type1 boost if exists
            if (item.itemType1 != ItemType.Null && itemTypeTrackers.ContainsKey(item.itemType1))
            {
                finalWeight *= itemTypeTrackers[item.itemType1].currentBoost;
            }
            
            // Multiply by type2 boost if exists (dual-type stacking)
            if (item.itemType2 != ItemType.Null && itemTypeTrackers.ContainsKey(item.itemType2))
            {
                finalWeight *= itemTypeTrackers[item.itemType2].currentBoost;
            }
            
            entry.currentWeight = finalWeight;
        }
    }
    
    /// <summary>
    /// Generic boost calculation
    /// </summary>
    private float CalculateBoost(int rollsIntoBoost, float boostRate)
    {
        return Mathf.Pow(1f + boostRate, rollsIntoBoost);
    }
    
    /// <summary>
    /// Roll for rarity based on current rarity weights
    /// </summary>
    private ItemRarity RollForRarity()
    {
        float totalWeight = rarityTrackers.Values.Sum(r => r.currentWeight);
        float randomValue = Random.Range(0f, totalWeight);
        float currentThreshold = 0f;
        
        foreach (var tracker in rarityTrackers.Values.OrderByDescending(r => (int)r.rarity))
        {
            currentThreshold += tracker.currentWeight;
            
            if (randomValue < currentThreshold)
            {
                return tracker.rarity;
            }
        }
        
        return ItemRarity.Common;
    }
    
    /// <summary>
    /// Roll within a specific rarity using current (boosted) weights
    /// </summary>
    private CatchableItem RollWithinRarity(ItemRarity rarity)
    {
        List<FishingTableEntry> itemsInRarity = currentFishingTable.entries
            .Where(e => e.rarity == rarity)
            .ToList();
        
        if (itemsInRarity.Count == 0)
        {
            Debug.LogError($"[AdaptiveRNG] No items in rarity: {rarity}");
            return null;
        }
        
        float totalWeightInRarity = itemsInRarity.Sum(e => e.currentWeight);
        float randomValue = Random.Range(0f, totalWeightInRarity);
        float currentThreshold = 0f;
        
        foreach (var entry in itemsInRarity)
        {
            currentThreshold += entry.currentWeight;
            
            if (randomValue < currentThreshold)
            {
                if (showDebugLogs)
                {
                    float oddsPercent = (entry.currentWeight / totalWeightInRarity) * 100f;
                    Debug.Log($"  [CAUGHT] {entry.itemName} - Item boost: {itemStatsTracker[entry.itemID].currentItemBoost:F2}x " +
                              $"(Odds: {oddsPercent:F1}%)");
                }
                
                return itemLookup[entry.itemID];
            }
        }
        
        return itemLookup[itemsInRarity[0].itemID];
    }
    
    /// <summary>
    /// Update tracking after catching an item
    /// </summary>
    private void UpdateCatchTracking(CatchableItem item)
    {
        // Increment item catch count
        itemStatsTracker[item.itemID].catchCount++;
        
        // Reset item boost
        itemStatsTracker[item.itemID].currentItemBoost = 1f;
        
        // Increment item type hit counts
        if (item.itemType1 != ItemType.Null && itemTypeTrackers.ContainsKey(item.itemType1))
        {
            itemTypeTrackers[item.itemType1].hitCount++;
        }
        
        if (item.itemType2 != ItemType.Null && itemTypeTrackers.ContainsKey(item.itemType2))
        {
            itemTypeTrackers[item.itemType2].hitCount++;
        }
        
        // Reset item's current weight
        var entry = currentFishingTable.entries.Find(e => e.itemID == item.itemID);
        if (entry != null)
        {
            entry.currentWeight = entry.baseWeight;
        }
    }
    
    /// <summary>
    /// Get current fishing table (for debug)
    /// </summary>
    public FishingTable GetCurrentTable() => currentFishingTable;
    
    /// <summary>
    /// Get global roll counter (for debug)
    /// </summary>
    public int GetGlobalRolls() => globalRollCounter;
    
    /// <summary>
    /// Get rarity trackers (for debug menu)
    /// </summary>
    public Dictionary<ItemRarity, RarityTracker> GetRarityTrackers() => rarityTrackers;
    
    /// <summary>
    /// Get item type trackers (for debug menu)
    /// </summary>
    public Dictionary<ItemType, ItemTypeTracker> GetItemTypeTrackers() => itemTypeTrackers;
    
    /// <summary>
    /// Get item stats tracker (for debug menu)
    /// </summary>
    public Dictionary<string, ItemStats> GetItemStats() => itemStatsTracker;
}

// ============================================
// DATA STRUCTURES
// ============================================

[System.Serializable]
public class LuckKeyframe
{
    public float luckValue;
    public float[] rarityWeights;
    
    public LuckKeyframe(float luck, float[] weights)
    {
        luckValue = luck;
        rarityWeights = weights;
    }
}

public class FishingTable
{
    public List<FishingTableEntry> entries = new List<FishingTableEntry>();
    public float totalCombinedWeight = 0f;
    
    public void AddEntry(FishingTableEntry entry)
    {
        entries.Add(entry);
    }
    
    public void CalculateTotalWeight()
    {
        totalCombinedWeight = entries.Sum(e => e.totalWeight);
    }
}

public class FishingTableEntry
{
    public string itemID;
    public string itemName;
    public ItemRarity rarity;
    public float baseWeight;
    public float currentWeight;
    public float rarityWeight;
    public float totalWeight;
    public int targetRollsWithinRarity;
    public CatchableItem itemReference;
}

public class RarityTracker
{
    public ItemRarity rarity;
    public float baseWeight;
    public float currentWeight;
    public float totalItemWeight;
    public int rollCount;
    public float expectedFrequency; // Expected GLOBAL rolls per hit (using float for precision)
    public int hitCount;
}

public class ItemTypeTracker
{
    public ItemType type;
    public float totalWeight;
    public float currentBoost;
    public float expectedFrequency; // Expected GLOBAL rolls per hit (using float for precision)
    public int hitCount;
}

public class ItemStats
{
    public string itemName;
    public ItemRarity belongsToRarity;
    public float baseWeight;
    public float currentItemBoost;
    public int targetRollsWithinRarity;
    public int catchCount;
    
    public int NextExpectedRarityRoll => targetRollsWithinRarity * (catchCount + 1);
}