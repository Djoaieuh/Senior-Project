using UnityEngine; 
using UnityEngine.InputSystem; 
using System.Collections; 
using System.Collections.Generic; 
using System.Linq; 
using System.Text; 
using System.IO; 

public class FishingManager : MonoBehaviour 
{ 
    [Header("RNG System")] 
    [Tooltip("Adaptive RNG system (should be on same GameObject)")] 
    [SerializeField] private AdaptiveRNGSystem rngSystem; 
    
    [Header("Minigame")] 
    [SerializeField] private FishingMinigameController minigameController; 
    
    [Header("Input")] 
    [SerializeField] private InputActionReference catchAction; 
    [SerializeField] private InputActionReference switchBaitAction; 
    
    [Header("UI References")] 
    [SerializeField] private InventoryUI inventoryUI; 
    
    [Header("Timing")] 
    [Tooltip("Minimum wait time before fish bites (seconds)")] 
    [SerializeField] private float minWaitTime = 2f; 
    [Tooltip("Maximum wait time before fish bites (seconds)")] 
    [SerializeField] private float maxWaitTime = 4f; 
    
    [Header("Simulation Mode")] 
    [Tooltip("Enable simulation mode - bypasses animations/minigame, runs X simulations")] 
    [SerializeField] private bool simulationMode = false; 
    [Tooltip("Number of fishing simulations to run")] 
    [SerializeField] private int numberOfSimulations = 1000; 
    
    [Header("Debug")] 
    [SerializeField] private bool showDebugLogs = true; 

    // Always pulls from the scene's FishingSpotData - single source of truth
    private FishingPool FishingPool => FishingSpotData.Current?.FishingPool;
    
    private bool isWaitingForBite = false; 
    
    private void Awake() 
    { 
        if (rngSystem == null) 
            rngSystem = GetComponent<AdaptiveRNGSystem>(); 
    } 
    
    private void Start() 
    { 
        if (rngSystem != null && FishingPool != null) 
        { 
            EquippedGearInventory gear = GameManager._instance.EquippedGear; 
            rngSystem.BuildFishingTable(FishingPool, gear); 
        } 
    } 
    
    private void OnEnable() 
    { 
        if (catchAction != null) 
        { 
            catchAction.action.Enable(); 
            catchAction.action.performed += OnCatchPerformed; 
        } 
        
        if (switchBaitAction != null) 
        { 
            switchBaitAction.action.Enable(); 
            switchBaitAction.action.performed += OnSwitchBaitPerformed; 
        } 
        
        FishingEvents.OnFishCaught += HandleFishCaught; 
    } 
    
    private void OnDisable() 
    { 
        if (catchAction != null) 
        { 
            catchAction.action.performed -= OnCatchPerformed; 
            catchAction.action.Disable(); 
        } 
        
        if (switchBaitAction != null) 
        { 
            switchBaitAction.action.performed -= OnSwitchBaitPerformed; 
            switchBaitAction.action.Disable(); 
        } 
        
        FishingEvents.OnFishCaught -= HandleFishCaught; 
    } 
    
    private void OnCatchPerformed(InputAction.CallbackContext context) 
    { 
        if ((minigameController != null && minigameController.IsActive()) || isWaitingForBite) 
        { 
            if (showDebugLogs && !simulationMode) 
                Debug.Log("[Catch Blocked] Minigame is active or waiting for bite!"); 
            return; 
        } 
        
        if (inventoryUI != null && inventoryUI.IsOpen()) 
        { 
            if (showDebugLogs && !simulationMode) 
                Debug.Log("[Catch Blocked] Cannot fish while inventory is open!"); 
            return; 
        } 
        
        if (simulationMode) 
        { 
            RunSimulations(); 
            return; 
        } 
        
        CatchFish(); 
    } 
    
    private void OnSwitchBaitPerformed(InputAction.CallbackContext context) 
    { 
        if ((minigameController != null && minigameController.IsActive()) || isWaitingForBite) 
        { 
            if (showDebugLogs && !simulationMode) 
                Debug.Log("[Bait Switch Blocked] Minigame is active or waiting for bite!"); 
            return; 
        } 
        
        SwitchBait(); 
    } 
    
    /// <summary> 
    /// Cycle to the next bait type 
    /// </summary> 
    private void SwitchBait() 
    { 
        EquippedGearInventory gear = GameManager._instance.EquippedGear; 
        if (gear.Bait == null) 
        { 
            if (showDebugLogs && !simulationMode) 
                Debug.Log("[Bait Switch] No bait equipped!"); 
            return; 
        } 
        
        BaitType previousBait = gear.CurrentBaitType; 
        FishingEvents.BaitChanged(previousBait, gear.CurrentBaitType); 
        
        if (showDebugLogs && !simulationMode) 
            Debug.Log($"[Bait] Current: {gear.CurrentBaitType} ({gear.BaitQuantity}x remaining)"); 
    } 
    
    /// <summary> 
    /// Run fishing simulations 
    /// </summary> 
    private void RunSimulations() 
    { 
        if (rngSystem == null || FishingPool == null) 
        { 
            Debug.LogError("[Simulation] Cannot run - missing RNG system or fishing pool!"); 
            return; 
        } 
        
        Debug.Log($"[SIMULATION MODE] Running {numberOfSimulations} simulations..."); 
        
        bool originalDebugSetting = rngSystem.showDebugLogs; 
        rngSystem.showDebugLogs = false; 
        
        FishingTable table = rngSystem.GetCurrentTable(); 
        if (table == null || table.entries.Count == 0) 
        { 
            Debug.LogError("[Simulation] Fishing table is empty!"); 
            rngSystem.showDebugLogs = originalDebugSetting; 
            return; 
        } 
        
        Dictionary<string, int> itemCatches = new Dictionary<string, int>(); 
        Dictionary<ItemRarity, int> rarityCatches = new Dictionary<ItemRarity, int>(); 
        Dictionary<ItemType, int> typeCatches = new Dictionary<ItemType, int>(); 
        
        foreach (var entry in table.entries) 
        { 
            itemCatches[entry.itemID] = 0; 
            if (!rarityCatches.ContainsKey(entry.rarity)) rarityCatches[entry.rarity] = 0; 
        } 
        
        var typeTrackers = rngSystem.GetItemTypeTrackers(); 
        foreach (var type in typeTrackers.Keys) 
            typeCatches[type] = 0; 
        
        for (int i = 0; i < numberOfSimulations; i++) 
        { 
            CatchableItem caught = rngSystem.PerformRoll(); 
            if (caught != null) 
            { 
                itemCatches[caught.itemID]++; 
                rarityCatches[caught.rarity]++; 
                
                if (caught.itemType1 != ItemType.Null && typeCatches.ContainsKey(caught.itemType1)) 
                    typeCatches[caught.itemType1]++; 
                if (caught.itemType2 != ItemType.Null && typeCatches.ContainsKey(caught.itemType2)) 
                    typeCatches[caught.itemType2]++; 
                
                FishingEvents.FishCaught(caught); 
            } 
        } 
        
        rngSystem.showDebugLogs = originalDebugSetting; 
        
        string summary = GenerateSimulationSummary( 
            numberOfSimulations, table, itemCatches, rarityCatches, typeCatches, typeTrackers 
        ); 
        
        Debug.Log(summary); 
        SaveSimulationToFile(summary); 
        
        Debug.Log("[SIMULATION MODE] Complete! Results saved to simulation_results.txt"); 
    } 
    
    /// <summary> 
    /// Generate simulation summary 
    /// </summary> 
    private string GenerateSimulationSummary( 
        int totalRolls, 
        FishingTable table, 
        Dictionary<string, int> itemCatches, 
        Dictionary<ItemRarity, int> rarityCatches, 
        Dictionary<ItemType, int> typeCatches, 
        Dictionary<ItemType, ItemTypeTracker> typeTrackers) 
    { 
        StringBuilder sb = new StringBuilder(); 
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗"); 
        sb.AppendLine("║           FISHING SIMULATION RESULTS                           ║"); 
        sb.AppendLine("╚════════════════════════════════════════════════════════════════╝"); 
        sb.AppendLine(); 
        
        sb.AppendLine($"Total Rolls: {totalRolls}"); 
        sb.AppendLine($"Pity System: {(rngSystem.IsPitySystemEnabled ? "ENABLED" : "DISABLED")}"); 
        sb.AppendLine(); 
        
        var rarityTrackers = rngSystem.GetRarityTrackers(); 
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        sb.AppendLine("RARITIES"); 
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        
        foreach (var rarity in rarityCatches.Keys.OrderByDescending(r => (int)r)) 
        { 
            int caught = rarityCatches[rarity]; 
            float expectedPercent = rarityTrackers[rarity].baseWeight; 
            float expected = (totalRolls * expectedPercent) / 100f; 
            float actualPercent = (caught / (float)totalRolls) * 100f; 
            float deviation = ((caught - expected) / expected) * 100f; 
            
            sb.AppendLine($"{rarity,-15} Caught: {caught,6} | Expected: {expected,8:F2} ({expectedPercent:F2}%) | Actual: {actualPercent:F2}% | Deviation: {deviation,6:+0.00;-0.00}%"); 
        } 
        sb.AppendLine(); 
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        sb.AppendLine("ITEM TYPES"); 
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        
        foreach (var type in typeCatches.Keys.OrderBy(t => t.ToString())) 
        { 
            int caught = typeCatches[type]; 
            float expectedTypeCount = 0f; 
            
            foreach (var rarityTracker in rarityTrackers.Values) 
            { 
                float rarityProbability = rarityTracker.baseWeight / 100f; 
                var itemsInRarity = table.entries.Where(e => e.rarity == rarityTracker.rarity); 
                float totalBaseWeightInRarity = itemsInRarity.Sum(e => e.baseWeight); 
                float typeBaseWeightInRarity = 0f; 
                
                foreach (var entry in itemsInRarity) 
                { 
                    if (entry.itemReference.itemType1 == type || entry.itemReference.itemType2 == type) 
                        typeBaseWeightInRarity += entry.baseWeight; 
                } 
                
                if (totalBaseWeightInRarity > 0) 
                { 
                    float typeProbabilityInRarity = typeBaseWeightInRarity / totalBaseWeightInRarity; 
                    expectedTypeCount += totalRolls * rarityProbability * typeProbabilityInRarity; 
                } 
            } 
            
            float expectedPercent = (expectedTypeCount / totalRolls) * 100f; 
            float actualPercent = (caught / (float)totalRolls) * 100f; 
            float deviation = expectedTypeCount > 0 ? ((caught - expectedTypeCount) / expectedTypeCount) * 100f : 0f; 
            
            sb.AppendLine($"{type,-15} Caught: {caught,6} | Expected: {expectedTypeCount,8:F2} ({expectedPercent:F2}%) | Actual: {actualPercent:F2}% | Deviation: {deviation,6:+0.00;-0.00}%"); 
        } 
        sb.AppendLine(); 
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        sb.AppendLine("ITEMS (by Rarity)"); 
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        
        var entriesByRarity = table.entries 
            .OrderByDescending(e => (int)e.rarity) 
            .ThenBy(e => e.itemName); 
            
        ItemRarity currentRarity = ItemRarity.Common; 
        bool firstRarity = true; 
        
        foreach (var entry in entriesByRarity) 
        { 
            if (entry.rarity != currentRarity || firstRarity) 
            { 
                currentRarity = entry.rarity; 
                firstRarity = false; 
                sb.AppendLine(); 
                sb.AppendLine($"--- {currentRarity} ---"); 
            } 
            
            int caught = itemCatches[entry.itemID]; 
            float rarityPercent = rarityTrackers[entry.rarity].baseWeight / 100f; 
            float rarityTotalWeight = rarityTrackers[entry.rarity].totalItemWeight; 
            float itemProbabilityWithinRarity = entry.baseWeight / rarityTotalWeight; 
            float itemProbability = rarityPercent * itemProbabilityWithinRarity; 
            float expected = totalRolls * itemProbability; 
            float actualPercent = (caught / (float)totalRolls) * 100f; 
            float deviation = expected > 0 ? ((caught - expected) / expected) * 100f : 0f; 
            
            sb.AppendLine($"  {entry.itemName,-30} Caught: {caught,6} | Expected: {expected,8:F2} ({itemProbability * 100f:F4}%) | Deviation: {deviation,6:+0.00;-0.00}%"); 
        } 
        
        sb.AppendLine(); 
        sb.AppendLine("═══════════════════════════════════════════════════════════════"); 
        return sb.ToString(); 
    } 
    
    private void SaveSimulationToFile(string content) 
    { 
        string filePath = Path.Combine(Application.dataPath, "simulation_results.txt"); 
        try 
        { 
            File.WriteAllText(filePath, content); 
            Debug.Log($"[Simulation] Results saved to: {filePath}"); 
        } 
        catch (System.Exception e) 
        { 
            Debug.LogError($"[Simulation] Failed to save file: {e.Message}"); 
        } 
    } 
    
    /// <summary> 
    /// Main fishing logic - rolls for a fish then casts the line
    /// </summary> 
    public CatchableItem CatchFish() 
    { 
        if (FishingPool == null) 
        { 
            Debug.LogError("No fishing pool assigned to this scene!"); 
            return null; 
        } 
        
        if (rngSystem == null) 
        { 
            Debug.LogError("No AdaptiveRNGSystem found!"); 
            return null; 
        } 
        
        EquippedGearInventory gear = GameManager._instance.EquippedGear; 
        if (!gear.IsComplete()) 
        { 
            Debug.LogError("Cannot fish - incomplete gear! Make sure you have Rod, Reel, Line, and Hook equipped."); 
            return null; 
        } 
        
        CatchableItem caughtItem = rngSystem.PerformRoll(); 
        if (caughtItem == null) 
        { 
            Debug.LogWarning("RNG system returned no item!"); 
            return null; 
        } 
        
        if (showDebugLogs && !simulationMode) 
        { 
            string typeInfo = caughtItem.IsDualType() ? $"{caughtItem.itemType1}/{caughtItem.itemType2}" : caughtItem.itemType1.ToString(); 
            Debug.Log($"[Hooked!] {caughtItem.itemName} ({caughtItem.rarity}) - Type: {typeInfo}"); 
        } 
        
        FishingEvents.LineCast(); 
        StartCoroutine(WaitForFishBite(caughtItem)); 
        
        return caughtItem; 
    } 
    
    private IEnumerator WaitForFishBite(CatchableItem fish) 
    { 
        isWaitingForBite = true; 
        float waitTime = Random.Range(minWaitTime, maxWaitTime); 
        
        if (showDebugLogs && !simulationMode) 
            Debug.Log($"[Waiting] Waiting {waitTime:F1}s for fish to bite..."); 
        
        yield return new WaitForSeconds(waitTime); 
        isWaitingForBite = false; 
        
        if (showDebugLogs && !simulationMode) 
            Debug.Log("[Fish Bite!] Starting minigame"); 
        
        EquippedGearInventory gear = GameManager._instance.EquippedGear; 
        
        if (minigameController != null) 
            minigameController.StartMinigame(fish, gear, FishingPool); 
        else 
            Debug.LogWarning("No minigame controller assigned! Skipping minigame."); 
    } 
    
    private void HandleFishCaught(CatchableItem fish)
    {
        if (simulationMode) return;

        string locationID   = FishingSpotData.Current?.LocationID   ?? "";
        string locationName = FishingSpotData.Current?.LocationName ?? "Unknown Waters";

        GameManager._instance.AcquireItem(fish, locationID, locationName, AcquisitionMethod.Fished, 1);
        GameManager._instance.ConsumeBait();
    }
    
    public FishingPool GetFishingPool() => FishingPool; 
}