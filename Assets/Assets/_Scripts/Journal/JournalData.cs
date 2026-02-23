using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class JournalData
{
    [SerializeField] private List<JournalEntry> entries = new List<JournalEntry>();
    private Dictionary<string, JournalEntry> entryLookup = new Dictionary<string, JournalEntry>();

    public void Initialize()
    {
        RebuildLookup();
    }

    private void RebuildLookup()
    {
        entryLookup.Clear();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.journalKey))
                entryLookup[entry.journalKey] = entry;
        }
    }

    /// <summary>
    /// Record an item acquisition. Fish are grouped by speciesID, all others by itemID.
    /// </summary>
    public void RecordAcquisition(Item item, string locationID, string locationName, 
        AcquisitionMethod method, FishWeightClass? weightClass = null)
    {
        if (!item.journalSettings.createsJournalEntry) return;

        // Determine the journal key
        string journalKey = item.itemID; // Default: unique per item
        string displayName = item.itemName;

        if (item is CatchableItem catchable && catchable.HasType(ItemType.Fish) 
            && !string.IsNullOrEmpty(catchable.speciesID))
        {
            journalKey = catchable.speciesID; // Fish group by species
            // Strip size prefix for the journal title - just use species name
            displayName = catchable.itemName; 
        }

        if (!entryLookup.ContainsKey(journalKey))
        {
            CreateNewEntry(item, journalKey, displayName);
        }

        JournalEntry entry = entryLookup[journalKey];
        bool wasNewDiscovery = !entry.discovered;

        entry.RecordAcquisition(locationID, locationName, method, weightClass);

        if (wasNewDiscovery)
            Debug.Log($"[Journal] ✨ NEW DISCOVERY: {displayName}!");
        else
            Debug.Log($"[Journal] Recorded: {displayName} (Total: {entry.totalAcquired})");
    }

    private void CreateNewEntry(Item item, string journalKey, string displayName)
    {
        JournalEntry newEntry = new JournalEntry(item, journalKey, displayName);
        entries.Add(newEntry);
        entryLookup[journalKey] = newEntry;
    }

    public List<JournalEntry> GetDiscoveredEntries() => entries.Where(e => e.discovered).ToList();

    public JournalEntry GetEntry(string journalKey)
    {
        if (entryLookup.TryGetValue(journalKey, out JournalEntry entry) && entry.discovered)
            return entry;
        return null;
    }

    public bool IsDiscovered(string journalKey) =>
        entryLookup.TryGetValue(journalKey, out JournalEntry entry) && entry.discovered;

    public List<JournalEntry> GetEntriesByType(ItemType type) =>
        entries.Where(e => e.discovered && e.itemType1 == type).ToList();

    public List<JournalEntry> GetEntriesByRarity(ItemRarity rarity) =>
        entries.Where(e => e.discovered && e.rarity == rarity).ToList();

    public List<JournalEntry> GetAllEntries() => entries.ToList();

    public void LoadFromSaveData(List<JournalEntrySaveData> savedEntries)
    {
        entries.Clear();
        entryLookup.Clear();

        foreach (var save in savedEntries)
        {
            Enum.TryParse(save.itemType1String, out ItemType type1);
            Enum.TryParse(save.itemType2String, out ItemType type2);
            Enum.TryParse(save.rarityString,    out ItemRarity rarity);

            JournalEntry entry = new JournalEntry(save.journalKey, save.itemID, save.itemName,
                save.description, type1, type2, rarity,
                save.tracksWeightClass, save.tracksCount);

            entry.discovered        = save.discovered;
            entry.discoveredDate    = save.discoveredDate;
            entry.totalAcquired     = save.totalAcquired;
            entry.hasBiggestWeightClass = save.hasBiggestWeightClass;

            if (save.hasBiggestWeightClass && Enum.TryParse(save.biggestWeightClassString, 
                    out FishWeightClass wc))
                entry.biggestWeightClass = wc;

            foreach (var acqSave in save.acquisitions)
            {
                Enum.TryParse(acqSave.methodString, out AcquisitionMethod method);
                FishWeightClass? weightClass = null;
                if (acqSave.hasWeightClass && Enum.TryParse(acqSave.weightClassString, 
                        out FishWeightClass acqWc))
                    weightClass = acqWc;

                entry.acquisitions.Add(new AcquisitionRecord(acqSave.locationID, 
                    acqSave.locationName, method, weightClass)
                {
                    timestamp = acqSave.timestamp
                });
            }

            entries.Add(entry);
            entryLookup[entry.journalKey] = entry;
        }
    }
    
    public int GetDiscoveredCount() => entries.Count(e => e.discovered);
    public int GetTotalSlots() => entries.Count;
    public int GetDiscoveryPercentage() => entries.Count > 0 ? (GetDiscoveredCount() * 100 / entries.Count) : 0;
}