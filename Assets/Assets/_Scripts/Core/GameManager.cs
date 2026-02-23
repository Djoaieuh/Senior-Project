using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager _instance;
    
    [Header("Player Data (Single Source of Truth)")]
    [SerializeField] private PlayerData playerData = new PlayerData();
    
    [Header("Starter Gear (Given to Player on First Start)")]
    [Tooltip("Default rod base given to new players")]
    [SerializeField] private RodBase starterRodBase;
    
    [Tooltip("Default reel given to new players")]
    [SerializeField] private Reel starterReel;
    
    [Tooltip("Default fishing line given to new players")]
    [SerializeField] private FishingLine starterLine;
    
    [Tooltip("Default hook given to new players")]
    [SerializeField] private FishingHook starterHook;
    
    [Header("Item Database")]
    [SerializeField] private MasterItemDatabase itemDatabase;
    
    [Header("Default Location (First Boot)")]
    [Tooltip("Scene name to load if no save file exists - must match exactly in Build Settings")]
    [SerializeField] private string defaultSceneName;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Public accessors
    public PlayerData PlayerData => playerData;
    public PlayerInventory Inventory => playerData.Inventory;
    public int Money => playerData.Money;
    public EquippedGearInventory EquippedGear => playerData.EquippedGear;
    public JournalData Journal => playerData.Journal;
    public MapData Map => playerData.Map;
    
    // Events (forwarded from PlayerData)
    public event System.Action<int> OnMoneyChanged;
    public event System.Action OnGearChanged;
    
    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        MasterItemDatabase.SetInstance(itemDatabase);

        playerData.Initialize();

        // Forward events
        playerData.OnMoneyChanged += (money) => OnMoneyChanged?.Invoke(money);
        playerData.OnGearChanged  += () => OnGearChanged?.Invoke();

        // Load save data
        SaveData saveData = SaveSystem.Load();

        if (saveData != null)
        {
            SaveSystem.ApplyToPlayerData(saveData, playerData);
            LoadScene(saveData.currentSceneName);
        }
        else
        {
            // First boot - give starter gear then load default location
            CheckAndGiveStarterGear();
            LoadScene(defaultSceneName); // add [SerializeField] string defaultSceneName to GameManager
        }
    }
    
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[GameManager] No scene name to load!");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
    
    // Call this whenever you want to save - from map, from pause menu, etc.
    public void SaveGame()
    {
        string locationID   = FishingSpotData.Current?.LocationID   ?? "";
        string sceneName    = FishingSpotData.Current?.SceneName    ?? "";
        SaveSystem.Save(playerData, locationID, sceneName);
    }
    
    // ============================================
    // MONEY MANAGEMENT (Forwarded to PlayerData)
    // ============================================
    
    public void AddMoney(int amount) => playerData.AddMoney(amount);
    public bool RemoveMoney(int amount) => playerData.RemoveMoney(amount);
    public void SetMoney(int amount) => playerData.SetMoney(amount);
    public bool CanAfford(int cost) => playerData.CanAfford(cost);
    
    // ============================================
    // GEAR EQUIP/UNEQUIP METHODS
    // ============================================
    
    public bool EquipRodBase(string itemID)
    {
        var rodBase = Inventory.GetAllRodBases().Find(r => r.itemID == itemID);
        if (rodBase == null)
        {
            Debug.LogWarning($"[GameManager] Rod base {itemID} not found in inventory!");
            return false;
        }
        
        EquippedGear.EquipRodBase(rodBase);
        FishingEvents.EquippedGearChanged();
        return true;
    }
    
    public bool EquipReel(string itemID)
    {
        var reel = Inventory.GetAllReels().Find(r => r.itemID == itemID);
        if (reel == null)
        {
            Debug.LogWarning($"[GameManager] Reel {itemID} not found in inventory!");
            return false;
        }
        
        EquippedGear.EquipReel(reel);
        FishingEvents.EquippedGearChanged();
        return true;
    }
    
    public bool EquipLine(string itemID)
    {
        var line = Inventory.GetAllFishingLines().Find(l => l.itemID == itemID);
        if (line == null)
        {
            Debug.LogWarning($"[GameManager] Line {itemID} not found in inventory!");
            return false;
        }
        
        EquippedGear.EquipLine(line);
        FishingEvents.EquippedGearChanged();
        return true;
    }
    
    public bool EquipHook(string itemID)
    {
        var hook = Inventory.GetAllFishingHooks().Find(h => h.itemID == itemID);
        if (hook == null)
        {
            Debug.LogWarning($"[GameManager] Hook {itemID} not found in inventory!");
            return false;
        }
        
        EquippedGear.EquipHook(hook);
        FishingEvents.EquippedGearChanged();
        return true;
    }
    
    public bool EquipBait(BaitType baitType, int quantity)
    {
        var bait = Inventory.GetBait(baitType);
        if (bait == null)
        {
            Debug.LogWarning($"[GameManager] Bait {baitType} not found in inventory!");
            return false;
        }
        
        bool success = EquippedGear.EquipBait(bait, quantity);
        if (success)
        {
            FishingEvents.EquippedGearChanged();
        }
        return success;
    }
    
    public void ConsumeBait()
    {
        if (EquippedGear.Bait == null) return;
        
        bool removed = Inventory.RemoveItem(EquippedGear.Bait.itemID, 1);
        if (removed)
        {
            EquippedGear.ConsumeBait();
        }
    }
    
    public bool CanFish() => EquippedGear.IsComplete();
    
    // ============================================
    // JOURNAL RECORDING
    // ============================================
    
    /// <summary>
    /// Record item acquisition in journal and add to inventory
    /// Call this whenever player obtains an item
    /// </summary>
    public void AcquireItem(Item item, string locationID, string locationName, 
        AcquisitionMethod method, int quantity = 1)
    {
        // Extract weight class if this is a fish
        FishWeightClass? weightClass = null;
        if (item is CatchableItem catchable && catchable.HasType(ItemType.Fish))
            weightClass = catchable.weightClass;

        Journal.RecordAcquisition(item, locationID, locationName, method, weightClass);

        if (item is CatchableItem catchableItem)
            Inventory.AddCaughtItem(catchableItem, quantity);

        if (showDebugLogs)
        {
            string sizeInfo = weightClass.HasValue ? $" ({weightClass})" : "";
            Debug.Log($"[GameManager] Acquired: {item.itemName}{sizeInfo} x{quantity} from {locationName}");
        }
    }
    
    // ============================================
    // DEBUG/CHEAT METHODS
    // ============================================
    
    [ContextMenu("Debug: Print Player Data")]
    public void DebugPrintPlayerData()
    {
        Debug.Log("=== PLAYER DATA ===");
        Debug.Log($"Money: ${Money}");
        Debug.Log($"Inventory Slots: {Inventory.CurrentSlotCount}/{Inventory.MaxSlots}");
        Debug.Log($"Journal: {Journal.GetDiscoveredCount()} discovered");
        Debug.Log($"Map: {Map.GetUnlockedCount()} locations unlocked");
        Debug.Log($"\n{EquippedGear.GetStatsSummary()}");
    }
    
    private void CheckAndGiveStarterGear()
    {
        if (starterRodBase == null || starterReel == null || starterLine == null || starterHook == null)
        {
            Debug.LogWarning("[GameManager] Starter gear not assigned!");
            return;
        }
        
        bool needsRod = !Inventory.GetAllRodBases().Any(r => r.itemID == starterRodBase.rodID);
        bool needsReel = !Inventory.GetAllReels().Any(r => r.itemID == starterReel.reelID);
        bool needsLine = !Inventory.GetAllFishingLines().Any(l => l.itemID == starterLine.lineID);
        bool needsHook = !Inventory.GetAllFishingHooks().Any(h => h.itemID == starterHook.hookID);
        
        if (needsRod)
        {
            Inventory.AddRodBase(starterRodBase);
            var rodItem = Inventory.GetAllRodBases().Find(r => r.itemID == starterRodBase.rodID);
            if (rodItem != null) EquippedGear.EquipRodBase(rodItem);
            if (showDebugLogs) Debug.Log($"[GameManager] Given starter rod: {starterRodBase.rodName}");
        }
        
        if (needsReel)
        {
            Inventory.AddReel(starterReel);
            var reelItem = Inventory.GetAllReels().Find(r => r.itemID == starterReel.reelID);
            if (reelItem != null) EquippedGear.EquipReel(reelItem);
            if (showDebugLogs) Debug.Log($"[GameManager] Given starter reel: {starterReel.reelName}");
        }
        
        if (needsLine)
        {
            Inventory.AddFishingLine(starterLine);
            var lineItem = Inventory.GetAllFishingLines().Find(l => l.itemID == starterLine.lineID);
            if (lineItem != null) EquippedGear.EquipLine(lineItem);
            if (showDebugLogs) Debug.Log($"[GameManager] Given starter line: {starterLine.lineName}");
        }
        
        if (needsHook)
        {
            Inventory.AddFishingHook(starterHook);
            var hookItem = Inventory.GetAllFishingHooks().Find(h => h.itemID == starterHook.hookID);
            if (hookItem != null) EquippedGear.EquipHook(hookItem);
            if (showDebugLogs) Debug.Log($"[GameManager] Given starter hook: {starterHook.hookName}");
        }
    }
}