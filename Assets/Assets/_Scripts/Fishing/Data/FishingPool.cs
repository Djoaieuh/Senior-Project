using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Fishing Pool", menuName = "Fishing Game/Fishing Pool")]
public class FishingPool : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Unique identifier for this fishing pool")]
    public string poolID;
    
    [Header("Available Items")]
    [Tooltip("All catchable items available in this pool")]
    public List<CatchableItem> availableItems = new List<CatchableItem>();
    
    [Header("Fishing Distance Settings")]
    [Tooltip("Maximum distance before line breaks (defines difficulty/scale of this location)")]
    public float maxFishingDistance = 100f;
    
    // REMOVED: startingFishingDistance (now always 50% of max)
    // REMOVED: All rarity weights (now in AdaptiveRNG system)
    
    /// <summary>
    /// Gets all items of a specific rarity from this pool
    /// </summary>
    public List<CatchableItem> GetItemsByRarity(ItemRarity rarity)
    {
        return availableItems.FindAll(item => item.rarity == rarity);
    }

    public List<CatchableItem> GetAllItems()
    {
        return availableItems;
    }
}