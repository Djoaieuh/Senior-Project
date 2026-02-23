using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class AcquisitionRecord
{
    public string locationID;
    public string locationName;
    public AcquisitionMethod method;
    public DateTime timestamp;
    public bool hasWeightClass;
    public FishWeightClass weightClass; // Only meaningful for fish

    public AcquisitionRecord(string locationID, string locationName, 
        AcquisitionMethod method, FishWeightClass? weightClass = null)
    {
        this.locationID = locationID;
        this.locationName = locationName;
        this.method = method;
        this.timestamp = DateTime.Now;
        this.hasWeightClass = weightClass.HasValue;
        this.weightClass = weightClass ?? FishWeightClass.Modest;
    }
}

public enum AcquisitionMethod
{
    Fished,
    Gathered,
    Bought,
    Crafted,
    Found,
    Rewarded
}

[System.Serializable]
public class JournalEntry
{
    // Core identity
    public string journalKey;       // speciesID for fish, itemID for others
    public string itemID;           // Original item ID (kept for reference)
    public string itemName;
    public string description;
    public ItemType itemType1;
    public ItemType itemType2;
    public ItemRarity rarity;
    public Sprite icon;

    // Discovery
    public bool discovered = false;
    public DateTime discoveredDate;

    // Acquisition tracking
    public List<AcquisitionRecord> acquisitions = new List<AcquisitionRecord>();

    public bool tracksWeightClass;  // Renamed from tracksWeight
    public bool tracksCount;

    public int totalAcquired = 0;

    // Biggest size class caught (replaces biggestWeight float)
    public bool hasBiggestWeightClass = false;
    public FishWeightClass biggestWeightClass;

    public GameObject customPageLayout;

    public JournalEntry(Item item, string journalKey, string displayName)
    {
        this.journalKey = journalKey;
        this.itemID = item.itemID;
        this.itemName = displayName;
        this.description = item.description;
        this.itemType1 = item.itemType1;
        this.itemType2 = item.itemType2;
        this.rarity = item.rarity;
        this.icon = item.icon;

        this.tracksWeightClass = item.journalSettings.tracksWeight; // reuses existing toggle
        this.tracksCount = item.journalSettings.tracksAcquisitionCount;
        this.customPageLayout = item.journalSettings.customPageLayout;
    }
    
    // Add this second constructor to JournalEntry, keep the existing one
    public JournalEntry(string journalKey, string itemID, string itemName, string description,
        ItemType type1, ItemType type2, ItemRarity rarity, bool tracksWeightClass, bool tracksCount)
    {
        this.journalKey        = journalKey;
        this.itemID            = itemID;
        this.itemName          = itemName;
        this.description       = description;
        this.itemType1         = type1;
        this.itemType2         = type2;
        this.rarity            = rarity;
        this.tracksWeightClass = tracksWeightClass;
        this.tracksCount       = tracksCount;
    }

    public void RecordAcquisition(string locationID, string locationName, 
        AcquisitionMethod method, FishWeightClass? weightClass = null)
    {
        if (!discovered)
        {
            discovered = true;
            discoveredDate = DateTime.Now;
        }

        acquisitions.Add(new AcquisitionRecord(locationID, locationName, method, weightClass));

        if (tracksCount) totalAcquired++;

        // Track biggest weight class (higher enum value = bigger fish)
        if (tracksWeightClass && weightClass.HasValue)
        {
            if (!hasBiggestWeightClass || weightClass.Value > biggestWeightClass)
            {
                biggestWeightClass = weightClass.Value;
                hasBiggestWeightClass = true;
            }
        }
    }

    public List<string> GetUniqueLocations()
    {
        HashSet<string> unique = new HashSet<string>();
        foreach (var acq in acquisitions)
            if (!string.IsNullOrEmpty(acq.locationName))
                unique.Add(acq.locationName);
        return new List<string>(unique);
    }

    public int GetCountFromLocation(string locationID)
    {
        int count = 0;
        foreach (var acq in acquisitions)
            if (acq.locationID == locationID) count++;
        return count;
    }
}