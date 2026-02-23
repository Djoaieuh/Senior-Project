using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LocationFlags
{
    public string locationID;
    public System.DateTime unlockedDate;
    public Dictionary<string, bool> questFlags = new Dictionary<string, bool>();

    public LocationFlags(string locationID)
    {
        this.locationID  = locationID;
        this.unlockedDate = System.DateTime.Now;
    }

    public void SetFlag(string flagName, bool value) => questFlags[flagName] = value;

    public bool GetFlag(string flagName, bool defaultValue = false)
        => questFlags.TryGetValue(flagName, out bool value) ? value : defaultValue;
}

[System.Serializable]
public class MapData
{
    [SerializeField] private List<string> unlockedLocationIDs = new List<string>();

    // Serializable dictionary workaround for Unity Inspector
    [SerializeField] private List<string>        locationFlagKeys   = new List<string>();
    [SerializeField] private List<LocationFlags> locationFlagValues = new List<LocationFlags>();

    private Dictionary<string, LocationFlags> locationProgress = new Dictionary<string, LocationFlags>();

    // ============================================
    // INIT
    // ============================================

    public void Initialize()
    {
        RebuildLocationProgress();
    }

    private void RebuildLocationProgress()
    {
        locationProgress.Clear();
        for (int i = 0; i < locationFlagKeys.Count && i < locationFlagValues.Count; i++)
            locationProgress[locationFlagKeys[i]] = locationFlagValues[i];
    }

    private void SyncSerializedData()
    {
        locationFlagKeys.Clear();
        locationFlagValues.Clear();
        foreach (var kvp in locationProgress)
        {
            locationFlagKeys.Add(kvp.Key);
            locationFlagValues.Add(kvp.Value);
        }
    }

    // ============================================
    // UNLOCK
    // ============================================

    public void UnlockLocation(string locationID)
    {
        if (!unlockedLocationIDs.Contains(locationID))
        {
            unlockedLocationIDs.Add(locationID);
            locationProgress[locationID] = new LocationFlags(locationID);
            SyncSerializedData();
            Debug.Log($"[MapData] Unlocked location: {locationID}");
        }
    }

    public bool IsLocationUnlocked(string locationID)
        => unlockedLocationIDs.Contains(locationID);

    public List<string> GetUnlockedLocations()
        => new List<string>(unlockedLocationIDs);

    public int GetUnlockedCount() => unlockedLocationIDs.Count;

    // ============================================
    // FLAGS
    // ============================================

    public void SetLocationFlag(string locationID, string flagName, bool value)
    {
        if (!locationProgress.ContainsKey(locationID))
        {
            Debug.LogWarning($"[MapData] Location {locationID} not found in progress - creating entry");
            locationProgress[locationID] = new LocationFlags(locationID);
        }
        locationProgress[locationID].SetFlag(flagName, value);
        SyncSerializedData();
        Debug.Log($"[MapData] {locationID}.{flagName} = {value}");
    }

    public bool GetLocationFlag(string locationID, string flagName, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(locationID)) 
        {
            // Global flag - check under a special "_global" key
            locationID = "_global";
        }
        if (!locationProgress.ContainsKey(locationID)) return defaultValue;
        return locationProgress[locationID].GetFlag(flagName, defaultValue);
    }

    // ============================================
    // SAVE / LOAD
    // ============================================

    public void LoadFromSaveData(List<string> savedUnlockedIDs, List<LocationFlagSaveData> savedFlags)
    {
        unlockedLocationIDs.Clear();
        if (savedUnlockedIDs != null)
            unlockedLocationIDs.AddRange(savedUnlockedIDs);

        locationProgress.Clear();
        if (savedFlags != null)
        {
            foreach (var flagData in savedFlags)
            {
                var flags = new LocationFlags(flagData.locationID);
                for (int i = 0; i < flagData.flagKeys.Count && i < flagData.flagValues.Count; i++)
                    flags.questFlags[flagData.flagKeys[i]] = flagData.flagValues[i];
                locationProgress[flagData.locationID] = flags;
            }
        }

        SyncSerializedData();
        Debug.Log($"[MapData] Loaded {unlockedLocationIDs.Count} unlocked locations");
    }

    public (List<string> ids, List<LocationFlagSaveData> flags) ToSaveData()
    {
        var ids = new List<string>(unlockedLocationIDs);

        var flags = new List<LocationFlagSaveData>();
        foreach (var kvp in locationProgress)
        {
            var flagData = new LocationFlagSaveData { locationID = kvp.Key };
            foreach (var f in kvp.Value.questFlags)
            {
                flagData.flagKeys.Add(f.Key);
                flagData.flagValues.Add(f.Value);
            }
            flags.Add(flagData);
        }

        return (ids, flags);
    }
}