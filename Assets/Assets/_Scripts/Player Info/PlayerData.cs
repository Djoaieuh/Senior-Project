using UnityEngine;

[System.Serializable]
public class PlayerData
{
    [Header("Core Systems")]
    [SerializeField] private PlayerInventory inventory = new PlayerInventory();
    [SerializeField] private EquippedGearInventory equippedGear = new EquippedGearInventory();
    [SerializeField] private int money = 0;
    
    [Header("Progression Systems")]
    [SerializeField] private JournalData journal = new JournalData();
    [SerializeField] private MapData map = new MapData();
    
    [Header("Current Location")]
    [SerializeField] private string currentLocationID = "";
    [SerializeField] private string currentSceneName = "";

    public string CurrentLocationID => currentLocationID;
    public string CurrentSceneName => currentSceneName;

    public void SetCurrentLocation(string locationID, string sceneName)
    {
        currentLocationID = locationID;
        currentSceneName = sceneName;
    }
    
    // Properties
    public PlayerInventory Inventory => inventory;
    public EquippedGearInventory EquippedGear => equippedGear;
    public int Money => money;
    public JournalData Journal => journal;
    public MapData Map => map;
    
    // Events
    public event System.Action<int> OnMoneyChanged;
    public event System.Action OnGearChanged;
    
    /// <summary>
    /// Initialize all subsystems
    /// </summary>
    public void Initialize()
    {
        journal.Initialize();
        map.Initialize();
        
        // Subscribe to gear changes
        equippedGear.OnGearChanged += () => OnGearChanged?.Invoke();
    }
    
    // ============================================
    // MONEY MANAGEMENT
    // ============================================
    
    public void AddMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[PlayerData] Cannot add negative money. Use RemoveMoney() instead.");
            return;
        }
        
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }
    
    public bool RemoveMoney(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[PlayerData] Cannot remove negative money. Use AddMoney() instead.");
            return false;
        }
        
        if (money < amount)
        {
            Debug.LogWarning($"[PlayerData] Not enough money! Have ${money}, need ${amount}");
            return false;
        }
        
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }
    
    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(money);
    }
    
    public bool CanAfford(int cost) => money >= cost;
}