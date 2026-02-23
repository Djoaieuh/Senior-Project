using UnityEngine;
using System.Collections.Generic;

public enum LocationType
{
    FishingSpot,    // Loads a fishing scene
    Shop,           // Opens a shop (not yet implemented)
    Basic           // Dialogue / quest / anything else (not yet implemented)
}

public enum UnlockRequirementType
{
    None,           // Always unlocked
    MoneyCost,      // Player must pay to unlock
    QuestFlag,      // A specific flag must be true
    LocationVisited // Another location must be unlocked first
}

/// <summary>
/// A single unlock condition. You can stack multiple requirements on one location.
/// ALL requirements must be met for the location to be unlocked.
/// </summary>
[System.Serializable]
public class UnlockRequirement
{
    public UnlockRequirementType type = UnlockRequirementType.None;

    [Tooltip("MoneyCost: how much gold the player must pay")]
    public int moneyCost = 0;

    [Tooltip("QuestFlag: the flag name that must be true (e.g. 'completed_tutorial')")]
    public string questFlag = "";

    [Tooltip("QuestFlag: which locationID owns the flag (leave empty = global flag)")]
    public string questFlagLocationID = "";

    [Tooltip("LocationVisited: the locationID that must already be unlocked")]
    public string requiredLocationID = "";

    /// <summary>
    /// Returns true if this single requirement is currently satisfied.
    /// Note: MoneyCost is checked separately (it needs to deduct money), 
    /// so here it just checks if the player CAN afford it.
    /// </summary>
    public bool IsMet(MapData mapData)
    {
        switch (type)
        {
            case UnlockRequirementType.None:
                return true;

            case UnlockRequirementType.MoneyCost:
                return GameManager._instance != null &&
                       GameManager._instance.CanAfford(moneyCost);

            case UnlockRequirementType.QuestFlag:
                if (string.IsNullOrEmpty(questFlag)) return true;
                return mapData.GetLocationFlag(questFlagLocationID, questFlag, false);

            case UnlockRequirementType.LocationVisited:
                if (string.IsNullOrEmpty(requiredLocationID)) return true;
                return mapData.IsLocationUnlocked(requiredLocationID);

            default:
                return false;
        }
    }

    /// <summary>
    /// Human-readable description of this requirement, shown in map UI.
    /// </summary>
    public string GetDescription()
    {
        switch (type)
        {
            case UnlockRequirementType.None:           return "";
            case UnlockRequirementType.MoneyCost:      return $"Cost: {moneyCost}g";
            case UnlockRequirementType.QuestFlag:      return $"Requires: {questFlag}";
            case UnlockRequirementType.LocationVisited:return $"Requires visiting: {requiredLocationID}";
            default:                                   return "";
        }
    }
}

[CreateAssetMenu(fileName = "New Location", menuName = "Fishing Game/Location Data")]
public class LocationData : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Unique location identifier - must match FishingSpotData.locationID for fishing spots")]
    public string locationID;

    [Tooltip("Display name shown on the map and UI")]
    public string locationName;

    [Tooltip("Short description shown in the location panel")]
    [TextArea(2, 4)]
    public string description;

    // ============================================
    // LOCATION TYPE
    // ============================================

    [Header("Location Type")]
    public LocationType locationType;

    [Tooltip("FishingSpot/Shop only: exact Unity scene name to load (must match Build Settings)")]
    public string sceneName;

    // ============================================
    // UNLOCK REQUIREMENTS
    // ============================================

    [Header("Unlock Requirements")]
    [Tooltip("If empty, location is unlocked from the start")]
    public List<UnlockRequirement> unlockRequirements = new List<UnlockRequirement>();

    // ============================================
    // VISUALS
    // ============================================

    [Header("Visuals")]
    [Tooltip("Icon shown on the map pin")]
    public Sprite locationIcon;

    [Tooltip("Preview image shown in the location info panel")]
    public Sprite previewImage;

    // ============================================
    // SHOP DATA (not yet implemented)
    // ============================================

    // ============================================
    // HELPERS
    // ============================================

    /// <summary>
    /// Returns true if the location is already unlocked in save data.
    /// </summary>
    public bool IsUnlocked(MapData mapData)
    {
        return mapData.IsLocationUnlocked(locationID);
    }

    /// <summary>
    /// Returns true if ALL unlock requirements are currently met.
    /// </summary>
    public bool AreRequirementsMet(MapData mapData)
    {
        if (unlockRequirements == null || unlockRequirements.Count == 0)
            return true;

        foreach (var req in unlockRequirements)
        {
            if (!req.IsMet(mapData))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns true if location is unlocked OR has no requirements (free to unlock).
    /// </summary>
    public bool IsAvailable(MapData mapData)
    {
        return IsUnlocked(mapData) || AreRequirementsMet(mapData);
    }

    /// <summary>
    /// Attempts to unlock this location. Deducts money if needed.
    /// Returns true if successfully unlocked.
    /// </summary>
    public bool TryUnlock(MapData mapData)
    {
        if (IsUnlocked(mapData)) return true;
        if (!AreRequirementsMet(mapData)) return false;

        // Deduct money costs
        foreach (var req in unlockRequirements)
        {
            if (req.type == UnlockRequirementType.MoneyCost && req.moneyCost > 0)
            {
                if (!GameManager._instance.RemoveMoney(req.moneyCost))
                    return false;
            }
        }

        mapData.UnlockLocation(locationID);
        return true;
    }

    /// <summary>
    /// Returns all requirement descriptions for display in UI.
    /// </summary>
    public List<string> GetRequirementDescriptions()
    {
        var result = new List<string>();
        if (unlockRequirements == null) return result;

        foreach (var req in unlockRequirements)
        {
            string desc = req.GetDescription();
            if (!string.IsNullOrEmpty(desc))
                result.Add(desc);
        }
        return result;
    }
}