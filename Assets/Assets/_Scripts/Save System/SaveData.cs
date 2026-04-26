using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Pure data container - no Unity types, fully JSON serializable.
/// This is what gets written to disk.
/// </summary>
[Serializable]
public class SaveData
{
    public int schemaVersion = 1; // Bump this if you change the format
    public DateTime lastSaved;

    // Player
    public int money;
    
    public double globalTimerSeconds = 0.0;

    // Location
    public string currentLocationID;
    public string currentSceneName;
    
    // Shops
    public List<ShopSaveData> shopStates = new List<ShopSaveData>();

    // Inventory - TypeNameHandling handles polymorphism
    [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
    public List<InventoryItemSaveData> inventoryItems = new List<InventoryItemSaveData>();

    // Equipped gear (stored as instanceIDs, resolved on load)
    public EquippedGearSaveData equippedGear = new EquippedGearSaveData();

    // Journal
    public List<JournalEntrySaveData> journalEntries = new List<JournalEntrySaveData>();

    // Map
    public List<string> unlockedLocationIDs = new List<string>();
    public List<LocationFlagSaveData> locationFlags = new List<LocationFlagSaveData>();
}

// ============================================
// INVENTORY SAVE DATA
// ============================================

/// <summary>
/// Base class for all inventory save data.
/// Subclasses are used so Newtonsoft can restore the right type.
/// </summary>
[Serializable]
public abstract class InventoryItemSaveData
{
    public string itemID;
    public string itemName;
    public string instanceID;
    public int quantity;
    public string rarityString;     // Store as string - enum values can shift
    public string itemType1String;
    public string itemType2String;
    public string description;
    public string customName;
    public bool isFavorite;
    public string playerNotes;
    public DateTime dateObtained;
}

[Serializable]
public class FishItemSaveData : InventoryItemSaveData
{
    public string weightClassString; // FishWeightClass as string
    public string speciesID;
}

[Serializable]
public class MaterialItemSaveData : InventoryItemSaveData
{
    // No extra fields needed currently - easy to extend later
}

[Serializable]
public class RodBaseItemSaveData : InventoryItemSaveData
{
    public float greenZoneEnd;
    public float redZoneStart;
    public float resistanceBonus;
    public float reelingPowerBonus;
    public float lineStabilityBonus;
    public float luck;
}

[Serializable]
public class ReelItemSaveData : InventoryItemSaveData
{
    public int visibleButtonCount;
    public float resistanceBonus;
    public float reelingPowerBonus;
    public float lineStabilityBonus;
    public float luck;
}

[Serializable]
public class FishingLineItemSaveData : InventoryItemSaveData
{
    public float lineLength;
    public float resistanceBonus;
    public float reelingPowerBonus;
    public float lineStabilityBonus;
    public float luck;
}

[Serializable]
public class FishingHookItemSaveData : InventoryItemSaveData
{
    public List<string> preferredTypeStrings = new List<string>();
    public float typeWeightBonus;
    public int baitSlots;
    public float resistanceBonus;
    public float reelingPowerBonus;
    public float lineStabilityBonus;
    public float luck;
}

[Serializable]
public class BaitItemSaveData : InventoryItemSaveData
{
    public string baitTypeString;
}

[Serializable]
public class ConsumableItemSaveData : InventoryItemSaveData
{
    public string effectDescription;
}

// ============================================
// EQUIPPED GEAR SAVE DATA
// ============================================

[Serializable]
public class EquippedGearSaveData
{
    // Stored as instanceIDs - matched back to inventory on load
    public string rodBaseInstanceID;
    public string reelInstanceID;
    public string lineInstanceID;
    public string hookInstanceID;

    // Bait stored by itemID (stackable, no unique instance)
    public string baitItemID;
    public int baitQuantity;
}

// ============================================
// JOURNAL SAVE DATA
// ============================================

[Serializable]
public class JournalEntrySaveData
{
    public string journalKey;
    public string itemID;
    public string itemName;
    public string description;
    public string itemType1String;
    public string itemType2String;
    public string rarityString;

    public bool discovered;
    public DateTime discoveredDate;

    public int totalAcquired;
    public bool tracksWeightClass;
    public bool tracksCount;

    public bool hasBiggestWeightClass;
    public string biggestWeightClassString;

    public List<AcquisitionRecordSaveData> acquisitions = new List<AcquisitionRecordSaveData>();
}

[Serializable]
public class AcquisitionRecordSaveData
{
    public string locationID;
    public string locationName;
    public string methodString;
    public DateTime timestamp;
    public bool hasWeightClass;
    public string weightClassString;
}

// ============================================
// MAP SAVE DATA
// ============================================

[Serializable]
public class LocationFlagSaveData
{
    public string locationID;
    public List<string> flagKeys   = new List<string>();
    public List<bool>   flagValues = new List<bool>();
}