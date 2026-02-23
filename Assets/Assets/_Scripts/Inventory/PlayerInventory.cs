using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DateTime = System.DateTime;
using Sprite = UnityEngine.Sprite;

[Serializable]
public class PlayerInventory
{
    private List<InventoryItem> items = new List<InventoryItem>();
    
    [Header("Inventory Limits")]
    [SerializeField] private int maxInventorySlots = 100;
    
    // Events
    public event Action<InventoryItem> OnItemAdded;
    public event Action<InventoryItem> OnItemRemoved;
    public event Action<InventoryItem, int> OnItemQuantityChanged;
    public event Action OnInventoryChanged;
    
    // Properties
    public int CurrentSlotCount => items.Count;
    public int MaxSlots => maxInventorySlots;
    public bool IsFull => items.Count >= maxInventorySlots;
    public IReadOnlyList<InventoryItem> Items => items.AsReadOnly();
    
    /// <summary>
    /// Add a fish to inventory (from CatchableItem)
    /// </summary>
    public bool AddFish(CatchableItem fishData, int quantity = 1)
    {
        if (fishData == null)
        {
            Debug.LogError("[Inventory] Cannot add null fish!");
            return false;
        }

        FishInventoryItem existingFish = items.OfType<FishInventoryItem>()
            .FirstOrDefault(f => f.itemID == fishData.itemID);

        if (existingFish != null)
        {
            int oldQuantity = existingFish.quantity;
            existingFish.AddQuantity(quantity);
            Debug.Log($"[Inventory] Stacked {quantity}x {existingFish.DisplayName}. Total: {existingFish.quantity}");
            OnItemQuantityChanged?.Invoke(existingFish, oldQuantity);
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            if (IsFull)
            {
                Debug.LogWarning($"[Inventory] Cannot add {fishData.itemName} - inventory full!");
                return false;
            }

            FishInventoryItem newFish = new FishInventoryItem(fishData, quantity);
            items.Add(newFish);
            Debug.Log($"[Inventory] Added {quantity}x {newFish.DisplayName}");
            OnItemAdded?.Invoke(newFish);
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
    
    /// <summary>
    /// Add material to inventory (plants, metals, magical, etc.)
    /// </summary>
    public bool AddMaterial(CatchableItem materialData, int quantity = 1)
    {
        if (materialData == null)
        {
            Debug.LogError("[Inventory] Cannot add null material!");
            return false;
        }
        
        MaterialInventoryItem existingMaterial = items.OfType<MaterialInventoryItem>()
            .FirstOrDefault(m => m.itemID == materialData.itemID);
        
        if (existingMaterial != null)
        {
            int oldQuantity = existingMaterial.quantity;
            existingMaterial.AddQuantity(quantity);
            Debug.Log($"[Inventory] Stacked {quantity}x {materialData.itemName}. Total: {existingMaterial.quantity}");
            OnItemQuantityChanged?.Invoke(existingMaterial, oldQuantity);
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            if (IsFull)
            {
                Debug.LogWarning($"[Inventory] Cannot add {materialData.itemName} - inventory full!");
                return false;
            }
            
            MaterialInventoryItem newMaterial = new MaterialInventoryItem(materialData, quantity);
            items.Add(newMaterial);
            Debug.Log($"[Inventory] Added {quantity}x {materialData.itemName}");
            OnItemAdded?.Invoke(newMaterial);
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
    
    /// <summary>
    /// Add rod base to inventory
    /// </summary>
    public bool AddRodBase(RodBase rodData)
    {
        if (rodData == null || IsFull) return false;
        
        RodBaseInventoryItem newRod = new RodBaseInventoryItem(rodData);
        items.Add(newRod);
        Debug.Log($"[Inventory] Added rod base: {rodData.rodName}");
        OnItemAdded?.Invoke(newRod);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Add reel to inventory
    /// </summary>
    public bool AddReel(Reel reelData)
    {
        if (reelData == null || IsFull) return false;
        
        ReelInventoryItem newReel = new ReelInventoryItem(reelData);
        items.Add(newReel);
        Debug.Log($"[Inventory] Added reel: {reelData.reelName}");
        OnItemAdded?.Invoke(newReel);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Add fishing line to inventory
    /// </summary>
    public bool AddFishingLine(FishingLine lineData)
    {
        if (lineData == null || IsFull) return false;
        
        FishingLineInventoryItem newLine = new FishingLineInventoryItem(lineData);
        items.Add(newLine);
        Debug.Log($"[Inventory] Added fishing line: {lineData.lineName}");
        OnItemAdded?.Invoke(newLine);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Add fishing hook to inventory
    /// </summary>
    public bool AddFishingHook(FishingHook hookData)
    {
        if (hookData == null || IsFull) return false;
        
        FishingHookInventoryItem newHook = new FishingHookInventoryItem(hookData);
        items.Add(newHook);
        Debug.Log($"[Inventory] Added fishing hook: {hookData.hookName}");
        OnItemAdded?.Invoke(newHook);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Add bait to inventory (stackable)
    /// </summary>
    public bool AddBait(string baitID, string baitName, BaitType baitType, Sprite icon, int quantity = 1, string description = "")
    {
        BaitInventoryItem existingBait = items.OfType<BaitInventoryItem>()
            .FirstOrDefault(b => b.baitType == baitType);
        
        if (existingBait != null)
        {
            int oldQuantity = existingBait.quantity;
            existingBait.AddQuantity(quantity);
            Debug.Log($"[Inventory] Stacked {quantity}x {baitName}. Total: {existingBait.quantity}");
            OnItemQuantityChanged?.Invoke(existingBait, oldQuantity);
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            if (IsFull) return false;
            
            BaitInventoryItem newBait = new BaitInventoryItem(baitID, baitName, baitType, icon, quantity, description);
            items.Add(newBait);
            Debug.Log($"[Inventory] Added {quantity}x {baitName}");
            OnItemAdded?.Invoke(newBait);
            OnInventoryChanged?.Invoke();
            return true;
        }
    }

    public List<RodBaseInventoryItem> GetAllRodBases() => items.OfType<RodBaseInventoryItem>().ToList();
    public List<ReelInventoryItem> GetAllReels() => items.OfType<ReelInventoryItem>().ToList();
    public List<FishingLineInventoryItem> GetAllFishingLines() => items.OfType<FishingLineInventoryItem>().ToList();
    public List<FishingHookInventoryItem> GetAllFishingHooks() => items.OfType<FishingHookInventoryItem>().ToList();
    public List<BaitInventoryItem> GetAllBait() => items.OfType<BaitInventoryItem>().ToList();

    public BaitInventoryItem GetBait(BaitType baitType)
    {
        return items.OfType<BaitInventoryItem>().FirstOrDefault(b => b.baitType == baitType);
    }
    
    /// <summary>
    /// Smart add — routes caught items to the correct inventory list by type
    /// </summary>
    public bool AddCaughtItem(CatchableItem caughtItem, int quantity = 1)
    {
        if (caughtItem == null) return false;

        switch (caughtItem.itemType1)
        {
            case ItemType.Fish:
                return AddFish(caughtItem, quantity);
            
            case ItemType.Plant:
            case ItemType.Metal:
            case ItemType.Magical:
            case ItemType.Other:
            case ItemType.Quest:
                return AddMaterial(caughtItem, quantity);
            
            default:
                Debug.LogWarning($"[Inventory] No handler for item type: {caughtItem.itemType1} ({caughtItem.itemName})");
                return false;
        }
    }

    /// <summary>
    /// Remove item from inventory
    /// </summary>
    public bool RemoveItem(string itemID, int quantity = 1)
    {
        InventoryItem item = items.FirstOrDefault(i => i.itemID == itemID);
        
        if (item == null)
        {
            Debug.LogWarning($"[Inventory] Item {itemID} not found!");
            return false;
        }
        
        if (item.IsStackable())
        {
            int oldQuantity = 0;
            bool success = false;
            
            if (item is FishInventoryItem fish)
            {
                oldQuantity = fish.quantity;
                success = fish.RemoveQuantity(quantity);
                if (!success) return false;
                if (fish.quantity <= 0) { items.Remove(item); OnItemRemoved?.Invoke(item); }
                else OnItemQuantityChanged?.Invoke(item, oldQuantity);
            }
            else if (item is MaterialInventoryItem material)
            {
                oldQuantity = material.quantity;
                success = material.RemoveQuantity(quantity);
                if (!success) return false;
                if (material.quantity <= 0) { items.Remove(item); OnItemRemoved?.Invoke(item); }
                else OnItemQuantityChanged?.Invoke(item, oldQuantity);
            }
            else if (item is BaitInventoryItem bait)
            {
                oldQuantity = bait.quantity;
                success = bait.RemoveQuantity(quantity);
                if (!success) return false;
                if (bait.quantity <= 0) { items.Remove(item); OnItemRemoved?.Invoke(item); }
                else OnItemQuantityChanged?.Invoke(item, oldQuantity);
            }
        }
        else
        {
            items.Remove(item);
            OnItemRemoved?.Invoke(item);
        }
        
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    /// <summary>
    /// Get item by ID
    /// </summary>
    public InventoryItem GetItem(string itemID)
    {
        return items.FirstOrDefault(i => i.itemID == itemID);
    }
    
    /// <summary>
    /// Check if player has item (and optionally a minimum quantity)
    /// </summary>
    public bool HasItem(string itemID, int minQuantity = 1)
    {
        InventoryItem item = GetItem(itemID);
        if (item == null) return false;
        
        if (item is FishInventoryItem fish) return fish.quantity >= minQuantity;
        if (item is MaterialInventoryItem material) return material.quantity >= minQuantity;
        if (item is BaitInventoryItem bait) return bait.quantity >= minQuantity;
        
        return true; // Gear just needs to exist
    }
    
    /// <summary>
    /// Get item quantity
    /// </summary>
    public int GetItemQuantity(string itemID)
    {
        InventoryItem item = GetItem(itemID);
        
        if (item is FishInventoryItem fish) return fish.quantity;
        if (item is MaterialInventoryItem material) return material.quantity;
        if (item is BaitInventoryItem bait) return bait.quantity;
        
        return item != null ? 1 : 0; // Gear counts as 1 if it exists
    }
    
    public List<FishInventoryItem> GetAllFish() => items.OfType<FishInventoryItem>().ToList();
    public List<MaterialInventoryItem> GetAllMaterials() => items.OfType<MaterialInventoryItem>().ToList();
    public List<GearInventoryItem> GetAllGear() => items.OfType<GearInventoryItem>().ToList();
    
    public List<InventoryItem> GetItemsByInventoryType(InventoryItemType type) =>
        items.Where(i => i.GetInventoryType() == type).ToList();
    
    public List<InventoryItem> GetItemsByItemType(ItemType type) =>
        items.Where(i => i.HasType(type)).ToList();
    
    public List<InventoryItem> GetItemsByRarity(ItemRarity rarity) =>
        items.Where(i => i.rarity == rarity).ToList();
    
    public List<InventoryItem> GetFavoriteItems() =>
        items.Where(i => i.isFavorite).ToList();
    
    public void SortByName()
    {
        items = items.OrderBy(i => i.DisplayName).ToList();
        OnInventoryChanged?.Invoke();
    }
    
    public void SortByRarity()
    {
        items = items.OrderByDescending(i => (int)i.rarity).ToList();
        OnInventoryChanged?.Invoke();
    }
    
    public void SortByDate()
    {
        items = items.OrderByDescending<InventoryItem, DateTime>(i => i.dateObtained).ToList();
        OnInventoryChanged?.Invoke();
    }
    
    public void Clear()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("[Inventory] Cleared all items");
    }
    
    public int GetTotalItemCount()
    {
        int total = 0;
        foreach (var item in items)
        {
            if (item is FishInventoryItem fish) total += fish.quantity;
            else if (item is MaterialInventoryItem material) total += material.quantity;
            else total += 1;
        }
        return total;
    }
    
    /// <summary>
    /// Used by SaveSystem only — inserts a pre-built InventoryItem directly
    /// </summary>
    public void AddRawItem(InventoryItem item)
    {
        items.Add(item);
    }
}

// ============================================
// INVENTORY ITEM TYPE ENUM
// ============================================

public enum InventoryItemType
{
    Fish,
    Material,
    Gear,
    Consumable,
    Misc
}

// ============================================
// INVENTORY ITEM BASE CLASS
// ============================================

[Serializable]
public abstract class InventoryItem
{
    [Header("Core Item Data")]
    public string itemID;
    public string itemName;
    public ItemRarity rarity;
    public Sprite icon;
    public string description;
    
    [Header("Classification")]
    public ItemType itemType1;
    public ItemType itemType2 = ItemType.Null;
    
    [Header("Consumable")]
    public bool isConsumable = false;
    
    [Header("Quantity")]
    public int quantity = 1;
    
    [Header("Instance Data")]
    public DateTime dateObtained;
    public string instanceID;
    
    [Header("Player Customization")]
    public string customName;
    public bool isFavorite;
    public string playerNotes;
    
    protected InventoryItem(string itemID, string itemName, ItemRarity rarity, Sprite icon, 
                           ItemType type1, ItemType type2 = ItemType.Null, string description = "", int quantity = 1)
    {
        this.itemID = itemID;
        this.itemName = itemName;
        this.rarity = rarity;
        this.icon = icon;
        this.itemType1 = type1;
        this.itemType2 = type2;
        this.description = description;
        this.quantity = IsStackable() ? quantity : Mathf.Min(quantity, 1);
        this.dateObtained = DateTime.Now;
        this.instanceID = Guid.NewGuid().ToString();
        this.customName = "";
        this.isFavorite = false;
        this.playerNotes = "";
    }
    
    public string DisplayName => string.IsNullOrEmpty(customName) ? itemName : customName;
    public bool IsDualType => itemType2 != ItemType.Null;
    
    public bool HasType(ItemType type) => itemType1 == type || itemType2 == type;
    public abstract bool IsStackable();
    public abstract InventoryItemType GetInventoryType();
    
    public virtual void AddQuantity(int amount)
    {
        if (IsStackable()) quantity += amount;
        else quantity = 1;
    }
    
    public virtual bool RemoveQuantity(int amount)
    {
        if (quantity >= amount) { quantity -= amount; return true; }
        return false;
    }
}

// ============================================
// CATCHABLE ITEM SUBTYPES
// ============================================

[System.Serializable]
public class FishInventoryItem : InventoryItem
{
    public FishWeightClass weightClass;
    public string speciesID;

    public FishInventoryItem(CatchableItem fishData, int quantity = 1)
        : base(fishData.itemID, fishData.itemName, fishData.rarity, fishData.icon,
            fishData.itemType1, fishData.itemType2, "", quantity)
    {
        this.weightClass = fishData.weightClass;
        this.speciesID   = fishData.speciesID;
        this.description = $"A {fishData.rarity} fish.";
    }

    public FishInventoryItem(string itemID, string itemName, ItemRarity rarity,
        ItemType type1, ItemType type2, string description, int quantity,
        FishWeightClass weightClass, string speciesID)
        : base(itemID, itemName, rarity, null, type1, type2, description, quantity)
    {
        this.weightClass = weightClass;
        this.speciesID   = speciesID;
    }

    public new string DisplayName => string.IsNullOrEmpty(customName)
        ? $"{weightClass} {itemName}"
        : customName;

    public override bool IsStackable() => true;
    public override InventoryItemType GetInventoryType() => InventoryItemType.Fish;
}

[System.Serializable]
public class MaterialInventoryItem : InventoryItem
{
    public MaterialInventoryItem(CatchableItem materialData, int quantity = 1)
        : base(materialData.itemID, materialData.itemName, materialData.rarity, materialData.icon,
            materialData.itemType1, materialData.itemType2, "", quantity)
    {
        this.description = $"A {materialData.rarity} {materialData.itemType1.ToString().ToLower()} material.";
    }

    public MaterialInventoryItem(string itemID, string itemName, ItemRarity rarity,
        ItemType type1, ItemType type2, string description, int quantity)
        : base(itemID, itemName, rarity, null, type1, type2, description, quantity) { }

    public override bool IsStackable() => true;
    public override InventoryItemType GetInventoryType() => InventoryItemType.Material;
}

// ============================================
// GEAR BASE CLASS
// ============================================

[System.Serializable]
public abstract class GearInventoryItem : InventoryItem
{
    [Header("Gear Stats")]
    public float resistanceBonus;
    public float reelingPowerBonus;
    public float lineStabilityBonus;
    public float luck;
    
    [Header("Gear Info")]
    public string gearType;
    
    protected GearInventoryItem(string itemID, string itemName, Sprite icon, string description,
        float resistance, float reelingPower, float stability, float luck, string gearType)
        : base(itemID, itemName, ItemRarity.Common, icon, ItemType.Null, ItemType.Null, description, 1)
    {
        this.resistanceBonus    = resistance;
        this.reelingPowerBonus  = reelingPower;
        this.lineStabilityBonus = stability;
        this.luck               = luck;
        this.gearType           = gearType;
        this.quantity           = 1;
    }
    
    public override bool IsStackable() => false;
    public override InventoryItemType GetInventoryType() => InventoryItemType.Gear;
    public override void AddQuantity(int amount) { quantity = 1; }
    
    public virtual string GetStatsDisplay()
    {
        return $"Resistance: +{resistanceBonus}\n" +
               $"Reeling Power: +{reelingPowerBonus}\n" +
               $"Stability: +{lineStabilityBonus}\n" +
               $"Luck: +{luck}";
    }
}

// ============================================
// GEAR SUBTYPES
// ============================================

[System.Serializable]
public class RodBaseInventoryItem : GearInventoryItem
{
    public float greenZoneEnd;
    public float redZoneStart;

    public RodBaseInventoryItem(RodBase rodData)
        : base(rodData.rodID, rodData.rodName, rodData.rodIcon, rodData.description,
            rodData.resistanceBonus, rodData.reelingPowerBonus, rodData.lineStabilityBonus,
            rodData.luck, "Rod Base")
    {
        this.greenZoneEnd = rodData.greenZoneEnd;
        this.redZoneStart = rodData.redZoneStart;
    }

    public RodBaseInventoryItem(string itemID, string itemName, float resistance,
        float reelingPower, float stability, float luck, float greenZoneEnd, float redZoneStart)
        : base(itemID, itemName, null, "", resistance, reelingPower, stability, luck, "Rod Base")
    {
        this.greenZoneEnd = greenZoneEnd;
        this.redZoneStart = redZoneStart;
    }
}

[System.Serializable]
public class ReelInventoryItem : GearInventoryItem
{
    public int visibleButtonCount;

    public ReelInventoryItem(Reel reelData)
        : base(reelData.reelID, reelData.reelName, reelData.reelIcon, reelData.description,
            reelData.resistanceBonus, reelData.reelingPowerBonus, reelData.lineStabilityBonus,
            reelData.luck, "Reel")
    {
        this.visibleButtonCount = reelData.visibleButtonCount;
    }

    public ReelInventoryItem(string itemID, string itemName, float resistance,
        float reelingPower, float stability, float luck, int visibleButtonCount)
        : base(itemID, itemName, null, "", resistance, reelingPower, stability, luck, "Reel")
    {
        this.visibleButtonCount = visibleButtonCount;
    }

    public override string GetStatsDisplay() =>
        base.GetStatsDisplay() + $"\n\nVisible Buttons: {visibleButtonCount}";
}

[System.Serializable]
public class FishingLineInventoryItem : GearInventoryItem
{
    public float lineLength;

    public FishingLineInventoryItem(FishingLine lineData)
        : base(lineData.lineID, lineData.lineName, lineData.lineIcon, lineData.description,
            lineData.resistanceBonus, lineData.reelingPowerBonus, lineData.lineStabilityBonus,
            lineData.luck, "Fishing Line")
    {
        this.lineLength = lineData.lineLength;
    }

    public FishingLineInventoryItem(string itemID, string itemName, float resistance,
        float reelingPower, float stability, float luck, float lineLength)
        : base(itemID, itemName, null, "", resistance, reelingPower, stability, luck, "Fishing Line")
    {
        this.lineLength = lineLength;
    }

    public override string GetStatsDisplay() =>
        base.GetStatsDisplay() + $"\n\nLine Length: {lineLength}m";
}

[System.Serializable]
public class FishingHookInventoryItem : GearInventoryItem
{
    public List<ItemType> preferredTypes = new List<ItemType>();
    public float typeWeightBonus;
    public int baitSlots;

    public FishingHookInventoryItem(FishingHook hookData)
        : base(hookData.hookID, hookData.hookName, hookData.hookIcon, hookData.description,
            hookData.resistanceBonus, hookData.reelingPowerBonus, hookData.lineStabilityBonus,
            hookData.luck, "Fishing Hook")
    {
        this.preferredTypes  = new List<ItemType>(hookData.preferredTypes);
        this.typeWeightBonus = hookData.typeWeightBonus;
        this.baitSlots       = hookData.baitSlots;
    }

    public FishingHookInventoryItem(string itemID, string itemName, float resistance,
        float reelingPower, float stability, float luck,
        List<ItemType> preferredTypes, float typeWeightBonus, int baitSlots)
        : base(itemID, itemName, null, "", resistance, reelingPower, stability, luck, "Fishing Hook")
    {
        this.preferredTypes  = preferredTypes ?? new List<ItemType>();
        this.typeWeightBonus = typeWeightBonus;
        this.baitSlots       = baitSlots;
    }

    public override string GetStatsDisplay()
    {
        string preferred = preferredTypes.Count > 0 ? string.Join(", ", preferredTypes) : "None";
        return base.GetStatsDisplay() +
               $"\n\nBait Slots: {baitSlots}" +
               $"\nPreferred Types: {preferred}" +
               $"\nType Bonus: x{typeWeightBonus}";
    }
}

[System.Serializable]
public class BaitInventoryItem : GearInventoryItem
{
    public BaitType baitType;

    public BaitInventoryItem(string baitID, string baitName, BaitType baitType,
        Sprite icon, int quantity = 1, string description = "")
        : base(baitID, baitName, icon, description, 0f, 0f, 0f, 0f, "Bait")
    {
        this.baitType = baitType;
        this.quantity = quantity;
    }

    public override bool IsStackable() => true;
    public override void AddQuantity(int amount) => quantity += amount;

    public override string GetStatsDisplay() =>
        $"Bait Type: {baitType}\nQuantity: {quantity}\n\n{description}";
}

// ============================================
// CONSUMABLE
// ============================================

[System.Serializable]
public class ConsumableInventoryItem : InventoryItem
{
    public string effectDescription;
    
    public ConsumableInventoryItem(string itemID, string itemName, Sprite icon, string description, string effectDescription, int quantity = 1)
        : base(itemID, itemName, ItemRarity.Common, icon, ItemType.Null, ItemType.Null, description, quantity)
    {
        this.isConsumable = true;
        this.effectDescription = effectDescription;
    }
    
    public override bool IsStackable() => true;
    public override InventoryItemType GetInventoryType() => InventoryItemType.Consumable;
}

// ============================================
// EQUIPPED GEAR INVENTORY
// ============================================

[System.Serializable]
public class EquippedGearInventory
{
    [Header("Equipped Gear (Required)")]
    [SerializeField] private RodBaseInventoryItem equippedRodBase;
    [SerializeField] private ReelInventoryItem equippedReel;
    [SerializeField] private FishingLineInventoryItem equippedLine;
    [SerializeField] private FishingHookInventoryItem equippedHook;
    
    [Header("Equipped Bait (Optional)")]
    [SerializeField] private BaitInventoryItem equippedBait;
    [SerializeField] private int equippedBaitQuantity = 0;
    
    public event Action OnGearChanged;
    public event Action<BaitInventoryItem, int> OnBaitChanged;
    
    public RodBaseInventoryItem RodBase => equippedRodBase;
    public ReelInventoryItem Reel => equippedReel;
    public FishingLineInventoryItem Line => equippedLine;
    public FishingHookInventoryItem Hook => equippedHook;
    public BaitInventoryItem Bait => equippedBait;
    public int BaitQuantity => equippedBaitQuantity;
    public BaitType CurrentBaitType => equippedBait?.baitType ?? BaitType.None;
    
    public void EquipRodBase(RodBaseInventoryItem rodBase)
    {
        if (rodBase == null) { Debug.LogWarning("[EquippedGear] Cannot equip null rod base!"); return; }
        equippedRodBase = rodBase;
        OnGearChanged?.Invoke();
        Debug.Log($"[EquippedGear] Equipped rod base: {rodBase.itemName}");
    }
    
    public void EquipReel(ReelInventoryItem reel)
    {
        if (reel == null) { Debug.LogWarning("[EquippedGear] Cannot equip null reel!"); return; }
        equippedReel = reel;
        OnGearChanged?.Invoke();
        Debug.Log($"[EquippedGear] Equipped reel: {reel.itemName}");
    }
    
    public void EquipLine(FishingLineInventoryItem line)
    {
        if (line == null) { Debug.LogWarning("[EquippedGear] Cannot equip null fishing line!"); return; }
        equippedLine = line;
        OnGearChanged?.Invoke();
        Debug.Log($"[EquippedGear] Equipped line: {line.itemName}");
    }
    
    public void EquipHook(FishingHookInventoryItem hook)
    {
        if (hook == null) { Debug.LogWarning("[EquippedGear] Cannot equip null hook!"); return; }
        equippedHook = hook;
        OnGearChanged?.Invoke();
        Debug.Log($"[EquippedGear] Equipped hook: {hook.itemName}");
    }
    
    public bool EquipBait(BaitInventoryItem bait, int quantity)
    {
        if (bait == null) { Debug.LogWarning("[EquippedGear] Cannot equip null bait!"); return false; }
        
        if (bait.quantity < quantity)
        {
            Debug.LogWarning($"[EquippedGear] Not enough bait! Have {bait.quantity}, trying to equip {quantity}");
            return false;
        }
        
        int maxBaitSlots = GetMaxBaitSlots();
        if (quantity > maxBaitSlots)
        {
            Debug.LogWarning($"[EquippedGear] Capping bait at hook limit: {maxBaitSlots}");
            quantity = maxBaitSlots;
        }
        
        equippedBait = bait;
        equippedBaitQuantity = quantity;
        OnBaitChanged?.Invoke(equippedBait, equippedBaitQuantity);
        Debug.Log($"[EquippedGear] Equipped {quantity}x {bait.itemName}");
        return true;
    }
    
    public void UnequipBait()
    {
        equippedBait = null;
        equippedBaitQuantity = 0;
        OnBaitChanged?.Invoke(null, 0);
        Debug.Log("[EquippedGear] Unequipped bait");
    }
    
    public void ConsumeBait()
    {
        if (equippedBait == null || equippedBaitQuantity <= 0) return;
        
        equippedBaitQuantity--;
        
        if (equippedBaitQuantity <= 0)
        {
            Debug.Log($"[EquippedGear] Ran out of {equippedBait.itemName}!");
            UnequipBait();
        }
        else
        {
            OnBaitChanged?.Invoke(equippedBait, equippedBaitQuantity);
        }
    }
    
    public bool IsComplete() =>
        equippedRodBase != null && equippedReel != null && equippedLine != null && equippedHook != null;
    
    public float GetTotalRodResistance()
    {
        float total = 0f;
        if (equippedRodBase != null) total += equippedRodBase.resistanceBonus;
        if (equippedReel != null)    total += equippedReel.resistanceBonus;
        if (equippedLine != null)    total += equippedLine.resistanceBonus;
        if (equippedHook != null)    total += equippedHook.resistanceBonus;
        return total;
    }
    
    public float GetTotalReelingPower()
    {
        float total = 0f;
        if (equippedRodBase != null) total += equippedRodBase.reelingPowerBonus;
        if (equippedReel != null)    total += equippedReel.reelingPowerBonus;
        if (equippedLine != null)    total += equippedLine.reelingPowerBonus;
        if (equippedHook != null)    total += equippedHook.reelingPowerBonus;
        return total;
    }
    
    public float GetTotalLineStability()
    {
        float total = 0f;
        if (equippedRodBase != null) total += equippedRodBase.lineStabilityBonus;
        if (equippedReel != null)    total += equippedReel.lineStabilityBonus;
        if (equippedLine != null)    total += equippedLine.lineStabilityBonus;
        if (equippedHook != null)    total += equippedHook.lineStabilityBonus;
        return total;
    }
    
    public float GetTotalLuck()
    {
        float total = 0f;
        if (equippedRodBase != null) total += equippedRodBase.luck;
        if (equippedReel != null)    total += equippedReel.luck;
        if (equippedLine != null)    total += equippedLine.luck;
        if (equippedHook != null)    total += equippedHook.luck;
        return total;
    }
    
    public int GetVisibleButtonCount() => equippedReel != null ? equippedReel.visibleButtonCount : 4;
    public float GetLineLength() => equippedLine != null ? equippedLine.lineLength : 100f;
    public int GetMaxBaitSlots() => equippedHook != null ? equippedHook.baitSlots : 1;
    
    public float GetTypeWeightMultiplier(ItemType type)
    {
        if (equippedHook == null) return 1f;
        return equippedHook.preferredTypes.Contains(type) ? equippedHook.typeWeightBonus : 1f;
    }
    
    public string GetLoadoutName() =>
        IsComplete() ? $"{equippedRodBase.itemName} Setup" : "Incomplete Loadout";
    
    public string GetStatsSummary()
    {
        if (!IsComplete()) return "Incomplete gear!";
    
        return $"=== EQUIPPED GEAR ===\n" +
               $"Rod Resistance: {GetTotalRodResistance()}\n" +
               $"Reeling Power: {GetTotalReelingPower()}\n" +
               $"Line Stability: {GetTotalLineStability()}\n" +
               $"Luck: {GetTotalLuck()}\n" +
               $"Line Length: {GetLineLength()}m\n" +
               $"Visible Buttons: {GetVisibleButtonCount()}\n" +
               $"Bait Slots: {GetMaxBaitSlots()}\n" +
               $"Current Bait: {(equippedBait != null ? $"{equippedBait.itemName} ({equippedBaitQuantity}x)" : "None")}";
    }
}